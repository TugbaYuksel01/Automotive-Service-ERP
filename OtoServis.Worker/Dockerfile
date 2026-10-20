FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "OtoServis.Worker/OtoServis.Worker.csproj"
RUN dotnet publish "OtoServis.Worker/OtoServis.Worker.csproj" -c Release -o /app/publish

# Burada runtime yerine aspnet imajını kullanıyoruz ki AspNetCore paketleri eksik kalmasın
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OtoServis.Worker.dll"]
