#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE USER test_user WITH PASSWORD 'test_password';
    CREATE DATABASE auth_test;
    GRANT ALL PRIVILEGES ON DATABASE auth_test TO test_user;
    
    \c auth_test;
    GRANT ALL ON SCHEMA public TO test_user;
EOSQL