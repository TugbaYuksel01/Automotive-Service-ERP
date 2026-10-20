# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "OtoServis.Worker/OtoServisWorker.csproj"
RUN dotnet publish "OtoServis.Worker/OtoServisWorker.csproj" -c Release -o /app/publish

# Run aşaması
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OtoServisWorker.dll"]
