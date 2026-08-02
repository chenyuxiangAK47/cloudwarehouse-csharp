# Optional container image for DevSecOps demos (Trivy / image scan).
# Primary deploy remains self-contained win-x64 exe; this Dockerfile proves containerization path.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY CloudWarehouse.sln ./
COPY CloudWarehouse.Pricing.Core/ CloudWarehouse.Pricing.Core/
COPY CloudWarehouse.Backend/ CloudWarehouse.Backend/
COPY CloudWarehouse.TestCommon/ CloudWarehouse.TestCommon/
COPY CloudWarehouse.Tests/ CloudWarehouse.Tests/
COPY CloudWarehouse.IntegrationTests/ CloudWarehouse.IntegrationTests/
RUN dotnet restore CloudWarehouse.Backend/CloudWarehouse.Backend.csproj
RUN dotnet publish CloudWarehouse.Backend/CloudWarehouse.Backend.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CloudWarehouse.Backend.dll"]
