/*
  回滚 03_procedures.sql 创建的四个过程。
  每段均允许目标过程已经不存在，便于重复执行。
*/
BEGIN
    EXECUTE IMMEDIATE 'DROP PROCEDURE sp_accept_task_atomic';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PROCEDURE sp_audit_runner';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PROCEDURE sp_set_default_address';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP PROCEDURE sp_manage_account_status';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

SELECT '03_procedures.sql 中的四个过程已删除。' AS rollback_result
  FROM dual;
