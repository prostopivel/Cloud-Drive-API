#!/bin/bash

set -e

echo "Running integration tests only..."

docker-compose --profile test up -d auth-db redis

echo "Waiting for test database..."
sleep 5

docker-compose --profile test up \
    --abort-on-container-exit \
    --exit-code-from auth-integration-tests \
    auth-integration-tests

docker-compose down