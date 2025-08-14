# =========================
# Etapa 1: Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar .csproj con su carpeta
COPY ["ControlPanelGeshk/ControlPanelGeshk.csproj", "ControlPanelGeshk/"]
RUN dotnet restore "ControlPanelGeshk/ControlPanelGeshk.csproj"

# Copiar todo el código
COPY . .
RUN dotnet publish "ControlPanelGeshk/ControlPanelGeshk.csproj" -c Release -o /app/publish

# =========================
# Etapa 2: Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "ControlPanelGeshk.dll"]
