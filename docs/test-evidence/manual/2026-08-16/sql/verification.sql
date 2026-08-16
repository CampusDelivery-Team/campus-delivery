-- 截图证据包只读核验脚本
-- 使用方法：在 SQL Developer 中连接隔离测试库，将 :变量 替换为本次页面生成的 ID。
-- 本文件只执行 SELECT，不直接修改业务状态。

-- ENV-04：数据库版本与当前用户
SELECT banner FROM v$version WHERE banner LIKE 'Oracle%';
SELECT USER AS current_user FROM dual;

-- FLOW-01：登录账号状态（不要截图 password_hash）
SELECT user_id, username, user_role, account_status
FROM APPUSER.users
WHERE username = :username;

-- FLOW-02：地址及唯一默认地址
SELECT user_id, address_no, contact_name, address_detail, is_default
FROM APPUSER.user_addresses
WHERE user_id = :user_id
ORDER BY address_no;

SELECT user_id, COUNT(*) AS default_count
FROM APPUSER.user_addresses
WHERE user_id = :user_id AND is_default = 'Y'
GROUP BY user_id;

-- FLOW-03 / FLOW-04：资格申请与角色同步
SELECT r.runner_id, r.user_id, r.audit_status, r.work_status, u.user_role
FROM APPUSER.runners r
JOIN APPUSER.users u ON u.user_id = r.user_id
WHERE r.user_id = :user_id;

-- FLOW-05：服务、节点与适用规则
SELECT n.node_id, n.node_name, n.node_status,
       st.service_type_id, st.service_name, st.type_status
FROM APPUSER.service_node_rules snr
JOIN APPUSER.nodes n ON n.node_id = snr.node_id
JOIN APPUSER.service_types st ON st.service_type_id = snr.service_type_id
WHERE n.node_id = :node_id OR st.service_type_id = :service_type_id
ORDER BY n.node_id, st.service_type_id;

-- FLOW-06 / FLOW-07 / FLOW-08：三类任务及专属明细
SELECT task_id, publisher_user_id, service_type_id, task_title,
       task_price, urgent_flag, task_status, created_at
FROM APPUSER.tasks
WHERE task_id = :task_id;

SELECT * FROM APPUSER.food_delivery_details WHERE task_id = :task_id;
SELECT * FROM APPUSER.express_pickup_details WHERE task_id = :task_id;
SELECT * FROM APPUSER.private_task_details WHERE task_id = :task_id;

-- FLOW-09：任务取消
SELECT task_id, task_status, completed_at
FROM APPUSER.tasks
WHERE task_id = :task_id;

-- FLOW-10 / FLOW-11 / FLOW-12 / FLOW-13：接派、并发和状态日志
SELECT task_id, task_status FROM APPUSER.tasks WHERE task_id = :task_id;

SELECT record_id, task_id, runner_id, operation_type, assigned_at
FROM APPUSER.assign_records
WHERE task_id = :task_id
ORDER BY record_id;

SELECT COUNT(*) AS assign_count,
       COUNT(DISTINCT runner_id) AS runner_count
FROM APPUSER.assign_records
WHERE task_id = :task_id;

SELECT l.status_before, l.status_after, l.operator_user_id, l.operated_at
FROM APPUSER.task_status_logs l
JOIN APPUSER.assign_records ar ON ar.record_id = l.record_id
WHERE ar.task_id = :task_id
ORDER BY l.log_id;

SELECT runner_id, audit_status, work_status
FROM APPUSER.runners
WHERE runner_id IN (:runner_id_1, :runner_id_2)
ORDER BY runner_id;

-- FLOW-14：确认收货与支付
SELECT t.task_id, t.task_status, p.payment_id, p.pay_amount,
       p.pay_method, p.pay_status, p.paid_at
FROM APPUSER.tasks t
JOIN APPUSER.assign_records ar ON ar.task_id = t.task_id
LEFT JOIN APPUSER.payments p ON p.record_id = ar.record_id
WHERE t.task_id = :task_id;

-- FLOW-15：评价和信誉分变化
SELECT rv.review_id, rv.task_id, rv.record_id, rv.rating,
       rv.credit_delta, rv.reviewed_at, r.credit_score
FROM APPUSER.reviews rv
JOIN APPUSER.assign_records ar ON ar.record_id = rv.record_id
JOIN APPUSER.runners r ON r.runner_id = ar.runner_id
WHERE rv.task_id = :task_id;

-- FLOW-16：投诉
SELECT c.complaint_id, c.record_id, c.process_status,
       c.process_result, c.submitted_at, c.processed_at
FROM APPUSER.complaints c
JOIN APPUSER.assign_records ar ON ar.record_id = c.record_id
WHERE ar.task_id = :task_id;

-- FLOW-17：退款和支付联动
SELECT rf.refund_id, rf.payment_id, rf.refund_amount,
       rf.process_status, p.pay_status, t.task_status
FROM APPUSER.refunds rf
JOIN APPUSER.payments p ON p.payment_id = rf.payment_id
JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
JOIN APPUSER.tasks t ON t.task_id = ar.task_id
WHERE t.task_id = :task_id;

-- FLOW-18：结算及支付明细
SELECT settlement_id, runner_id, order_total, platform_fee,
       net_income, settlement_status, created_at
FROM APPUSER.settlements
WHERE settlement_id = :settlement_id;

SELECT settlement_id, payment_id
FROM APPUSER.settlement_payment_items
WHERE settlement_id = :settlement_id
ORDER BY payment_id;

-- FLOW-19：审计主表和关联明细
SELECT audit_id, audit_object, audit_result, audited_at, exception_note
FROM APPUSER.audit_logs
WHERE audit_id = :audit_id;

SELECT * FROM APPUSER.audit_payment_checks WHERE audit_id = :audit_id;
SELECT * FROM APPUSER.audit_refund_checks WHERE audit_id = :audit_id;
SELECT * FROM APPUSER.audit_status_log_checks WHERE audit_id = :audit_id;

-- FLOW-20：报表及审计关联
SELECT report_id, report_type, stat_period, report_status, generated_at
FROM APPUSER.reports
WHERE report_id = :report_id;

SELECT report_id, audit_id
FROM APPUSER.report_audit_items
WHERE report_id = :report_id
ORDER BY audit_id;
