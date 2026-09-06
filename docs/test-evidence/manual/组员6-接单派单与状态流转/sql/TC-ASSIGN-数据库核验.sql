-- 组员 6 接单派单与状态流转数据库核验
-- 执行日期：2026-09-06
-- 测试标识：A6-2451200-0904
-- 测试任务：T01~T08 对应 task_id 321~328

-- 1. 八个任务的当前状态与发布人
SELECT t.task_id,
       t.task_title,
       t.task_status,
       u.username AS publisher_username
FROM APPUSER.tasks t
JOIN APPUSER.users u ON u.user_id = t.publisher_user_id
WHERE t.task_id BETWEEN 321 AND 328
ORDER BY t.task_id;

-- 2. 抢单、派单和重派记录
SELECT ar.record_id,
       ar.task_id,
       ar.runner_id,
       r.real_name AS runner_name,
       ar.operation_type,
       ar.assigned_at,
       ar.reassign_reason
FROM APPUSER.assign_records ar
JOIN APPUSER.runners r ON r.runner_id = ar.runner_id
WHERE ar.task_id BETWEEN 321 AND 328
ORDER BY ar.task_id, ar.assigned_at, ar.record_id;

-- 3. 任务状态日志
SELECT ar.task_id,
       l.log_id,
       l.record_id,
       l.status_before,
       l.status_after,
       l.operator_user_id,
       l.operated_at
FROM APPUSER.task_status_logs l
JOIN APPUSER.assign_records ar ON ar.record_id = l.record_id
WHERE ar.task_id BETWEEN 321 AND 328
ORDER BY ar.task_id, l.operated_at, l.log_id;

-- 4. 跑腿员资格、工作状态与当前活动任务数
WITH latest_assign AS (
    SELECT ar.*,
           ROW_NUMBER() OVER (
               PARTITION BY ar.task_id
               ORDER BY ar.assigned_at DESC, ar.record_id DESC
           ) AS rn
    FROM APPUSER.assign_records ar
)
SELECT u.username,
       r.runner_id,
       r.audit_status,
       r.work_status,
       COUNT(t.task_id) AS active_task_count
FROM APPUSER.users u
JOIN APPUSER.runners r ON r.user_id = u.user_id
LEFT JOIN latest_assign la
       ON la.runner_id = r.runner_id
      AND la.rn = 1
LEFT JOIN APPUSER.tasks t
       ON t.task_id = la.task_id
      AND t.task_status IN ('ASSIGNED', 'PICKED_UP', 'DELIVERING', 'WAIT_CONFIRM')
WHERE u.username IN (
    'a6_2451200_r1',
    'a6_2451200_r2',
    'a6_2451200_r3',
    'a6_2451200_rp'
)
GROUP BY u.username, r.runner_id, r.audit_status, r.work_status
ORDER BY u.username;

-- 5. 三个负向用例的接派与日志数量
SELECT t.task_id,
       t.task_status,
       COUNT(DISTINCT ar.record_id) AS assign_count,
       COUNT(DISTINCT l.log_id) AS log_count
FROM APPUSER.tasks t
LEFT JOIN APPUSER.assign_records ar ON ar.task_id = t.task_id
LEFT JOIN APPUSER.task_status_logs l ON l.record_id = ar.record_id
WHERE t.task_id IN (324, 327, 328)
GROUP BY t.task_id, t.task_status
ORDER BY t.task_id;
