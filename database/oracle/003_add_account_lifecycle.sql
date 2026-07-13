/*
  Account lifecycle migration.
  Run once in an existing development database after 002_init_base_data.sql.
*/

UPDATE users
SET account_status = 'BLOCKED'
WHERE account_status = 'DISABLED';

ALTER TABLE users DROP CONSTRAINT ck_users_status;

ALTER TABLE users ADD CONSTRAINT ck_users_status
    CHECK (account_status IN ('NORMAL', 'BLOCKED', 'CANCELLED'));

COMMENT ON COLUMN users.account_status IS '账号状态：NORMAL/BLOCKED/CANCELLED';

COMMIT;
