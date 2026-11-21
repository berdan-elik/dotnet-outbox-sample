FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Project files first, so the restore layer is reused until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/OutboxSample.Domain/OutboxSample.Domain.csproj src/OutboxSample.Domain/
COPY src/OutboxSample.Application/OutboxSample.Application.csproj src/OutboxSample.Application/
COPY src/OutboxSample.Infrastructure/OutboxSample.Infrastructure.csproj src/OutboxSample.Infrastructure/
COPY src/OutboxSample.Api/OutboxSample.Api.csproj src/OutboxSample.Api/
RUN dotnet restore src/OutboxSample.Api

COPY src/ src/
RUN dotnet publish src/OutboxSample.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OutboxSample.Api.dll"]
