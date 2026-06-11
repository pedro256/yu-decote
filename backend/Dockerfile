# ==========================================
# Etapa 1: Base de Execução (Runtime)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Instala Python, FFmpeg e dependências necessárias para o yt-dlp funcionar no Linux
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    ffmpeg \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Baixa e instala a versão mais recente do yt-dlp globalmente no container
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp

# ==========================================
# Etapa 2: Compilação do .NET (SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura as dependências Nuget
COPY ["backend.csproj", "./"]
RUN dotnet restore "./backend.csproj"

# Copia o restante dos arquivos do código fonte
COPY . .
RUN dotnet build "backend.csproj" -c Release -o /app/build

# Publica o app otimizado para produção
FROM build AS publish
RUN dotnet publish "backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Etapa 3: Montagem Final da Imagem
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Cria a pasta onde os vídeos serão temporariamente salvos e dá permissão
RUN mkdir -p /app/downloads && chmod 777 /app/downloads

ENTRYPOINT ["dotnet", "backend.dll"]