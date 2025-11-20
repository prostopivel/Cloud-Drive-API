FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/FileStorage/FileStorage.API/FileStorage.API.csproj", "src/Services/FileStorage/FileStorage.API/"]
COPY ["src/Services/FileStorage/FileStorage.Core/FileStorage.Core.csproj", "src/Services/FileStorage/FileStorage.Core/"]
COPY ["src/Services/FileStorage/FileStorage.Infrastructure/FileStorage.Infrastructure.csproj", "src/Services/FileStorage/FileStorage.Infrastructure/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
RUN dotnet restore "src/Services/FileStorage/FileStorage.API/FileStorage.API.csproj"

COPY . .
WORKDIR "/src/src/Services/FileStorage/FileStorage.API"
RUN dotnet build "FileStorage.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileStorage.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create storage directory and set permissions
RUN mkdir -p /app/storage && chmod 755 /app/storage

ENTRYPOINT ["dotnet", "FileStorage.API.dll"]