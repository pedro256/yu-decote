using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Corte;
using backend.Models.Enviroments;
using backend.Queue.CorteQueue;
using ManuHub.Ytdlp.NET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;



namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController(
        ICorteQueue queue,
        IOptions<MinIoOptions> minIoOptions,
        ILogger<VideoController> _logger
        ) : ControllerBase
    {
        private readonly MinIoOptions _minIoOptions = minIoOptions.Value;
        public record CorteRequest(string Url, int Inicio, int Fim);
        [HttpPost("cortar")]
        public async Task<IActionResult> SolicitarCorte(CorteRequest model)
        {
            if (string.IsNullOrEmpty(model.Url))
            {
                return BadRequest("Parâmetros inválidos.");
            }

            string taskId = Guid.NewGuid().ToString();
            string keyName = $"cortes/{taskId}.mp4";

            var pedido = new CorteVideoPedido(
                TaskId: taskId,
                Url: model.Url,
                Inicio: model.Inicio,
                Fim: model.Fim,
                BucketName: _minIoOptions.BucketName,
                KeyName: keyName
            );

            _logger.LogInformation("Recebida nova solicitação de corte. Gerando TaskId: {Id}", taskId);

            await queue.EnfileirarPedidoAsync(pedido);
            return Accepted(new { TaskId = taskId, Key = keyName, Status = "Na Fila" });
        }
    }
}