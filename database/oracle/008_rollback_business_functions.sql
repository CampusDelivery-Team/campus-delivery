/*
  Rollback script for migration 008 / member 4 business functions.

  It only drops functions created by 008_create_business_functions.sql. Base tables, indexes
  and data are not modified.
*/

BEGIN
    EXECUTE IMMEDIATE 'DROP FUNCTION fn_get_credit_level';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP FUNCTION fn_runner_can_accept_task';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP FUNCTION fn_calculate_task_price';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4043 THEN
            RAISE;
        END IF;
END;
/

PROMPT Member 4 functions removed. Base tables and data were not changed.
