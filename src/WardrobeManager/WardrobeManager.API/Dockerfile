# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiem csproj-urile pentru restore rapid
COPY ["src/WardrobeManager/WardrobeManager.API/WardrobeManager.API.csproj", "src/WardrobeManager/WardrobeManager.API/"]
COPY ["src/WardrobeManager/WardrobeManager.Application/WardrobeManager.Application.csproj", "src/WardrobeManager/WardrobeManager.Application/"]
COPY ["src/WardrobeManager/WardrobeManager.Domain/WardrobeManager.Domain.csproj", "src/WardrobeManager/WardrobeManager.Domain/"]
COPY ["src/WardrobeManager/WardrobeManager.Infrastructure/WardrobeManager.Infrastructure.csproj", "src/WardrobeManager/WardrobeManager.Infrastructure/"]

RUN dotnet restore "src/WardrobeManager/WardrobeManager.API/WardrobeManager.API.csproj"

# Copiem tot codul
COPY . .

RUN dotnet publish "src/WardrobeManager/WardrobeManager.API/WardrobeManager.API.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WardrobeManager.API.dll"]