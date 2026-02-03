# Use .NET 10.0 SDK and runtime images
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Propentro.Api/Propentro.Api.csproj", "Propentro.Api/"]
COPY ["src/Propentro.Application/Propentro.Application.csproj", "Propentro.Application/"]
COPY ["src/Propentro.Domain/Propentro.Domain.csproj", "Propentro.Domain/"]
COPY ["src/Propentro.Infrastructure/Propentro.Infrastructure.csproj", "Propentro.Infrastructure/"]
COPY ["src/Propentro.Contracts/Propentro.Contracts.csproj", "Propentro.Contracts/"]
COPY ["aspire/Propentro.ServiceDefaults/Propentro.ServiceDefaults.csproj", "Propentro.ServiceDefaults/"]

RUN dotnet restore "Propentro.Api/Propentro.Api.csproj"

COPY src/ .
COPY aspire/Propentro.ServiceDefaults/ Propentro.ServiceDefaults/
WORKDIR /src/Propentro.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER $APP_UID

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Propentro.Api.dll"]
