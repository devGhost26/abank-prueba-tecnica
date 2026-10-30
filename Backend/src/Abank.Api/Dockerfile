# Etapa 1: compilar y publicar con el SDK completo
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: la restauración queda en caché mientras no cambien los paquetes.
COPY src/Abank.Domain/Abank.Domain.csproj src/Abank.Domain/
COPY src/Abank.Application/Abank.Application.csproj src/Abank.Application/
COPY src/Abank.Infrastructure/Abank.Infrastructure.csproj src/Abank.Infrastructure/
COPY src/Abank.Api/Abank.Api.csproj src/Abank.Api/
RUN dotnet restore src/Abank.Api/Abank.Api.csproj

COPY src/ src/
RUN dotnet publish src/Abank.Api/Abank.Api.csproj -c Release -o /app/publish --no-restore

# Etapa 2: imagen final, solo con el runtime y sin privilegios de root
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "Abank.Api.dll"]
