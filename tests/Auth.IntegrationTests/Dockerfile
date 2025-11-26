FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["tests/Auth.IntegrationTests/Auth.IntegrationTests.csproj", "tests/Auth.IntegrationTests/"]
COPY ["tests/Tests.Common/Tests.Common.csproj", "tests/Tests.Common/"]
COPY ["src/Services/Auth/Auth.API/Auth.API.csproj", "src/Services/Auth/Auth.API/"]
COPY ["src/Services/Auth/Auth.Core/Auth.Core.csproj", "src/Services/Auth/Auth.Core/"]
COPY ["src/Services/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj", "src/Services/Auth/Auth.Infrastructure/"]
COPY ["src/Services/Auth/Auth.Grpc/Auth.Grpc.csproj", "src/Services/Auth/Auth.Grpc/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Messaging/Shared.Messaging.csproj", "src/Shared/Messaging/"]
COPY ["src/Shared/Grpc/Shared.Grpc.csproj", "src/Shared/Grpc/"]

# Restore dependencies
RUN dotnet restore "tests/Auth.IntegrationTests/Auth.IntegrationTests.csproj"

# Copy source code
COPY . .

# Build tests
WORKDIR "/src/tests/Auth.IntegrationTests"
RUN dotnet build "Auth.IntegrationTests.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auth.IntegrationTests.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published tests
COPY --from=publish /app/publish .

# Create entrypoint script
RUN echo '#!/bin/bash\n\
echo "Waiting for dependencies..."\n\
while ! curl -f http://auth-db:5432 > /dev/null 2>&1; do\n\
  echo "Waiting for auth-db..."\n\
  sleep 2\n\
done\n\
\n\
while ! curl -f http://redis:6379 > /dev/null 2>&1; do\n\
  echo "Waiting for redis..."\n\
  sleep 2\n\
done\n\
\n\
echo "Running integration tests..."\n\
dotnet test Auth.IntegrationTests.dll --verbosity normal --logger "console;verbosity=detailed"\n\
\n\
if [ $? -eq 0 ]; then\n\
  echo "All tests passed!"\n\
else\n\
  echo "Some tests failed!"\n\
  exit 1\n\
fi' > /app/run-tests.sh && chmod +x /app/run-tests.sh

ENTRYPOINT ["/app/run-tests.sh"]