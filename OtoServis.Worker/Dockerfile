FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Tüm çözümü ve klasörleri kopyala ki Contracts gibi ortak projeleri bulabilsin
COPY . .

# Worker projesini kök dizindeki yolu belirterek restore ve publish et
RUN dotnet restore "OtoServis.Worker/OtoServis.Worker.csproj"
RUN dotnet publish "OtoServis.Worker/OtoServis.Worker.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OtoServis.Worker.dll"]
