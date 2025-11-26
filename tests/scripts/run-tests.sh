#!/bin/bash

set -e

echo "Starting test environment..."

docker-compose --profile test up -d auth-db redis auth-service

echo "Waiting for services to be healthy..."
sleep 10

echo "Waiting for auth-service..."
until curl -f http://localhost:5001/health > /dev/null 2>&1; do
    sleep 5
done

echo "Services are ready!"

echo "Running tests..."
docker-compose --profile test up \
    --abort-on-container-exit \
    --exit-code-from auth-all-tests \
    auth-all-tests

TEST_EXIT_CODE=$?

echo "Stopping test environment..."
docker-compose down

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo "All tests passed!"
else
    echo "Some tests failed!"
    exit $TEST_EXIT_CODE
fi