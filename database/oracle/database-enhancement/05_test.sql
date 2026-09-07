/*
  Database-enhancement verification script.

  Execute as APPUSER after the numbered creation scripts. Sections 1-8 verify
  views, sections 9-11 verify procedures, and sections 12 onward verify member
  4 business functions and the credit-score constraint. Enable DBMS Output in
  DBeaver and execute this file as a script (Alt+X).
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


/* 9. Member 3: 测试用户封禁与解封过程 */
SET SERVEROUTPUT ON;
DECLARE
    v_msg VARCHAR2(200);
BEGIN
    sp_manage_account_status(41, 'BLOCK', v_msg);
    DBMS_OUTPUT.PUT_LINE('封禁测试结果: ' || v_msg);
    sp_manage_account_status(41, 'UNBLOCK', v_msg);
    DBMS_OUTPUT.PUT_LINE('解封测试结果: ' || v_msg);
END;
/

/* 10. Member 3: 测试设置默认地址过程 */
DECLARE
    v_msg VARCHAR2(200);
BEGIN
    sp_set_default_address(41, 1, v_msg);
    DBMS_OUTPUT.PUT_LINE('设置默认地址结果: ' || v_msg);
END;
/

/* 11. Member 3: 测试配送员审核过程 */
DECLARE
    v_msg VARCHAR2(200);
BEGIN
    sp_audit_runner(485, 'APPROVED', v_msg);
    DBMS_OUTPUT.PUT_LINE('审核测试结果: ' || v_msg);
END;
/

/* 12. Member 4: all three functions must be VALID. */
SELECT object_name, object_type, status
  FROM user_objects
 WHERE object_type = 'FUNCTION'
   AND object_name IN (
       'FN_CALCULATE_TASK_PRICE',
       'FN_RUNNER_CAN_ACCEPT_TASK',
       'FN_GET_CREDIT_LEVEL'
   )
 ORDER BY object_name;

/* Must return no rows. */
SELECT name, type, line, position, text
  FROM user_errors
 WHERE type = 'FUNCTION'
   AND name IN (
       'FN_CALCULATE_TASK_PRICE',
       'FN_RUNNER_CAN_ACCEPT_TASK',
       'FN_GET_CREDIT_LEVEL'
   )
 ORDER BY name, sequence;

/* 13. Member 4: configured base fees and caller-supplied surcharge. */
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

/* 14. Member 4: a disabled service type must be rejected when one exists. */
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

/* 15. Member 4: invalid price inputs and oversized results are rejected. */
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

/* 16. Member 4: runner eligibility samples and independent comparison. */
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

/* 17. Member 4: credit-level boundaries. */
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
  )
 ORDER BY score;

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

    BEGIN
        v_result := fn_get_credit_level(101);
        RAISE_APPLICATION_ERROR(-20998, 'Credit above 100 was unexpectedly accepted.');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE != -20044 THEN
                RAISE;
            END IF;
            DBMS_OUTPUT.PUT_LINE('PASS credit upper-bound check: ' || SQLERRM);
    END;
END;
/

/* 18. Member 4: credit scores and constraint must both enforce 0..100. */
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
  19. Member 4: complaint-style credit updates stay inside 0..100.

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
