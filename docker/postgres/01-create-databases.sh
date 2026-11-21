#!/bin/sh
set -eu

psql --set=ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
    --set=accounts_password="$ACCOUNTS_DB_PASSWORD" --set=tasks_password="$TASKS_DB_PASSWORD" <<'SQL'
CREATE ROLE accounts_app LOGIN PASSWORD :'accounts_password';
CREATE ROLE tasks_app LOGIN PASSWORD :'tasks_password';

CREATE DATABASE accounts OWNER accounts_app;
CREATE DATABASE tasks OWNER tasks_app;

REVOKE CONNECT ON DATABASE accounts FROM PUBLIC;
REVOKE CONNECT ON DATABASE tasks FROM PUBLIC;
SQL
