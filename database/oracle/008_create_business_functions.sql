/*
  Migration 008 / member 4: business functions.

  Execute as APPUSER. These functions expose reusable calculations and
  eligibility checks for SQL, views and stored procedures. They do not replace
  C# service-layer authorization, transactions, row locks or state changes.
*/

CREATE OR REPLACE FUNCTION fn_calculate_task_price (
    p_service_type_id IN service_types.service_type_id%TYPE,
    p_extra_amount    IN NUMBER DEFAULT 0
) RETURN NUMBER
IS
    v_base_price       service_types.base_price%TYPE;
    v_calculated_price NUMBER;
BEGIN
    IF p_service_type_id IS NULL THEN
        RAISE_APPLICATION_ERROR(-20041, 'Service type id is required.');
    END IF;

    IF p_extra_amount IS NULL OR p_extra_amount < 0 THEN
        RAISE_APPLICATION_ERROR(-20042, 'Extra amount must be zero or greater.');
    END IF;

    SELECT base_price
      INTO v_base_price
      FROM service_types
     WHERE service_type_id = p_service_type_id
       AND type_status = 'ENABLED';

    v_calculated_price := ROUND(v_base_price + p_extra_amount, 2);

    IF v_calculated_price > 99999999.99 THEN
        RAISE_APPLICATION_ERROR(-20045, 'Calculated task price exceeds NUMBER(10,2).');
    END IF;

    RETURN v_calculated_price;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20043, 'Enabled service type was not found.');
END;
/

CREATE OR REPLACE FUNCTION fn_runner_can_accept_task (
    p_runner_id IN runners.runner_id%TYPE,
    p_task_id   IN tasks.task_id%TYPE
) RETURN NUMBER
IS
    v_match_count PLS_INTEGER;
BEGIN
    IF p_runner_id IS NULL OR p_task_id IS NULL THEN
        RETURN 0;
    END IF;

    SELECT COUNT(*)
      INTO v_match_count
      FROM runners r
      JOIN users u
        ON u.user_id = r.user_id
      JOIN tasks t
        ON t.task_id = p_task_id
     WHERE r.runner_id = p_runner_id
       AND u.user_role = 'RUNNER'
       AND u.account_status = 'NORMAL'
       AND r.audit_status = 'APPROVED'
       AND r.work_status IN ('FREE', 'BUSY')
       AND t.task_status = 'WAITING'
       AND t.publisher_user_id <> r.user_id;

    RETURN CASE WHEN v_match_count = 1 THEN 1 ELSE 0 END;
END;
/

CREATE OR REPLACE FUNCTION fn_get_credit_level (
    p_credit_score IN runners.credit_score%TYPE
) RETURN VARCHAR2 DETERMINISTIC
IS
BEGIN
    IF p_credit_score IS NULL OR p_credit_score < 0 THEN
        RAISE_APPLICATION_ERROR(-20044, 'Credit score must be zero or greater.');
    END IF;

    RETURN CASE
        WHEN p_credit_score >= 90 THEN 'EXCELLENT'
        WHEN p_credit_score >= 80 THEN 'GOOD'
        WHEN p_credit_score >= 70 THEN 'NORMAL'
        WHEN p_credit_score >= 60 THEN 'WATCH'
        ELSE 'RISK'
    END;
END;
/

PROMPT Member 4 functions created. Check USER_OBJECTS and USER_ERRORS before use.
