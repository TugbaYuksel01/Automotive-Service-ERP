FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "OtoServis.Worker.csproj"
RUN dotnet publish "OtoServis.Worker.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OtoServis.Worker.dll"]
