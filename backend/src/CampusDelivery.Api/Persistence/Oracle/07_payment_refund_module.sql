-- 组员 7：收货后支付与退款模块（按当前表清单的真实字段编写）
--
-- payments: payment_id, record_id, order_amount, pay_amount, pay_method,
--           third_trade_no, pay_status
-- refunds : refund_id, payment_id, refund_amount, refund_reason,
--           approved_amount, process_status
--
-- 重要：payments 没有 task_id；必须通过
-- payments.record_id -> assign_records.record_id -> assign_records.task_id 关联任务。
-- 重要：refunds 没有 refund_status、审核人、审核时间、独立审核原因字段；
-- 审核状态写入 process_status，审核金额写入 approved_amount；审核理由由应用层
-- 以“申请原因 + 换行 + 审核意见：审核理由”的格式合并存入 refund_reason。

ALTER SESSION SET CURRENT_SCHEMA = APPUSER;

--------------------------------------------------------------------------------
-- 1. 用户支付状态查询：只使用 payments 的真实字段，并通过接派记录关联任务。
--------------------------------------------------------------------------------
SELECT p.payment_id,
       ar.task_id,
       p.record_id,
       t.task_title,
       p.order_amount,
       p.pay_amount,
       p.pay_method,
       p.pay_status,
       latest_refund.process_status AS refund_process_status
  FROM payments p
  JOIN assign_records ar ON ar.record_id = p.record_id
  JOIN tasks t ON t.task_id = ar.task_id
  LEFT JOIN (
        SELECT payment_id,
               process_status,
               ROW_NUMBER() OVER (PARTITION BY payment_id ORDER BY refund_id DESC) AS rn
          FROM refunds
       ) latest_refund
    ON latest_refund.payment_id = p.payment_id
   AND latest_refund.rn = 1
 WHERE t.publisher_user_id = :publisher_user_id
 ORDER BY p.payment_id DESC;

--------------------------------------------------------------------------------
-- 2. 管理员退款列表：退款状态来自 process_status。
--------------------------------------------------------------------------------
SELECT r.refund_id,
       r.payment_id,
       ar.task_id,
       t.task_title,
       r.refund_amount,
       r.refund_reason,
       r.approved_amount,
       r.process_status
  FROM refunds r
  JOIN payments p ON p.payment_id = r.payment_id
  JOIN assign_records ar ON ar.record_id = p.record_id
  JOIN tasks t ON t.task_id = ar.task_id
 ORDER BY r.refund_id DESC;

--------------------------------------------------------------------------------
-- 3. 收货后支付或稍后付款。
--    由应用层在同一个事务中执行下列语句，并在全部成功后 COMMIT。
--------------------------------------------------------------------------------
-- 锁定任务；只允许 WAIT_CONFIRM 状态支付。
SELECT task_status, publisher_user_id, task_price
  FROM tasks
 WHERE task_id = :task_id
 FOR UPDATE;

-- 找到当前任务最新接派记录。
SELECT record_id, runner_id
  FROM (
        SELECT record_id, runner_id
          FROM assign_records
         WHERE task_id = :task_id
         ORDER BY assigned_at DESC, record_id DESC
       )
 WHERE ROWNUM = 1;

-- 由“确认收货”日志判断用户已经确认收货。
SELECT COUNT(*) AS receipt_confirmed_count
  FROM task_status_logs l
 WHERE l.record_id = :record_id
   AND l.status_before = 'WAIT_CONFIRM'
   AND l.status_after = 'WAIT_CONFIRM'
   AND l.operator_user_id = :publisher_user_id;

-- 检查是否已经支付。payments 不能直接按 task_id 查询，必须经 assign_records。
SELECT p.payment_id, p.pay_status
  FROM payments p
  JOIN assign_records ar ON ar.record_id = p.record_id
 WHERE ar.task_id = :task_id
 FOR UPDATE;

-- 新增支付记录（立即支付传 PAID，稍后付款传 UNPAID）。
INSERT INTO payments (
  record_id, order_amount, pay_amount, pay_method, third_trade_no, pay_status
) VALUES (
  :record_id, :task_price, :task_price, :pay_method, NULL, :pay_status
);

-- 用户已经确认收货，因此无论立即支付还是稍后付款，都要完成任务并释放跑腿员。
UPDATE tasks
   SET task_status = 'FINISHED',
       completed_at = SYSDATE
 WHERE task_id = :task_id;

UPDATE runners
   SET work_status = 'FREE'
 WHERE runner_id = :runner_id;

INSERT INTO task_status_logs (
  record_id, status_before, status_after, operator_user_id, operated_at
) VALUES (
  :record_id, 'WAIT_CONFIRM', 'FINISHED', :publisher_user_id, SYSDATE
);

-- 稍后付款的用户再次付款时，只更新支付记录；任务和跑腿员已经处理完毕，不能重复写状态日志。
UPDATE payments
   SET pay_method = :pay_method,
       third_trade_no = NULL,
       pay_status = 'PAID'
 WHERE payment_id = :payment_id
   AND pay_status = 'UNPAID';

--------------------------------------------------------------------------------
-- 4. 退款登记。
--    process_status：APPLY（待审核）、APPROVED（通过）、REJECTED（拒绝）。
--------------------------------------------------------------------------------
-- 仅允许已支付、已完成任务申请退款；关联关系仍然经 record_id 建立。
SELECT p.payment_id,
       p.pay_amount,
       p.pay_status,
       p.record_id,
       ar.task_id,
       t.publisher_user_id,
       t.task_status
  FROM payments p
  JOIN assign_records ar ON ar.record_id = p.record_id
  JOIN tasks t ON t.task_id = ar.task_id
 WHERE p.payment_id = :payment_id
 FOR UPDATE;

-- 避免同一支付记录重复存在待审核或已通过退款。
SELECT COUNT(*) AS active_refund_count
  FROM refunds
 WHERE payment_id = :payment_id
   AND process_status IN ('APPLY', 'APPROVED');

INSERT INTO refunds (
  payment_id, refund_amount, refund_reason, approved_amount, process_status
) VALUES (
  :payment_id, :refund_amount, :refund_reason, NULL, 'APPLY'
);

UPDATE tasks
   SET task_status = 'REFUNDING'
 WHERE task_id = :task_id;

INSERT INTO task_status_logs (
  record_id, status_before, status_after, operator_user_id, operated_at
) VALUES (
  :record_id, 'FINISHED', 'REFUNDING', :publisher_user_id, SYSDATE
);

--------------------------------------------------------------------------------
-- 5. 管理员审核退款。
--------------------------------------------------------------------------------
-- 通过退款：退款状态改为 APPROVED、支付状态改为 REFUNDED。
UPDATE refunds
   SET process_status = 'APPROVED',
       approved_amount = refund_amount,
       refund_reason = :combined_reason
 WHERE refund_id = :refund_id
   AND process_status = 'APPLY';

UPDATE payments
   SET pay_status = 'REFUNDED'
 WHERE payment_id = :payment_id;

-- 拒绝退款：退款状态改为 REJECTED，核定金额为 0。
UPDATE refunds
   SET process_status = 'REJECTED',
       approved_amount = 0,
       refund_reason = :combined_reason
 WHERE refund_id = :refund_id
   AND process_status = 'APPLY';

-- 两种审核结果处理结束后，任务恢复为 FINISHED，并写入状态日志。
UPDATE tasks
   SET task_status = 'FINISHED'
 WHERE task_id = :task_id;

INSERT INTO task_status_logs (
  record_id, status_before, status_after, operator_user_id, operated_at
) VALUES (
  :record_id, 'REFUNDING', 'FINISHED', :admin_user_id, SYSDATE
);

-- :combined_reason 由应用层生成，格式为：
-- 原退款原因 || CHR(10) || '审核意见：' || :review_reason。
-- 这样不增加表字段，并且查询时可拆分显示申请原因与审核理由。
