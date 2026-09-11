/*
  Rollback script for member 4 business functions created by
  04_functions.sql.

  It only drops the three functions. It intentionally does not remove the
  0..100 credit-score constraint or attempt to reconstruct historical scores
  that were truncated to 100, because those previous values were not retained.
*/

BEGIN
    EXECUTE IMMEDIATE 'DROP FUNCTION fn_service_node_allowed';
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

SELECT 'Member 4 functions removed. Credit-score data and constraint were retained.'
       AS rollback_result
  FROM dual;
