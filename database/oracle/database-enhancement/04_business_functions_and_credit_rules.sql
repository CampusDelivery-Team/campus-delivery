/*
  Member 4 database enhancement: business functions and credit-score rules.

  Execute once as APPUSER after the base schema and seed data are ready.
  The functions expose reusable calculations and eligibility checks for SQL,
  views and stored procedures. They do not replace C# service-layer
  authorization, transactions, row locks or state changes.

  The final section normalizes historical credit scores above 100 and replaces
  CK_RUNNERS_CREDIT with a 0..100 constraint. The normalization is intentional
  and cannot be reversed because the previous excess values are not retained.
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

    -- The service table is the source of truth for the base fee. Callers only
    -- supply the explicit distance, urgency, weight or complexity surcharge.
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
    IF p_credit_score IS NULL OR p_credit_score < 0 OR p_credit_score > 100 THEN
        RAISE_APPLICATION_ERROR(-20044, 'Credit score must be between zero and 100.');
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

/*
  Normalize existing data before tightening the credit-score constraint.
  Back up rows above 100 and pause review/complaint writes before deployment.
*/
UPDATE runners
   SET credit_score = 100
 WHERE credit_score > 100;

COMMIT;

ALTER TABLE runners DROP CONSTRAINT ck_runners_credit;

ALTER TABLE runners ADD CONSTRAINT ck_runners_credit
    CHECK (credit_score BETWEEN 0 AND 100);

COMMENT ON COLUMN runners.credit_score IS
    '信誉分，范围0至100，默认100';

SELECT 'Member 4 functions and credit-score rules created. Run 05_test.sql next.'
       AS deployment_result
  FROM dual;
