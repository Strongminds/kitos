#!/bin/sh
set -e

POSTGRES_HOST="${POSTGRES_HOST:-postgres}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
APP_USER="${APP_USER:-kitos}"

until pg_isready -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d postgres >/dev/null 2>&1; do
    sleep 1
done

for db in kitos kitos_hangfiredb kitos_pubsub; do
    db_exists="$(
        psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d postgres -tA -v ON_ERROR_STOP=1 -v db_name="$db" \
            -c "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'db_name');"
    )"

    if [ "$db_exists" != "t" ]; then
        echo "Skipping permissions for missing database: $db"
        continue
    fi

    psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 -v app_user="$APP_USER" -v db_name="$db" <<'EOSQL'
DO $$
BEGIN
    EXECUTE format('ALTER DATABASE %I OWNER TO %I', :'db_name', :'app_user');
    EXECUTE format('GRANT CONNECT, CREATE, TEMPORARY ON DATABASE %I TO %I', :'db_name', :'app_user');
END
$$;
EOSQL

    psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$db" -v ON_ERROR_STOP=1 -v app_user="$APP_USER" <<'EOSQL'
GRANT ALL ON SCHEMA public TO :"app_user";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO :"app_user";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO :"app_user";
EOSQL
done
