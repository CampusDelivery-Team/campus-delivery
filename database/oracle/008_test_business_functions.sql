/*
  Migration 008 / member 4 function verification script.

  Execute as APPUSER after 008_create_business_functions.sql. The script does not modify base
  tables. Enable DBMS Output in DBeaver to see the expected-error messages.
*/

/* 1. All three functions must be VALID. */
SELECT object_name, object_type, status
  FROM user_objects
 WHERE object_type = 'FUNCTION'
   AND object_name IN (
       'FN_CALCULATE_TASK_PRICE',
       'FN_RUNNER_CAN_ACCEPT_TASK',
       'FN_GET_CREDIT_LEVEL'
   )
 ORDER BY object_name;

/* 2. This query must return no rows. */
SELECT name, type, line, position, text
  FROM user_errors
 WHERE type = 'FUNCTION'
   AND name IN (
       'FN_CALCULATE_TASK_PRICE',
       'FN_RUNNER_CAN_ACCEPT_TASK',
       'FN_GET_CREDIT_LEVEL'
   )
 ORDER BY name, sequence;

/* 3. Price calculation: base price and explicit non-negative surcharge. */
SELECT service_type_id,
       service_name,
       base_price,
       fn_calculate_task_price(service_type_id) AS base_result,
       fn_calculate_task_price(service_type_id, 2.50) AS price_with_extra
  FROM service_types
 WHERE type_status = 'ENABLED'
 ORDER BY service_type_id;

/* 4. Price calculation rejects a negative surcharge. */
DECLARE
    v_service_type_id service_types.service_type_id%TYPE;
    v_result          NUMBER;
BEGIN
    SELECT MIN(service_type_id)
      INTO v_service_type_id
      FROM service_types
     WHERE type_status = 'ENABLED';

    BEGIN
        v_result := fn_calculate_task_price(v_service_type_id, -0.01);
        RAISE_APPLICATION_ERROR(-20991, 'Negative surcharge was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20042 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS price negative-extra check: ' || SQLERRM);
    END;
END;
/

/* 5. Price calculation rejects a result outside tasks.task_price NUMBER(10,2). */
DECLARE
    v_service_type_id service_types.service_type_id%TYPE;
    v_result          NUMBER;
BEGIN
    SELECT MIN(service_type_id)
      INTO v_service_type_id
      FROM service_types
     WHERE type_status = 'ENABLED';

    BEGIN
        v_result := fn_calculate_task_price(v_service_type_id, 99999999.99);
        RAISE_APPLICATION_ERROR(-20994, 'Oversized price was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20045 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS oversized-price check: ' || SQLERRM);
    END;
END;
/

/*
  6. Eligibility samples. EXPECTED_RESULT is calculated independently from
  the documented current rules. FREE and BUSY are both eligible work states.
*/
SELECT r.runner_id,
       t.task_id,
       r.audit_status,
       r.work_status,
       u.account_status,
       t.task_status,
       CASE
           WHEN u.user_role = 'RUNNER'
            AND u.account_status = 'NORMAL'
            AND r.audit_status = 'APPROVED'
            AND r.work_status IN ('FREE', 'BUSY')
            AND t.task_status = 'WAITING'
            AND t.publisher_user_id <> r.user_id
           THEN 1 ELSE 0
       END AS expected_result,
       fn_runner_can_accept_task(r.runner_id, t.task_id) AS actual_result
  FROM runners r
  JOIN users u ON u.user_id = r.user_id
 CROSS JOIN tasks t
 ORDER BY r.runner_id, t.task_id
 FETCH FIRST 30 ROWS ONLY;

/* This query must return no rows. */
SELECT r.runner_id,
       t.task_id,
       fn_runner_can_accept_task(r.runner_id, t.task_id) AS actual_result
  FROM runners r
  JOIN users u ON u.user_id = r.user_id
 CROSS JOIN tasks t
 WHERE fn_runner_can_accept_task(r.runner_id, t.task_id) <>
       CASE
           WHEN u.user_role = 'RUNNER'
            AND u.account_status = 'NORMAL'
            AND r.audit_status = 'APPROVED'
            AND r.work_status IN ('FREE', 'BUSY')
            AND t.task_status = 'WAITING'
            AND t.publisher_user_id <> r.user_id
           THEN 1 ELSE 0
       END;

/* Nonexistent and NULL identifiers must return 0. */
SELECT fn_runner_can_accept_task(NULL, NULL) AS null_result,
       fn_runner_can_accept_task(-1, -1) AS missing_result
  FROM dual;

/* 7. Credit-level normal and boundary cases. */
SELECT score,
       fn_get_credit_level(score) AS credit_level
  FROM (
      SELECT 0 AS score FROM dual
      UNION ALL SELECT 59 FROM dual
      UNION ALL SELECT 60 FROM dual
      UNION ALL SELECT 69 FROM dual
      UNION ALL SELECT 70 FROM dual
      UNION ALL SELECT 79 FROM dual
      UNION ALL SELECT 80 FROM dual
      UNION ALL SELECT 89 FROM dual
      UNION ALL SELECT 90 FROM dual
      UNION ALL SELECT 100 FROM dual
      UNION ALL SELECT 120 FROM dual
  )
 ORDER BY score;

/* 8. Credit calculation rejects negative and NULL scores. */
DECLARE
    v_result VARCHAR2(20);
BEGIN
    BEGIN
        v_result := fn_get_credit_level(-1);
        RAISE_APPLICATION_ERROR(-20992, 'Negative credit was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20044 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS negative-credit check: ' || SQLERRM);
    END;

    BEGIN
        v_result := fn_get_credit_level(NULL);
        RAISE_APPLICATION_ERROR(-20993, 'NULL credit was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20044 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS null-credit check: ' || SQLERRM);
    END;
END;
/
