/*
  Member 9 view verification script.

  Execute after 01_views.sql. These queries are read-only and can be used by
  the integration/test member to capture SQL result screenshots.
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