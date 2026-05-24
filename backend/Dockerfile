# =============================================================================
# 42jobs - Backend Dockerfile (Multi-stage .NET)
# =============================================================================

# ─── Development (dotnet watch hot-reload) ─────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS development
LABEL maintainer="Samuel Hurtado Marin <samuel@hurtadom.dev>"
WORKDIR /app
COPY src/src.csproj src/
RUN dotnet restore src/src.csproj
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
EXPOSE 8080
CMD ["dotnet", "watch", "run", "--project", "src/src.csproj", "--no-launch-profile"]

# ─── Build ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app
COPY src/src.csproj src/
RUN dotnet restore src/src.csproj
COPY src/ src/
RUN dotnet publish src/src.csproj -c Release -o /out

# ─── Production ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS production
RUN apk add --no-cache curl
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
CMD ["dotnet", "src.dll"]
