# ✂️ YuDeCote

Plataforma SaaS para corte automático de vídeos do YouTube. O usuário informa a URL do vídeo, o tempo de início e o tempo de fim — o sistema processa, corta e disponibiliza o trecho para download.

---

## 🧱 Arquitetura

```
Frontend (Next.js)  →  API (ASP.NET Core)  →  Fila  →  Worker  →  MinIO
                                                           ↓
                                                     yt-dlp + ffmpeg
```

### Componentes

| Camada | Tecnologia | Responsabilidade |
|---|---|---|
| Frontend | Next.js 16 | Interface do usuário, envio de pedidos de corte |
| Backend | ASP.NET Core (.NET) | API REST, gerenciamento de fila, autenticação |
| Worker | BackgroundService (.NET) | Processamento assíncrono dos cortes |
| Download | yt-dlp | Obtenção do vídeo/segmento do YouTube |
| Processamento | ffmpeg | Corte e re-encoding do vídeo |
| Storage | MinIO (S3-compatible) | Armazenamento temporário dos vídeos cortados |

---

## ⚙️ Como funciona

1. O usuário acessa o frontend e informa:
   - URL do vídeo do YouTube
   - Tempo de início (em segundos)
   - Tempo de fim (em segundos)

2. O frontend envia o pedido para a API

3. A API enfileira o pedido e retorna um `taskId`

4. O Worker processa o pedido de forma assíncrona:
   - Verifica a duração total do vídeo via `yt-dlp --print duration`
   - **Vídeo ≤ 10 minutos:** baixa o vídeo completo e corta localmente com ffmpeg
   - **Vídeo > 10 minutos:** usa `--download-sections` para baixar apenas os segmentos do intervalo solicitado
   - Re-encoda com `libx264 + aac -preset ultrafast` para garantir vídeo sem travamento
   - Faz upload do arquivo cortado para o MinIO
   - Deleta os arquivos temporários automaticamente

5. O frontend consulta o status pelo `taskId` e disponibiliza o download quando concluído

---

## 🔒 Modelo Free vs Premium

| Recurso | Free | Premium |
|---|---|---|
| Duração máxima do corte | 3 minutos | Sem limite |
| Qualidade máxima | 480p | 720p |
| Velocidade da fila | Normal | Prioritária |
| Downloads simultâneos | 1 | Múltiplos |

---

## 🛠️ Tecnologias utilizadas

- **Next.js 16** — frontend
- **ASP.NET Core (.NET)** — backend e worker
- **yt-dlp** — download e extração de segmentos do YouTube
- **ffmpeg** — corte e processamento de vídeo
- **MinIO** — object storage compatível com S3
- **Docker / Docker Compose** — ambiente containerizado

---

## 🚀 Como rodar localmente

### Pré-requisitos

- Docker e Docker Compose instalados
- `yt-dlp` e `ffmpeg` disponíveis no container do backend

### Subindo o ambiente

```bash
docker compose up -d
```

### Variáveis de ambiente (backend)

```env
MinIO__ServiceURL=http://localhost:9000
MinIO__AccessKey=minioadmin
MinIO__SecretKey=minioadmin
```

---

## 📁 Estrutura do projeto

```
/
├── frontend/          # Next.js 16
│   └── ...
├── backend/           # ASP.NET Core
│   ├── Workers/
│   │   └── CorteVideoWorker.cs
│   ├── Queue/
│   │   └── CorteQueue/
│   ├── Models/
│   │   ├── Corte/
│   │   └── Enviroments/
│   └── ...
└── docker-compose.yml
```

---

## ⚠️ Observações sobre infraestrutura

O desempenho do worker depende diretamente da **banda de rede entre o servidor e os CDNs do YouTube**. Em ambiente local (especialmente em regiões com peering indireto com o Google, como Manaus), o download pode ser significativamente mais lento do que em servidores cloud com presença em São Paulo ou nos EUA.

Em produção, recomenda-se uma VM com:

- 2+ vCPUs
- 4GB RAM
- **Banda de 1Gbps** (DigitalOcean, AWS sa-east-1, Hetzner)
- 20GB+ de disco para arquivos temporários

---

## 📌 Pendências

- [ ] **Download mais performático** — a abordagem atual ainda é limitada pela banda e pela forma como o ffmpeg lida com streams DASH do YouTube. Investigar alternativas como: pre-fetch de segmentos em paralelo, uso de cookies autenticados para streams de maior qualidade, ou integração com serviços de processamento de vídeo gerenciados (ex: Mux, api.video).
