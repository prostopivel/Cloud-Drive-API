FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files
COPY ["tests/Tests.Common/Tests.Common.csproj", "tests/Tests.Common/"]
COPY ["tests/Auth.IntegrationTests/Auth.IntegrationTests.csproj", "tests/Auth.IntegrationTests/"]
COPY ["tests/Auth.LoadTests/Auth.LoadTests.csproj", "tests/Auth.LoadTests/"]
COPY ["src/Services/Auth/Auth.API/Auth.API.csproj", "src/Services/Auth/Auth.API/"]
COPY ["src/Services/Auth/Auth.Core/Auth.Core.csproj", "src/Services/Auth/Auth.Core/"]
COPY ["src/Services/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj", "src/Services/Auth/Auth.Infrastructure/"]
COPY ["src/Services/Auth/Auth.Grpc/Auth.Grpc.csproj", "src/Services/Auth/Auth.Grpc/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Messaging/Shared.Messaging.csproj", "src/Shared/Messaging/"]
COPY ["src/Shared/Grpc/Shared.Grpc.csproj", "src/Shared/Grpc/"]

# Restore dependencies for all test projects
RUN dotnet restore "tests/Tests.Common/Tests.Common.csproj"
RUN dotnet restore "tests/Auth.IntegrationTests/Auth.IntegrationTests.csproj"
RUN dotnet restore "tests/Auth.LoadTests/Auth.LoadTests.csproj"

# Copy source code
COPY . .

# Build all tests
WORKDIR "/src/tests/Tests.Common"
RUN dotnet build "Tests.Common.csproj" -c Release --no-restore

WORKDIR "/src/tests/Auth.IntegrationTests"
RUN dotnet build "Auth.IntegrationTests.csproj" -c Release -o /app/integration-tests

WORKDIR "/src/tests/Auth.LoadTests"
RUN dotnet build "Auth.LoadTests.csproj" -c Release -o /app/load-tests

# Runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

# Install dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        netcat-openbsd \
        curl \
        jq && \
    rm -rf /var/lib/apt/lists/* && \
    apt-get clean

# Copy built tests
COPY --from=build /app/integration-tests ./integration-tests
COPY --from=build /app/load-tests ./load-tests

# Create test runner script
RUN echo '#!/bin/bash\n\
\n\
run_integration_tests() {\n\
    echo "=== Running Integration Tests === "\n\
    cd /app/integration-tests\n\
    \n\
    echo "Waiting for test dependencies..."\n\
    while ! nc -z auth-db 5432; do\n\
        echo "Waiting for auth-db..."\n\
        sleep 2\n\
    done\n\
    \n\
    while ! nc -z redis 6379; do\n\
        echo "Waiting for redis..."\n\
        sleep 2\n\
    done\n\
    \n\
    echo "Starting integration tests..."\n\
    dotnet test Auth.IntegrationTests.dll --verbosity normal --logger "console;verbosity=normal"\n\
    return $?\n\
}\n\
\n\
run_load_tests() {\n\
    echo "=== Running Load Tests === "\n\
    cd /app/load-tests\n\
    \n\
    echo "Waiting for auth-service..."\n\
    while ! curl -f http://auth-service:5001/health > /dev/null 2>&1; do\n\
        echo "Waiting for auth-service..."\n\
        sleep 5\n\
    done\n\
    \n\
    echo "Starting load tests..."\n\
    dotnet test Auth.LoadTests.dll --verbosity normal --logger "console;verbosity=normal"\n\
    return $?\n\
}\n\
\n\
case "$1" in\n\
    "integration")\n\
        run_integration_tests\n\
        ;;\n\
    "load")\n\
        run_load_tests\n\
        ;;\n\
    "all")\n\
        run_integration_tests\n\
        if [ $? -eq 0 ]; then\n\
            run_load_tests\n\
        else\n\
            echo "Integration tests failed, skipping load tests"\n\
            exit 1\n\
        fi\n\
        ;;\n\
    *)\n\
        echo "Usage: $0 {integration|load|all}"\n\
        echo "  integration - Run only integration tests"\n\
        echo "  load        - Run only load tests"\n\
        echo "  all         - Run all tests in sequence"\n\
        exit 1\n\
        ;;\n\
esac\n\
\n\
if [ $? -eq 0 ]; then\n\
    echo "Tests completed successfully!"\n\
else\n\
    echo "Tests failed!"\n\
    exit 1\n\
fi' > /app/run-tests.sh && chmod +x /app/run-tests.sh

ENTRYPOINT ["/app/run-tests.sh"]
CMD ["all"]