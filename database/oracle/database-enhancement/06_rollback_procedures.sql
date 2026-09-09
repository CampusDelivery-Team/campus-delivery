/* Rollback script for member 3 procedures. */
BEGIN
    EXECUTE IMMEDIATE 'DROP PROCEDURE sp_accept_task_atomic';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

DROP PROCEDURE sp_manage_account_status;
DROP PROCEDURE sp_set_default_address;
DROP PROCEDURE sp_audit_runner;
