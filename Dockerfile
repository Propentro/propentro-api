FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY Propentro.slnx .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY global.json .
COPY src/Propentro.Api/Propentro.Api.csproj src/Propentro.Api/
COPY src/Propentro.Application/Propentro.Application.csproj src/Propentro.Application/
COPY src/Propentro.Contracts/Propentro.Contracts.csproj src/Propentro.Contracts/
COPY src/Propentro.Domain/Propentro.Domain.csproj src/Propentro.Domain/
COPY src/Propentro.Infrastructure/Propentro.Infrastructure.csproj src/Propentro.Infrastructure/

# Restore dependencies
RUN dotnet restore Propentro.slnx

# Copy everything and publish
COPY src/ src/
RUN dotnet publish src/Propentro.Api/Propentro.Api.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Railway injects PORT at runtime; fall back to 8080
CMD dotnet Propentro.Api.dll --urls "http://+:${PORT:-8080}"
