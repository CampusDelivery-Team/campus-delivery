SET SERVEROUTPUT ON
SET FEEDBACK ON
SET PAGESIZE 200
SET LINESIZE 200

PROMPT WARNING:
PROMPT This is a template. Do not run it directly without replacing placeholders.
PROMPT This template is for database maintainers only.
PROMPT Confirm the target PDB, username, and password before running.
PROMPT Do not commit real passwords into this repository.

ALTER SESSION SET CONTAINER = ORCLPDB1;

DECLARE
    user_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO user_count
    FROM dba_users
    WHERE username = 'APPUSER';

    IF user_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE USER APPUSER IDENTIFIED BY "<database_password>"';
        DBMS_OUTPUT.PUT_LINE('Created user APPUSER.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('User APPUSER already exists.');
        EXECUTE IMMEDIATE 'ALTER USER APPUSER IDENTIFIED BY "<database_password>" ACCOUNT UNLOCK';
    END IF;
END;
/

GRANT CREATE SESSION TO APPUSER;
GRANT CREATE TABLE TO APPUSER;
GRANT CREATE VIEW TO APPUSER;
GRANT CREATE SEQUENCE TO APPUSER;
GRANT CREATE TRIGGER TO APPUSER;
GRANT CREATE PROCEDURE TO APPUSER;
ALTER USER APPUSER QUOTA UNLIMITED ON USERS;

SELECT username, account_status FROM dba_users WHERE username = 'APPUSER';

EXIT
