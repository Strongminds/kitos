#!/bin/bash
set -e

# This script is mounted into the PostgreSQL container as an init script.
# It creates the required databases and a shared application user.

POSTGRES_USER="${POSTGRES_USER:-postgres}"
APP_USER="${APP_USER:-kitos}"
APP_PASSWORD="${APP_PASSWORD:-kitos}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "postgres" <<-EOSQL
    -- Create application user
    DO \$\$
    BEGIN
        IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '${APP_USER}') THEN
            CREATE ROLE ${APP_USER} WITH LOGIN PASSWORD '${APP_PASSWORD}';
        END IF;
    END
    \$\$;

    -- Create databases
    SELECT 'CREATE DATABASE kitos OWNER ${APP_USER}'
        WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'kitos')\gexec

    SELECT 'CREATE DATABASE kitos_hangfire OWNER ${APP_USER}'
        WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'kitos_hangfire')\gexec

    SELECT 'CREATE DATABASE kitos_pubsub OWNER ${APP_USER}'
        WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'kitos_pubsub')\gexec

    -- Grant privileges
    GRANT ALL PRIVILEGES ON DATABASE kitos TO ${APP_USER};
    GRANT ALL PRIVILEGES ON DATABASE kitos_hangfire TO ${APP_USER};
    GRANT ALL PRIVILEGES ON DATABASE kitos_pubsub TO ${APP_USER};
EOSQL

# Connect to each database and grant schema privileges
for db in kitos kitos_hangfire kitos_pubsub; do
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
        GRANT ALL ON SCHEMA public TO ${APP_USER};
        ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO ${APP_USER};
        ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO ${APP_USER};
EOSQL
done
