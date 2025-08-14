# =========================
# Etapa 1: Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos los archivos del proyecto
COPY ["ControlPanelGeshk.csproj", "./"]
RUN dotnet restore "ControlPanelGeshk.csproj"

# Copiamos todo el código y construimos en modo Release
COPY . .
RUN dotnet publish "ControlPanelGeshk.csproj" -c Release -o /app/publish

# =========================
# Etapa 2: Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Variables de entorno para ASP.NET
ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Copiamos la publicación desde la etapa anterior
COPY --from=build /app/publish .

# Exponemos el puerto
EXPOSE 5000

# Arrancamos la aplicación
ENTRYPOINT ["dotnet", "ControlPanelGeshk.dll"]
