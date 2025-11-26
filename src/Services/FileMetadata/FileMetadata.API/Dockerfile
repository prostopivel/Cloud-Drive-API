FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/FileMetadata/FileMetadata.API/FileMetadata.API.csproj", "src/Services/FileMetadata/FileMetadata.API/"]
COPY ["src/Services/FileMetadata/FileMetadata.Core/FileMetadata.Core.csproj", "src/Services/FileMetadata/FileMetadata.Core/"]
COPY ["src/Services/FileMetadata/FileMetadata.Infrastructure/FileMetadata.Infrastructure.csproj", "src/Services/FileMetadata/FileMetadata.Infrastructure/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/API/Shared.API.csproj", "src/Shared/API/"]
RUN dotnet restore "src/Services/FileMetadata/FileMetadata.API/FileMetadata.API.csproj"

COPY . .
WORKDIR "/src/src/Services/FileMetadata/FileMetadata.API"
RUN dotnet build "FileMetadata.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileMetadata.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create storage directory and set permissions
RUN mkdir -p /app/storage && chmod 755 /app/storage

ENTRYPOINT ["dotnet", "FileMetadata.API.dll"]