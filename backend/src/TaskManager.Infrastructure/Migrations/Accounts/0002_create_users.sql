CREATE TABLE accounts.users (
    id uuid PRIMARY KEY,
    user_name varchar(254) NOT NULL,
    normalized_user_name varchar(254) NOT NULL UNIQUE,
    password_hash text NOT NULL,
    concurrency_stamp text NOT NULL,
    access_failed_count integer NOT NULL DEFAULT 0,
    lockout_enabled boolean NOT NULL DEFAULT TRUE,
    lockout_end timestamptz
);
