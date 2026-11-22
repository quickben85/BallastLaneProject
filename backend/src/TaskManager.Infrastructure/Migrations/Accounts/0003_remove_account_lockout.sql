ALTER TABLE accounts.users
    DROP COLUMN access_failed_count,
    DROP COLUMN lockout_enabled,
    DROP COLUMN lockout_end;
