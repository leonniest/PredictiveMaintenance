FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PredictiveMaintenance.sln ./
COPY backend/src/PredictiveMaintenance.Domain/PredictiveMaintenance.Domain.csproj backend/src/PredictiveMaintenance.Domain/
COPY backend/src/PredictiveMaintenance.Application/PredictiveMaintenance.Application.csproj backend/src/PredictiveMaintenance.Application/
COPY backend/src/PredictiveMaintenance.Infrastructure/PredictiveMaintenance.Infrastructure.csproj backend/src/PredictiveMaintenance.Infrastructure/
COPY backend/src/PredictiveMaintenance.Api/PredictiveMaintenance.Api.csproj backend/src/PredictiveMaintenance.Api/
RUN dotnet restore backend/src/PredictiveMaintenance.Api/PredictiveMaintenance.Api.csproj
COPY backend/src backend/src
RUN dotnet publish backend/src/PredictiveMaintenance.Api/PredictiveMaintenance.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PredictiveMaintenance.Api.dll"]
