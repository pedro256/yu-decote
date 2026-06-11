using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using backend.Models.Corte;
using backend.Models.Enviroments;
using backend.Queue.CorteQueue;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend.Workers
{
    public class CorteVideoWorker : BackgroundService
    {
        private readonly ICorteQueue _queue;
        private readonly MinIoOptions _minIoOptions;
        private readonly ILogger<CorteVideoWorker> _logger;

        private static readonly SemaphoreSlim _semaphore = new(5);

        public CorteVideoWorker(
            ICorteQueue queue,
            IOptions<MinIoOptions> minIoOptions,
            ILogger<CorteVideoWorker> logger)
        {
            _queue = queue;
            _minIoOptions = minIoOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CorteVideoWorker inicializado e aguardando fila...");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pedido = await _queue.DesenfileirarPedidoAsync(stoppingToken);

                    if (pedido == null)
                    {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        _logger.LogInformation("Pedido [{Id}] aguardando liberação na fila de CPU...", pedido.TaskId);
                        await _semaphore.WaitAsync(stoppingToken);
                        try
                        {
                            await ProcessarEStreamarParaMinIoAsync(pedido, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro crítico ao processar tarefa [{Id}]", pedido.TaskId);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Operação cancelada. Desligando Worker...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no loop principal do Worker de vídeos.");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task ProcessarEStreamarParaMinIoAsync(CorteVideoPedido pedido, CancellationToken ct)
        {
            _logger.LogInformation("[{Now}] Iniciando tarefa [{Id}]: {Url}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), pedido.TaskId, pedido.Url);

            var config = new AmazonS3Config { ServiceURL = _minIoOptions.ServiceURL, ForcePathStyle = true };
            using var s3Client = new AmazonS3Client(_minIoOptions.AccessKey, _minIoOptions.SecretKey, config);

            var tempVideo = Path.Combine(Path.GetTempPath(), $"{pedido.TaskId}_full");
            var tempCorte = Path.Combine(Path.GetTempPath(), $"{pedido.TaskId}_corte.mp4");

            try
            {
                // ── PASSO 1: Checa a duração do vídeo antes de baixar ───────────────
                _logger.LogInformation("[{Id}] Verificando duração do vídeo...", pedido.TaskId);
                var duracaoStr = await RunProcessAndGetOutputAsync(
                    "yt-dlp",
                    $"--print duration \"{pedido.Url}\"",
                    ct);

                double duracaoTotal = double.Parse(duracaoStr.Trim(), System.Globalization.CultureInfo.InvariantCulture);
                _logger.LogInformation("[{Id}] Duração total: {Duracao}s", pedido.TaskId, duracaoTotal);

                if (duracaoTotal <= 1800) // até 30 minutos: baixa tudo e corta local
                {
                    _logger.LogInformation("[{Id}] Vídeo curto — baixando completo para corte local...", pedido.TaskId);

                    var ytArgs = $"-f \"bestvideo[height<=720]+bestaudio/best[height<=720]\" " +
                                 $"--no-playlist -o \"{tempVideo}.%(ext)s\" \"{pedido.Url}\"";

                    await RunProcessAsync("yt-dlp", ytArgs, pedido.TaskId, ct);

                    // Descobre a extensão que o yt-dlp usou
                    var arquivoBaixado = Directory.GetFiles(
                        Path.GetTempPath(),
                        $"{pedido.TaskId}_full.*")[0];

                    double duracao = pedido.Fim - pedido.Inicio;
                    var ffmpegArgs = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "-ss {0} -t {1} -i \"{2}\" -c:v libx264 -c:a aac -preset ultrafast -movflags +faststart -y \"{3}\"",
                        pedido.Inicio, duracao, arquivoBaixado, tempCorte);

                    _logger.LogInformation("[{Id}] Cortando localmente...", pedido.TaskId);
                    await RunProcessAsync("ffmpeg", ffmpegArgs, pedido.TaskId, ct);

                    // Deleta o vídeo completo assim que o corte termina
                    if (File.Exists(arquivoBaixado)) File.Delete(arquivoBaixado);
                }
                else // acima de 10 minutos: baixa só os segmentos do intervalo
                {
                    _logger.LogInformation("[{Id}] Vídeo longo — baixando apenas segmento {Inicio}-{Fim}...",
                        pedido.TaskId, pedido.Inicio, pedido.Fim);

                    var ytArgs = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "--download-sections \"*{0}-{1}\" " +
                        "-f \"bestvideo[height<=720][ext=mp4]+bestaudio[ext=m4a]/best[height<=720]\" " +
                        "--merge-output-format mp4 " +
                        "--postprocessor-args \"ffmpeg:-c:v libx264 -c:a aac -preset ultrafast -movflags +faststart\" " +
                        "--no-playlist -o \"{2}\" \"{3}\"",
                        pedido.Inicio, pedido.Fim, tempCorte, pedido.Url);

                    await RunProcessAsync("yt-dlp", ytArgs, pedido.TaskId, ct);
                }

                // ── PASSO 2: Upload pro MinIO ────────────────────────────────────────
                _logger.LogInformation("[{Id}] Enviando para MinIO...", pedido.TaskId);
                var transferUtility = new TransferUtility(s3Client);
                await transferUtility.UploadAsync(tempCorte, pedido.BucketName, pedido.KeyName, ct);

                _logger.LogInformation("[{Now}] Tarefa [{Id}] concluída! Chave: {Key}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), pedido.TaskId, pedido.KeyName);
            }
            finally
            {
                // Garante limpeza mesmo em caso de erro
                if (File.Exists(tempCorte)) File.Delete(tempCorte);

                // Limpa qualquer arquivo _full que tenha sobrado
                foreach (var f in Directory.GetFiles(Path.GetTempPath(), $"{pedido.TaskId}_full.*"))
                    File.Delete(f);
            }
        }

        private async Task<string> RunProcessAndGetOutputAsync(string fileName, string args, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            proc.Start();

            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
            {
                var err = await proc.StandardError.ReadToEndAsync(ct);
                throw new Exception($"{fileName} falhou (exit {proc.ExitCode}): {err}");
            }

            return output;
        }

        private async Task RunProcessAsync(string fileName, string args, string taskId, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogDebug("[{FileName}/{Id}] {Data}", fileName, taskId, e.Data);
            };

            proc.Start();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
                throw new Exception($"{fileName} falhou com exit code {proc.ExitCode}");
        }
    }
}