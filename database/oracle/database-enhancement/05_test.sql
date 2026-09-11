/*
  Database-enhancement verification script.

  Execute as APPUSER after the numbered creation scripts. Sections 1-8 verify
  views, sections 9-11 verify the original procedures, section 12 verifies the
  atomic acceptance procedure, sections 13-18 verify member 4 business
  functions and the credit-score constraint, and section 19 verifies member 2
  trigger deployment. Enable DBMS Output in
  DBeaver and execute this file as a script (Alt+X).

  Procedure write tests use dynamic fixtures and roll back to savepoints. They
  leave no committed test data, but should still run during a quiet maintenance
  window because their FOR UPDATE locks can briefly block application writes.
*/

/* 1. Check whether all member 9 views are valid. */
SELECT object_name, object_type, status
  FROM user_objects
 WHERE object_type = 'VIEW'
   AND object_name IN (
       'VW_TASK_OVERVIEW',
       'VW_PAYMENT_REFUND_OVERVIEW',
       'VW_RUNNER_PERFORMANCE',
       'VW_SETTLEMENT_REPORT'
   )
 ORDER BY object_name;

/* 2. Row count smoke test. */
SELECT 'VW_TASK_OVERVIEW' AS view_name, COUNT(*) AS row_count FROM vw_task_overview
UNION ALL
SELECT 'VW_PAYMENT_REFUND_OVERVIEW', COUNT(*) FROM vw_payment_refund_overview
UNION ALL
SELECT 'VW_RUNNER_PERFORMANCE', COUNT(*) FROM vw_runner_performance
UNION ALL
SELECT 'VW_SETTLEMENT_REPORT', COUNT(*) FROM vw_settlement_report;

/* 3. Task overview sample. */
SELECT task_id,
       task_title,
       task_status,
       service_name,
       node_name,
       publisher_username,
       runner_name,
       pay_status,
       refund_status,
       complaint_count
  FROM vw_task_overview
 ORDER BY created_at DESC, task_id DESC
 FETCH FIRST 10 ROWS ONLY;

/* 4. Payment and refund overview sample. */
SELECT payment_id,
       task_id,
       task_title,
       publisher_username,
       runner_name,
       pay_amount,
       pay_status,
       latest_refund_id,
       latest_refund_status,
       is_settled
  FROM vw_payment_refund_overview
 ORDER BY payment_id DESC
 FETCH FIRST 10 ROWS ONLY;

/* 5. Runner performance sample. */
SELECT runner_id,
       real_name,
       audit_status,
       work_status,
       credit_score,
       finished_task_count,
       paid_amount,
       average_rating,
       complaint_count,
       settled_net_income,
       candidate_payment_count,
       estimated_net_income
  FROM vw_runner_performance
 ORDER BY finished_task_count DESC, paid_amount DESC, runner_id
 FETCH FIRST 10 ROWS ONLY;

/* 6. Settlement report sample. */
SELECT settlement_id,
       runner_id,
       runner_name,
       settlement_status,
       order_total,
       platform_fee,
       net_income,
       payment_item_count,
       payment_total,
       data_check_result
  FROM vw_settlement_report
 ORDER BY settlement_id DESC
 FETCH FIRST 10 ROWS ONLY;

/* 7. Base table consistency check for task and settlement counts. */
SELECT 'TASK_BASE' AS item_name, COUNT(*) AS item_count FROM tasks
UNION ALL
SELECT 'TASK_VIEW', COUNT(*) FROM vw_task_overview
UNION ALL
SELECT 'SETTLEMENT_BASE', COUNT(*) FROM settlements
UNION ALL
SELECT 'SETTLEMENT_VIEW', COUNT(*) FROM vw_settlement_report;

/* 8. Settlement candidate aggregation by runner. */
SELECT runner_id,
       real_name,
       candidate_payment_count,
       candidate_pay_amount,
       estimated_platform_fee,
       estimated_net_income
  FROM vw_runner_performance
 WHERE candidate_payment_count > 0
 ORDER BY candidate_pay_amount DESC, runner_id;


/* All four backend procedures must be VALID, and USER_ERRORS must be empty. */
SELECT object_name, object_type, status
  FROM user_objects
 WHERE object_type = 'PROCEDURE'
   AND object_name IN (
       'SP_MANAGE_ACCOUNT_STATUS',
       'SP_SET_DEFAULT_ADDRESS',
       'SP_AUDIT_RUNNER',
       'SP_ACCEPT_TASK_ATOMIC'
   )
 ORDER BY object_name;

SELECT name, type, line, position, text
  FROM user_errors
 WHERE type = 'PROCEDURE'
   AND name IN (
       'SP_MANAGE_ACCOUNT_STATUS',
       'SP_SET_DEFAULT_ADDRESS',
       'SP_AUDIT_RUNNER',
       'SP_ACCEPT_TASK_ATOMIC'
   )
 ORDER BY name, sequence;

/* 9. Account block/unblock procedure: linked runner goes offline. */
SET SERVEROUTPUT ON;
DECLARE
    v_user_id       users.user_id%TYPE;
    v_result        VARCHAR2(40);
    v_account_state users.account_status%TYPE;
    v_online_runner_count PLS_INTEGER;
BEGIN
    SELECT MIN(u.user_id)
      INTO v_user_id
      FROM users u
     WHERE u.user_role IN ('USER', 'RUNNER')
       AND u.account_status = 'NORMAL'
       AND EXISTS (
           SELECT 1
             FROM runners r
            WHERE r.user_id = u.user_id
       );

    IF v_user_id IS NULL THEN
        DBMS_OUTPUT.PUT_LINE('SKIP account-status procedure check: no manageable NORMAL runner account.');
    ELSE
        SAVEPOINT before_account_status_test;
        BEGIN
            sp_manage_account_status(v_user_id, 'BLOCK', v_result);
            SELECT account_status INTO v_account_state FROM users WHERE user_id = v_user_id;
            SELECT COUNT(*)
              INTO v_online_runner_count
              FROM runners
             WHERE user_id = v_user_id
               AND work_status <> 'OFFLINE';

            IF v_result <> 'SUCCESS'
               OR v_account_state <> 'BLOCKED'
               OR v_online_runner_count <> 0 THEN
                RAISE_APPLICATION_ERROR(-20970, 'Account BLOCK procedure verification failed.');
            END IF;

            sp_manage_account_status(v_user_id, 'UNBLOCK', v_result);
            SELECT account_status INTO v_account_state FROM users WHERE user_id = v_user_id;
            IF v_result <> 'SUCCESS' OR v_account_state <> 'NORMAL' THEN
                RAISE_APPLICATION_ERROR(-20971, 'Account UNBLOCK procedure verification failed.');
            END IF;

            DBMS_OUTPUT.PUT_LINE('PASS account BLOCK/UNBLOCK procedure check.');
            ROLLBACK TO before_account_status_test;
        EXCEPTION
            WHEN OTHERS THEN
                ROLLBACK TO before_account_status_test;
                RAISE;
        END;
    END IF;
END;
/

/* 10. Default-address procedure: exactly one selected address becomes default. */
DECLARE
    v_user_id       user_addresses.user_id%TYPE;
    v_address_no    user_addresses.address_no%TYPE;
    v_result        VARCHAR2(40);
    v_default_count PLS_INTEGER;
BEGIN
    BEGIN
        SELECT user_id, address_no
          INTO v_user_id, v_address_no
          FROM (
              SELECT user_id, address_no
                FROM user_addresses
               WHERE is_default = 'N'
               ORDER BY user_id, address_no
          )
         WHERE ROWNUM = 1;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_user_id := NULL;
    END;

    IF v_user_id IS NULL THEN
        DBMS_OUTPUT.PUT_LINE('SKIP default-address procedure check: no non-default address exists.');
    ELSE
        SAVEPOINT before_default_address_test;
        BEGIN
            sp_set_default_address(v_user_id, v_address_no, v_result);

            SELECT COUNT(*)
              INTO v_default_count
              FROM user_addresses
             WHERE user_id = v_user_id
               AND is_default = 'Y';

            IF v_result <> 'SUCCESS' OR v_default_count <> 1 THEN
                RAISE_APPLICATION_ERROR(-20972, 'Default-address procedure verification failed.');
            END IF;

            sp_set_default_address(v_user_id, v_address_no, v_result);
            IF v_result <> 'ALREADY_DEFAULT' THEN
                RAISE_APPLICATION_ERROR(-20973, 'Default-address repeat-call check failed.');
            END IF;

            DBMS_OUTPUT.PUT_LINE('PASS default-address procedure check.');
            ROLLBACK TO before_default_address_test;
        EXCEPTION
            WHEN OTHERS THEN
                ROLLBACK TO before_default_address_test;
                RAISE;
        END;
    END IF;
END;
/

/* 11. Runner-audit procedure: approval/rejection linkage and repeat guard. */
DECLARE
    v_runner_id   runners.runner_id%TYPE;
    v_user_id     runners.user_id%TYPE;
    v_result      VARCHAR2(40);
    v_audit_state runners.audit_status%TYPE;
    v_work_state  runners.work_status%TYPE;
    v_user_role   users.user_role%TYPE;
BEGIN
    BEGIN
        SELECT runner_id, user_id
          INTO v_runner_id, v_user_id
          FROM (
              SELECT r.runner_id, r.user_id
                FROM runners r
                JOIN users u ON u.user_id = r.user_id
               WHERE r.audit_status = 'PENDING'
                 AND u.account_status = 'NORMAL'
                 AND u.user_role <> 'ADMIN'
               ORDER BY r.runner_id
          )
         WHERE ROWNUM = 1;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_runner_id := NULL;
    END;

    IF v_runner_id IS NULL THEN
        DBMS_OUTPUT.PUT_LINE('SKIP runner-audit procedure check: no eligible PENDING application.');
    ELSE
        SAVEPOINT before_runner_audit_test;
        BEGIN
            sp_audit_runner(v_runner_id, 'APPROVED', v_result);
            SELECT audit_status, work_status
              INTO v_audit_state, v_work_state
              FROM runners
             WHERE runner_id = v_runner_id;
            SELECT user_role INTO v_user_role FROM users WHERE user_id = v_user_id;

            IF v_result <> 'SUCCESS'
               OR v_audit_state <> 'APPROVED'
               OR v_work_state <> 'FREE'
               OR v_user_role <> 'RUNNER' THEN
                RAISE_APPLICATION_ERROR(-20974, 'Runner approval procedure verification failed.');
            END IF;

            sp_audit_runner(v_runner_id, 'APPROVED', v_result);
            IF v_result <> 'ALREADY_REVIEWED' THEN
                RAISE_APPLICATION_ERROR(-20975, 'Runner repeat-review guard failed.');
            END IF;

            ROLLBACK TO before_runner_audit_test;

            sp_audit_runner(v_runner_id, 'REJECTED', v_result);
            SELECT audit_status, work_status
              INTO v_audit_state, v_work_state
              FROM runners
             WHERE runner_id = v_runner_id;
            IF v_result <> 'SUCCESS'
               OR v_audit_state <> 'REJECTED'
               OR v_work_state <> 'OFFLINE' THEN
                RAISE_APPLICATION_ERROR(-20976, 'Runner rejection procedure verification failed.');
            END IF;

            DBMS_OUTPUT.PUT_LINE('PASS runner approval/rejection procedure check.');
            ROLLBACK TO before_runner_audit_test;
        EXCEPTION
            WHEN OTHERS THEN
                ROLLBACK TO before_runner_audit_test;
                RAISE;
        END;
    END IF;
END;
/

/*
  12. Member 4: one transaction may create only one acceptance for a task.

  This rollback-only functional check verifies the procedure contract and the
  repeated-call guard. The two-session blocking behavior is covered by the
  backend concurrency regression and should also be rerun in an isolated Oracle
  database before shared deployment.
*/
DECLARE
    v_runner_id         runners.runner_id%TYPE;
    v_runner_user_id    runners.user_id%TYPE;
    v_task_id           tasks.task_id%TYPE;
    v_first_result      VARCHAR2(40);
    v_second_result     VARCHAR2(40);
    v_first_record_id   assign_records.record_id%TYPE;
    v_second_record_id  assign_records.record_id%TYPE;
BEGIN
    sp_accept_task_atomic(
        NULL,
        NULL,
        NULL,
        NULL,
        v_first_result,
        v_first_record_id);

    IF v_first_result <> 'INVALID_OPERATION' OR v_first_record_id IS NOT NULL THEN
        RAISE_APPLICATION_ERROR(
            -20983,
            'NULL operation type was not rejected: ' || v_first_result);
    END IF;

    BEGIN
        SELECT runner_id, runner_user_id, task_id
          INTO v_runner_id, v_runner_user_id, v_task_id
          FROM (
              SELECT r.runner_id,
                     r.user_id AS runner_user_id,
                     t.task_id
                FROM runners r
                JOIN users u ON u.user_id = r.user_id
               CROSS JOIN tasks t
               WHERE u.user_role = 'RUNNER'
                 AND u.account_status = 'NORMAL'
                 AND r.audit_status = 'APPROVED'
                 AND r.work_status IN ('FREE', 'BUSY')
                 AND t.task_status = 'WAITING'
                 AND t.publisher_user_id <> r.user_id
               ORDER BY t.task_id, r.runner_id
          )
         WHERE ROWNUM = 1;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_task_id := NULL;
    END;

    IF v_task_id IS NULL THEN
        DBMS_OUTPUT.PUT_LINE(
            'SKIP atomic acceptance functional check: no eligible runner/task pair exists.');
    ELSE
        SAVEPOINT before_atomic_acceptance_test;
        BEGIN
            sp_accept_task_atomic(
                v_task_id,
                v_runner_id,
                v_runner_user_id,
                'SELF',
                v_first_result,
                v_first_record_id);

            sp_accept_task_atomic(
                v_task_id,
                v_runner_id,
                v_runner_user_id,
                'SELF',
                v_second_result,
                v_second_record_id);

            IF v_first_result <> 'SUCCESS' OR v_first_record_id IS NULL THEN
                RAISE_APPLICATION_ERROR(
                    -20984,
                    'First atomic acceptance did not succeed: ' || v_first_result);
            END IF;

            IF v_second_result <> 'TASK_NOT_WAITING' OR v_second_record_id IS NOT NULL THEN
                RAISE_APPLICATION_ERROR(
                    -20985,
                    'Repeated atomic acceptance was not rejected: ' || v_second_result);
            END IF;

            DBMS_OUTPUT.PUT_LINE(
                'PASS atomic acceptance: first call succeeded and repeated call was rejected.');
            ROLLBACK TO before_atomic_acceptance_test;
        EXCEPTION
            WHEN OTHERS THEN
                ROLLBACK TO before_atomic_acceptance_test;
                RAISE;
        END;
    END IF;
END;
/

/* 13. Member 4: all three functions must be VALID. */
SELECT object_name, object_type, status
  FROM user_objects
 WHERE object_type = 'FUNCTION'
   AND object_name IN (
       'FN_CALCULATE_TASK_PRICE',
       'FN_RUNNER_CAN_ACCEPT_TASK',
       'FN_SERVICE_NODE_ALLOWED'
   )
 ORDER BY object_name;

/* Must return no rows. */
SELECT name, type, line, position, text
  FROM user_errors
 WHERE type = 'FUNCTION'
   AND name IN (
       'FN_CALCULATE_TASK_PRICE',
       'FN_RUNNER_CAN_ACCEPT_TASK',
       'FN_SERVICE_NODE_ALLOWED'
   )
 ORDER BY name, sequence;

/* The removed credit-level function must not remain from an earlier deployment. */
DECLARE
    v_legacy_function_count PLS_INTEGER;
BEGIN
    SELECT COUNT(*)
      INTO v_legacy_function_count
      FROM user_objects
     WHERE object_type = 'FUNCTION'
       AND object_name = 'FN_GET_CREDIT_LEVEL';

    IF v_legacy_function_count <> 0 THEN
        RAISE_APPLICATION_ERROR(-20992, 'FN_GET_CREDIT_LEVEL still exists.');
    END IF;
END;
/

/* 14. Member 4: configured base fees and caller-supplied surcharge. */
SELECT service_type_id,
       service_name,
       base_price,
       fn_calculate_task_price(service_type_id) AS base_result,
       fn_calculate_task_price(service_type_id, 2.50) AS price_with_extra
  FROM service_types
 WHERE type_status = 'ENABLED'
 ORDER BY service_type_id;

DECLARE
    v_mismatch_count NUMBER;
BEGIN
    WITH expected_prices (service_name, base_price, price_with_extra) AS (
        SELECT '外卖分发', 3.00, 5.50 FROM dual
        UNION ALL
        SELECT '快递代取', 4.00, 6.50 FROM dual
        UNION ALL
        SELECT '私人跑腿', 5.00, 7.50 FROM dual
    )
    SELECT COUNT(*)
      INTO v_mismatch_count
      FROM expected_prices expected
      LEFT JOIN service_types actual
        ON actual.service_name = expected.service_name
     WHERE actual.service_type_id IS NULL
        OR actual.type_status <> 'ENABLED'
        OR actual.base_price <> expected.base_price
        OR CASE
               WHEN actual.service_type_id IS NULL THEN 1
               WHEN fn_calculate_task_price(actual.service_type_id) <> expected.base_price THEN 1
               WHEN fn_calculate_task_price(actual.service_type_id, 2.50) <> expected.price_with_extra THEN 1
               ELSE 0
           END = 1;

    IF v_mismatch_count <> 0 THEN
        RAISE_APPLICATION_ERROR(
            -20990,
            'Baseline price check failed: expected enabled service prices 3/4/5 and surcharge results 5.50/6.50/7.50.');
    END IF;

    DBMS_OUTPUT.PUT_LINE('PASS baseline price check: 3/4/5 plus caller surcharge.');
END;
/

/* 15. Member 4: a disabled service type must be rejected when one exists. */
DECLARE
    v_disabled_service_type_id service_types.service_type_id%TYPE;
    v_result                   NUMBER;
BEGIN
    SELECT MIN(service_type_id)
      INTO v_disabled_service_type_id
      FROM service_types
     WHERE type_status = 'DISABLED';

    IF v_disabled_service_type_id IS NULL THEN
        DBMS_OUTPUT.PUT_LINE('SKIP disabled service-type check: no disabled service type exists.');
    ELSE
        BEGIN
            v_result := fn_calculate_task_price(v_disabled_service_type_id, 0);
            RAISE_APPLICATION_ERROR(-20999, 'Disabled service type was unexpectedly accepted.');
        EXCEPTION
            WHEN OTHERS THEN
                IF SQLCODE != -20043 THEN
                    RAISE;
                END IF;
                DBMS_OUTPUT.PUT_LINE('PASS disabled service-type check: ' || SQLERRM);
        END;
    END IF;
END;
/

/* 16. Member 4: invalid price inputs and oversized results are rejected. */
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

    BEGIN
        v_result := fn_calculate_task_price(NULL, 0);
        RAISE_APPLICATION_ERROR(-20995, 'NULL service type id was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20041 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS NULL service-type check: ' || SQLERRM);
    END;

    BEGIN
        v_result := fn_calculate_task_price(-1, 0);
        RAISE_APPLICATION_ERROR(-20996, 'Missing service type was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20043 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS missing service-type check: ' || SQLERRM);
    END;

    BEGIN
        v_result := fn_calculate_task_price(v_service_type_id, NULL);
        RAISE_APPLICATION_ERROR(-20997, 'NULL surcharge was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20042 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS NULL-surcharge check: ' || SQLERRM);
    END;

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

/* 17. Member 4: runner eligibility samples and independent comparison. */
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

/* Must return no rows. */
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

SELECT fn_runner_can_accept_task(NULL, NULL) AS null_result,
       fn_runner_can_accept_task(-1, -1) AS missing_result
  FROM dual;

/* 18. Member 4: service-node eligibility must match the underlying rules. */
SELECT st.service_type_id,
       st.service_name,
       n.node_id,
       n.node_name,
       CASE
           WHEN st.type_status = 'ENABLED'
            AND n.node_status = 'NORMAL'
            AND EXISTS (
                SELECT 1
                  FROM service_node_rules snr
                 WHERE snr.service_type_id = st.service_type_id
                   AND snr.node_id = n.node_id
            )
           THEN 1 ELSE 0
       END AS expected_result,
       fn_service_node_allowed(st.service_type_id, n.node_id) AS actual_result
  FROM service_types st
 CROSS JOIN nodes n
 ORDER BY st.service_type_id, n.node_id;

DECLARE
    v_mismatch_count PLS_INTEGER;
BEGIN
    SELECT COUNT(*)
      INTO v_mismatch_count
      FROM service_types st
      CROSS JOIN nodes n
     WHERE fn_service_node_allowed(st.service_type_id, n.node_id) <>
           CASE
               WHEN st.type_status = 'ENABLED'
                AND n.node_status = 'NORMAL'
                AND EXISTS (
                    SELECT 1
                      FROM service_node_rules snr
                     WHERE snr.service_type_id = st.service_type_id
                       AND snr.node_id = n.node_id
                )
               THEN 1 ELSE 0
           END;

    IF v_mismatch_count <> 0 THEN
        RAISE_APPLICATION_ERROR(-20993, 'FN_SERVICE_NODE_ALLOWED disagrees with base tables.');
    END IF;
END;
/

SELECT fn_service_node_allowed(NULL, NULL) AS null_result,
       fn_service_node_allowed(-1, -1) AS missing_result
  FROM dual;

/* 19. Member 4: credit scores and constraint must both enforce 0..100. */
SELECT COUNT(*) AS out_of_range_credit_count
  FROM runners
 WHERE credit_score < 0
    OR credit_score > 100;

SELECT constraint_name,
       status,
       validated,
       search_condition_vc
  FROM user_constraints
 WHERE table_name = 'RUNNERS'
   AND constraint_name = 'CK_RUNNERS_CREDIT'
   AND constraint_type = 'C';

DECLARE
    v_invalid_credit_count  NUMBER;
    v_valid_constraint_count NUMBER;
BEGIN
    SELECT COUNT(*)
      INTO v_invalid_credit_count
      FROM runners
     WHERE credit_score < 0
        OR credit_score > 100;

    SELECT COUNT(*)
      INTO v_valid_constraint_count
      FROM user_constraints
     WHERE table_name = 'RUNNERS'
       AND constraint_name = 'CK_RUNNERS_CREDIT'
       AND constraint_type = 'C'
       AND status = 'ENABLED'
       AND validated = 'VALIDATED'
       AND REGEXP_LIKE(
           search_condition_vc,
           'CREDIT_SCORE[[:space:]]+BETWEEN[[:space:]]+0[[:space:]]+AND[[:space:]]+100',
           'i');

    IF v_invalid_credit_count <> 0 OR v_valid_constraint_count <> 1 THEN
        RAISE_APPLICATION_ERROR(-20989, 'Credit-score range verification failed.');
    END IF;

    DBMS_OUTPUT.PUT_LINE('PASS credit-score range and constraint check.');
END;
/

/* Must return no rows. */
SELECT runner_id, credit_score
  FROM runners
 WHERE credit_score NOT BETWEEN 0 AND 100
 ORDER BY runner_id;

/*
  20. Member 4: complaint-style credit updates stay inside 0..100.

  This uses the same LEAST/GREATEST expression as ComplaintRepository. A
  savepoint restores the selected runner's original score before the block
  exits, so the verification leaves no test data behind.
*/
DECLARE
    v_runner_id    runners.runner_id%TYPE;
    v_credit_score runners.credit_score%TYPE;
BEGIN
    SAVEPOINT complaint_credit_boundary_test;

    SELECT MIN(runner_id)
      INTO v_runner_id
      FROM runners;

    IF v_runner_id IS NULL THEN
        DBMS_OUTPUT.PUT_LINE('SKIP complaint credit-boundary check: no runner exists.');
    ELSE
        UPDATE runners
           SET credit_score = 5
         WHERE runner_id = v_runner_id;

        UPDATE runners
           SET credit_score = LEAST(100, GREATEST(0, credit_score - 10))
         WHERE runner_id = v_runner_id;

        SELECT credit_score
          INTO v_credit_score
          FROM runners
         WHERE runner_id = v_runner_id;

        IF v_credit_score <> 0 THEN
            RAISE_APPLICATION_ERROR(
                -20988,
                'Complaint lower-bound check failed: expected 5 - 10 to be clamped to 0.');
        END IF;

        UPDATE runners
           SET credit_score = LEAST(100, GREATEST(0, credit_score - 10))
         WHERE runner_id = v_runner_id;

        SELECT credit_score
          INTO v_credit_score
          FROM runners
         WHERE runner_id = v_runner_id;

        IF v_credit_score <> 0 THEN
            RAISE_APPLICATION_ERROR(
                -20987,
                'Complaint repeated-penalty check failed: expected 0 - 10 to remain 0.');
        END IF;

        UPDATE runners
           SET credit_score = 99
         WHERE runner_id = v_runner_id;

        UPDATE runners
           SET credit_score = LEAST(100, GREATEST(0, credit_score + 10))
         WHERE runner_id = v_runner_id;

        SELECT credit_score
          INTO v_credit_score
          FROM runners
         WHERE runner_id = v_runner_id;

        IF v_credit_score <> 100 THEN
            RAISE_APPLICATION_ERROR(
                -20986,
                'Credit upper-bound check failed: expected 99 + 10 to be clamped to 100.');
        END IF;

        DBMS_OUTPUT.PUT_LINE(
            'PASS complaint credit-boundary check: penalties stop at 0 and updates stop at 100.');
    END IF;

    ROLLBACK TO complaint_credit_boundary_test;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK TO complaint_credit_boundary_test;
        RAISE;
END;
/

/*
  19. Member 2: automatic audit triggers must exist and compile cleanly.

  Behavioral tests are executed in an isolated personal schema because firing
  these triggers creates audit rows. See member2_triggers.md for the verified
  transition matrix and the September 11, 2026 isolated test result.
*/
SELECT trigger_name, triggering_event, table_name, status
  FROM user_triggers
 WHERE trigger_name IN (
       'TRG_TASK_STATUS_AUDIT',
       'TRG_PAYMENT_CHANGE_AUDIT',
       'TRG_REFUND_CHANGE_AUDIT'
 )
 ORDER BY trigger_name;

/* Must return no rows. */
SELECT name, type, line, position, text
  FROM user_errors
 WHERE type = 'TRIGGER'
   AND name IN (
       'TRG_TASK_STATUS_AUDIT',
       'TRG_PAYMENT_CHANGE_AUDIT',
       'TRG_REFUND_CHANGE_AUDIT'
   )
 ORDER BY name, sequence;
