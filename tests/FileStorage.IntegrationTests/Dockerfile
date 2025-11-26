FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["tests/FileStorage.IntegrationTests/FileStorage.IntegrationTests.csproj", "tests/FileStorage.IntegrationTests/"]
COPY ["tests/Tests.Common/Tests.Common.csproj", "tests/Tests.Common/"]
COPY ["src/Services/FileStorage/FileStorage.API/FileStorage.API.csproj", "src/Services/FileStorage/FileStorage.API/"]
COPY ["src/Services/FileStorage/FileStorage.Core/FileStorage.Core.csproj", "src/Services/FileStorage/FileStorage.Core/"]
COPY ["src/Services/FileStorage/FileStorage.Infrastructure/FileStorage.Infrastructure.csproj", "src/Services/FileStorage/FileStorage.Infrastructure/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Messaging/Shared.Messaging.csproj", "src/Shared/Messaging/"]
COPY ["src/Shared/Grpc/Shared.Grpc.csproj", "src/Shared/Grpc/"]

# Restore dependencies
RUN dotnet restore "tests/FileStorage.IntegrationTests/FileStorage.IntegrationTests.csproj"

# Copy source code
COPY . .

# Build tests
WORKDIR "/src/tests/FileStorage.IntegrationTests"
RUN dotnet build "FileStorage.IntegrationTests.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "test", "FileStorage.IntegrationTests.dll", "--verbosity", "normal"]