# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f deploy/Dockerfile -t slate-ai .

# --- 1. Web: build the PWA into the API's wwwroot (D-10) ---
FROM node:22-alpine AS web
WORKDIR /repo/src/web
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/web/ ./
RUN npm run build

# --- 2. API: restore, then publish with the built SPA ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props ./
COPY src/api/SlateAi.Api/SlateAi.Api.csproj src/api/SlateAi.Api/
RUN dotnet restore src/api/SlateAi.Api/SlateAi.Api.csproj
COPY src/api/ src/api/
COPY data/seed/ data/seed/
COPY --from=web /repo/src/api/SlateAi.Api/wwwroot/ src/api/SlateAi.Api/wwwroot/
RUN dotnet publish src/api/SlateAi.Api/SlateAi.Api.csproj -c Release -o /app --no-restore

# --- 3. Runtime: chiseled, non-root, no shell. Listens on 8080. ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
# Reported by /health; the deploy smoke test waits for this value. Declared here so earlier stages stay cached.
ARG APP_VERSION=dev
ENV Build__Version=$APP_VERSION
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "SlateAi.Api.dll"]
