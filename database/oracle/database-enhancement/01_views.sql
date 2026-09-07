/*
  Member 9 database enhancement: business views.

  Execute as APPUSER for final delivery, or execute as a personal schema user
  for debugging after APPUSER base-table SELECT privileges are granted.
  These views are read-only query entrances for pages, reports and DB demo.
  They do not replace C# service-layer writes, transactions, locks or permissions.
*/

CREATE OR REPLACE VIEW vw_task_overview AS
WITH latest_assign AS (
    SELECT record_id,
           task_id,
           runner_id,
           operation_type,
           assigned_at,
           reassign_reason
      FROM (
          SELECT ar.*,
                 ROW_NUMBER() OVER (
                     PARTITION BY ar.task_id
                     ORDER BY ar.assigned_at DESC, ar.record_id DESC
                 ) AS rn
            FROM APPUSER.assign_records ar
      )
     WHERE rn = 1
),
latest_payment AS (
    SELECT payment_id,
           record_id,
           order_amount,
           pay_amount,
           pay_method,
           third_trade_no,
           pay_status
      FROM (
          SELECT p.*,
                 ROW_NUMBER() OVER (
                     PARTITION BY p.record_id
                     ORDER BY p.payment_id DESC
                 ) AS rn
            FROM APPUSER.payments p
      )
     WHERE rn = 1
),
latest_refund AS (
    SELECT refund_id,
           payment_id,
           refund_amount,
           refund_reason,
           approved_amount,
           process_status
      FROM (
          SELECT rf.*,
                 ROW_NUMBER() OVER (
                     PARTITION BY rf.payment_id
                     ORDER BY rf.refund_id DESC
                 ) AS rn
            FROM APPUSER.refunds rf
      )
     WHERE rn = 1
),
complaint_stats AS (
    SELECT c.record_id,
           COUNT(*) AS complaint_count,
           SUM(CASE WHEN c.process_status IN ('SUBMITTED', 'PROCESSING') THEN 1 ELSE 0 END) AS active_complaint_count
      FROM APPUSER.complaints c
     GROUP BY c.record_id
),
status_log_stats AS (
    SELECT l.record_id,
           COUNT(*) AS status_log_count,
           MAX(l.operated_at) AS last_status_operated_at
      FROM APPUSER.task_status_logs l
     GROUP BY l.record_id
)
SELECT t.task_id,
       t.task_title,
       t.task_status,
       t.task_price,
       t.urgent_flag,
       t.created_at,
       t.completed_at,
       t.publisher_user_id,
       publisher.username AS publisher_username,
       publisher.phone AS publisher_phone,
       t.service_type_id,
       st.service_name,
       t.node_id,
       n.node_type,
       n.node_name,
       n.location AS node_location,
       t.address_no,
       ua.contact_name,
       ua.contact_phone,
       ua.campus,
       ua.building_room,
       CASE
           WHEN fd.task_id IS NOT NULL THEN 'FOOD_DELIVERY'
           WHEN ep.task_id IS NOT NULL THEN 'EXPRESS_PICKUP'
           WHEN pt.task_id IS NOT NULL THEN 'PRIVATE_TASK'
           ELSE 'UNKNOWN'
       END AS detail_type,
       CASE
           WHEN fd.task_id IS NOT NULL THEN fd.merchant_name || ' / ' || NVL(fd.platform_order_no, '-') || ' / ' || NVL(fd.pickup_note, '-')
           WHEN ep.task_id IS NOT NULL THEN ep.express_company || ' / ' || ep.waybill_no || ' / ' || ep.pickup_code
           WHEN pt.task_id IS NOT NULL THEN pt.item_category || ' / ' || pt.pickup_location || ' -> ' || pt.delivery_location
           ELSE 'No detail'
       END AS detail_summary,
       la.record_id,
       la.operation_type,
       la.assigned_at,
       la.reassign_reason,
       la.runner_id,
       r.real_name AS runner_name,
       runner_user.username AS runner_username,
       r.audit_status AS runner_audit_status,
       r.work_status AS runner_work_status,
       lp.payment_id,
       lp.order_amount,
       lp.pay_amount,
       lp.pay_method,
       lp.third_trade_no,
       lp.pay_status,
       lr.refund_id,
       lr.refund_amount,
       lr.approved_amount,
       lr.process_status AS refund_status,
       rv.review_id,
       rv.rating,
       rv.reviewed_at,
       NVL(cs.complaint_count, 0) AS complaint_count,
       NVL(cs.active_complaint_count, 0) AS active_complaint_count,
       NVL(sls.status_log_count, 0) AS status_log_count,
       sls.last_status_operated_at
  FROM APPUSER.tasks t
  JOIN APPUSER.users publisher ON publisher.user_id = t.publisher_user_id
  JOIN APPUSER.service_types st ON st.service_type_id = t.service_type_id
  JOIN APPUSER.nodes n ON n.node_id = t.node_id
  JOIN APPUSER.user_addresses ua ON ua.user_id = t.publisher_user_id
                        AND ua.address_no = t.address_no
  LEFT JOIN APPUSER.food_delivery_details fd ON fd.task_id = t.task_id
                                    AND fd.detail_no = 1
  LEFT JOIN APPUSER.express_pickup_details ep ON ep.task_id = t.task_id
                                     AND ep.detail_no = 1
  LEFT JOIN APPUSER.private_task_details pt ON pt.task_id = t.task_id
                                   AND pt.detail_no = 1
  LEFT JOIN latest_assign la ON la.task_id = t.task_id
  LEFT JOIN APPUSER.runners r ON r.runner_id = la.runner_id
  LEFT JOIN APPUSER.users runner_user ON runner_user.user_id = r.user_id
  LEFT JOIN latest_payment lp ON lp.record_id = la.record_id
  LEFT JOIN latest_refund lr ON lr.payment_id = lp.payment_id
  LEFT JOIN APPUSER.reviews rv ON rv.task_id = t.task_id
  LEFT JOIN complaint_stats cs ON cs.record_id = la.record_id
  LEFT JOIN status_log_stats sls ON sls.record_id = la.record_id;

COMMENT ON TABLE vw_task_overview IS 'Task overview view for task details, management query and demo reports.';
COMMENT ON COLUMN vw_task_overview.detail_type IS 'Task detail table type: FOOD_DELIVERY/EXPRESS_PICKUP/PRIVATE_TASK/UNKNOWN.';
COMMENT ON COLUMN vw_task_overview.detail_summary IS 'Compact business detail summary for list and report display.';
COMMENT ON COLUMN vw_task_overview.active_complaint_count IS 'Open complaints that should block normal settlement.';

CREATE OR REPLACE VIEW vw_payment_refund_overview AS
WITH latest_refund AS (
    SELECT refund_id,
           payment_id,
           refund_amount,
           refund_reason,
           approved_amount,
           process_status
      FROM (
          SELECT rf.*,
                 ROW_NUMBER() OVER (
                     PARTITION BY rf.payment_id
                     ORDER BY rf.refund_id DESC
                 ) AS rn
            FROM APPUSER.refunds rf
      )
     WHERE rn = 1
),
refund_stats AS (
    SELECT payment_id,
           COUNT(*) AS refund_count,
           SUM(CASE WHEN process_status IN ('APPLY', 'APPROVED') THEN 1 ELSE 0 END) AS active_refund_count
      FROM APPUSER.refunds
     GROUP BY payment_id
)
SELECT p.payment_id,
       p.record_id,
       ar.task_id,
       t.task_title,
       t.task_status,
       t.publisher_user_id,
       publisher.username AS publisher_username,
       ar.runner_id,
       r.real_name AS runner_name,
       runner_user.username AS runner_username,
       p.order_amount,
       p.pay_amount,
       p.pay_method,
       p.third_trade_no,
       p.pay_status,
       NVL(t.completed_at, t.created_at) AS payment_business_time,
       lr.refund_id AS latest_refund_id,
       lr.refund_amount AS latest_refund_amount,
       lr.refund_reason AS latest_refund_reason,
       lr.approved_amount AS latest_approved_amount,
       lr.process_status AS latest_refund_status,
       NVL(rs.refund_count, 0) AS refund_count,
       NVL(rs.active_refund_count, 0) AS active_refund_count,
       spi.settlement_id,
       CASE WHEN spi.payment_id IS NULL THEN 0 ELSE 1 END AS is_settled
  FROM APPUSER.payments p
  JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
  JOIN APPUSER.tasks t ON t.task_id = ar.task_id
  JOIN APPUSER.users publisher ON publisher.user_id = t.publisher_user_id
  JOIN APPUSER.runners r ON r.runner_id = ar.runner_id
  JOIN APPUSER.users runner_user ON runner_user.user_id = r.user_id
  LEFT JOIN latest_refund lr ON lr.payment_id = p.payment_id
  LEFT JOIN refund_stats rs ON rs.payment_id = p.payment_id
  LEFT JOIN APPUSER.settlement_payment_items spi ON spi.payment_id = p.payment_id;

COMMENT ON TABLE vw_payment_refund_overview IS 'Payment and latest refund overview for payment status, refund review and report query.';
COMMENT ON COLUMN vw_payment_refund_overview.payment_business_time IS 'Report time bucket. The schema has no paid_at column, so task completed_at is used with created_at fallback.';
COMMENT ON COLUMN vw_payment_refund_overview.is_settled IS '1 means this payment has already entered a settlement.';

CREATE OR REPLACE VIEW vw_runner_performance AS
WITH task_stats AS (
    SELECT ar.runner_id,
           COUNT(DISTINCT ar.record_id) AS assign_count,
           COUNT(DISTINCT CASE WHEN t.task_status = 'FINISHED' THEN t.task_id END) AS finished_task_count,
           COUNT(DISTINCT CASE WHEN t.task_status IN ('ASSIGNED', 'PICKED_UP', 'DELIVERING', 'WAIT_CONFIRM') THEN t.task_id END) AS active_task_count,
           COUNT(DISTINCT CASE WHEN p.pay_status = 'PAID' THEN p.payment_id END) AS paid_order_count,
           NVL(SUM(CASE WHEN p.pay_status = 'PAID' THEN p.pay_amount ELSE 0 END), 0) AS paid_amount,
           MAX(ar.assigned_at) AS latest_assigned_at,
           MAX(t.completed_at) AS latest_completed_at
      FROM APPUSER.assign_records ar
      JOIN APPUSER.tasks t ON t.task_id = ar.task_id
      LEFT JOIN APPUSER.payments p ON p.record_id = ar.record_id
     GROUP BY ar.runner_id
),
review_stats AS (
    SELECT ar.runner_id,
           COUNT(*) AS review_count,
           ROUND(AVG(rv.rating), 2) AS average_rating,
           NVL(SUM(rv.credit_delta), 0) AS review_credit_delta
      FROM APPUSER.reviews rv
      JOIN APPUSER.assign_records ar ON ar.record_id = rv.record_id
     GROUP BY ar.runner_id
),
complaint_stats AS (
    SELECT ar.runner_id,
           COUNT(*) AS complaint_count,
           SUM(CASE WHEN c.process_status IN ('SUBMITTED', 'PROCESSING') THEN 1 ELSE 0 END) AS active_complaint_count,
           SUM(CASE WHEN c.process_status = 'DONE' THEN 1 ELSE 0 END) AS finished_complaint_count
      FROM APPUSER.complaints c
      JOIN APPUSER.assign_records ar ON ar.record_id = c.record_id
     GROUP BY ar.runner_id
),
settlement_stats AS (
    SELECT s.runner_id,
           COUNT(*) AS settlement_count,
           SUM(CASE WHEN s.settlement_status = 'WAITING' THEN 1 ELSE 0 END) AS waiting_settlement_count,
           SUM(CASE WHEN s.settlement_status = 'DONE' THEN 1 ELSE 0 END) AS done_settlement_count,
           SUM(CASE WHEN s.settlement_status = 'BLOCKED' THEN 1 ELSE 0 END) AS blocked_settlement_count,
           NVL(SUM(s.order_total), 0) AS settled_order_total,
           NVL(SUM(s.platform_fee), 0) AS settled_platform_fee,
           NVL(SUM(s.net_income), 0) AS settled_net_income
      FROM APPUSER.settlements s
     GROUP BY s.runner_id
),
candidate_stats AS (
    SELECT ar.runner_id,
           COUNT(DISTINCT p.payment_id) AS candidate_payment_count,
           NVL(SUM(p.pay_amount), 0) AS candidate_pay_amount
      FROM APPUSER.payments p
      JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
      JOIN APPUSER.tasks t ON t.task_id = ar.task_id
     WHERE p.pay_status = 'PAID'
       AND t.task_status = 'FINISHED'
       AND NOT EXISTS (
           SELECT 1
             FROM APPUSER.settlement_payment_items spi
            WHERE spi.payment_id = p.payment_id
       )
       AND NOT EXISTS (
           SELECT 1
             FROM APPUSER.complaints c
            WHERE c.record_id = p.record_id
              AND c.process_status IN ('SUBMITTED', 'PROCESSING')
       )
       AND NOT EXISTS (
           SELECT 1
             FROM APPUSER.refunds rf
            WHERE rf.payment_id = p.payment_id
              AND rf.process_status IN ('APPLY', 'APPROVED', 'DONE')
       )
     GROUP BY ar.runner_id
)
SELECT r.runner_id,
       r.user_id,
       u.username,
       u.phone,
       r.real_name,
       r.identity_info,
       r.audit_status,
       r.work_status,
       r.credit_score,
       NVL(ts.assign_count, 0) AS assign_count,
       NVL(ts.finished_task_count, 0) AS finished_task_count,
       NVL(ts.active_task_count, 0) AS active_task_count,
       NVL(ts.paid_order_count, 0) AS paid_order_count,
       NVL(ts.paid_amount, 0) AS paid_amount,
       ts.latest_assigned_at,
       ts.latest_completed_at,
       NVL(rv.review_count, 0) AS review_count,
       NVL(rv.average_rating, 0) AS average_rating,
       NVL(rv.review_credit_delta, 0) AS review_credit_delta,
       NVL(cs.complaint_count, 0) AS complaint_count,
       NVL(cs.active_complaint_count, 0) AS active_complaint_count,
       NVL(cs.finished_complaint_count, 0) AS finished_complaint_count,
       NVL(ss.settlement_count, 0) AS settlement_count,
       NVL(ss.waiting_settlement_count, 0) AS waiting_settlement_count,
       NVL(ss.done_settlement_count, 0) AS done_settlement_count,
       NVL(ss.blocked_settlement_count, 0) AS blocked_settlement_count,
       NVL(ss.settled_order_total, 0) AS settled_order_total,
       NVL(ss.settled_platform_fee, 0) AS settled_platform_fee,
       NVL(ss.settled_net_income, 0) AS settled_net_income,
       NVL(cand.candidate_payment_count, 0) AS candidate_payment_count,
       NVL(cand.candidate_pay_amount, 0) AS candidate_pay_amount,
       ROUND(NVL(cand.candidate_pay_amount, 0) * 0.10, 2) AS estimated_platform_fee,
       ROUND(NVL(cand.candidate_pay_amount, 0) * 0.90, 2) AS estimated_net_income
  FROM APPUSER.runners r
  JOIN APPUSER.users u ON u.user_id = r.user_id
  LEFT JOIN task_stats ts ON ts.runner_id = r.runner_id
  LEFT JOIN review_stats rv ON rv.runner_id = r.runner_id
  LEFT JOIN complaint_stats cs ON cs.runner_id = r.runner_id
  LEFT JOIN settlement_stats ss ON ss.runner_id = r.runner_id
  LEFT JOIN candidate_stats cand ON cand.runner_id = r.runner_id;

COMMENT ON TABLE vw_runner_performance IS 'Runner performance aggregation for admin report, settlement candidate summary and runner profile display.';
COMMENT ON COLUMN vw_runner_performance.candidate_payment_count IS 'Paid and finished payments that are not settled and not blocked by active refund or complaint.';
COMMENT ON COLUMN vw_runner_performance.estimated_net_income IS 'Estimated net income with the current 10 percent platform fee rule.';

CREATE OR REPLACE VIEW vw_settlement_report AS
SELECT s.settlement_id,
       s.runner_id,
       r.user_id AS runner_user_id,
       r.real_name AS runner_name,
       u.username AS runner_username,
       s.settlement_status,
       s.order_total,
       s.platform_fee,
       s.net_income,
       COUNT(spi.payment_id) AS payment_item_count,
       COUNT(DISTINCT ar.task_id) AS task_count,
       NVL(SUM(p.pay_amount), 0) AS payment_total,
       NVL(SUM(CASE WHEN p.pay_status = 'PAID' THEN p.pay_amount ELSE 0 END), 0) AS paid_payment_total,
       MIN(ar.assigned_at) AS first_assigned_at,
       MAX(ar.assigned_at) AS last_assigned_at,
       MIN(t.completed_at) AS first_completed_at,
       MAX(t.completed_at) AS last_completed_at,
       CASE
           WHEN COUNT(spi.payment_id) = 0 THEN 'NO_ITEMS'
           WHEN ABS(s.order_total - NVL(SUM(p.pay_amount), 0)) > 0.01 THEN 'AMOUNT_MISMATCH'
           ELSE 'OK'
       END AS data_check_result
  FROM APPUSER.settlements s
  JOIN APPUSER.runners r ON r.runner_id = s.runner_id
  JOIN APPUSER.users u ON u.user_id = r.user_id
  LEFT JOIN APPUSER.settlement_payment_items spi ON spi.settlement_id = s.settlement_id
  LEFT JOIN APPUSER.payments p ON p.payment_id = spi.payment_id
  LEFT JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
  LEFT JOIN APPUSER.tasks t ON t.task_id = ar.task_id
 GROUP BY s.settlement_id,
          s.runner_id,
          r.user_id,
          r.real_name,
          u.username,
          s.settlement_status,
          s.order_total,
          s.platform_fee,
          s.net_income;

COMMENT ON TABLE vw_settlement_report IS 'Settlement summary view for admin settlement list, detail verification and report export.';
COMMENT ON COLUMN vw_settlement_report.data_check_result IS 'Lightweight consistency check between settlement snapshot and linked payment items.';
