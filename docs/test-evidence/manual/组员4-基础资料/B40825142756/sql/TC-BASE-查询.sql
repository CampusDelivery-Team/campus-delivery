-- 执行标识：B40825142756
-- 本文件只包含复核用 SELECT，不包含账号密码、连接串或写入语句。

-- TC-BASE-01 / TC-BASE-02：节点最终状态及历史任务引用数量
SELECT n.node_id,
       n.node_type,
       n.node_name,
       n.location,
       n.open_time,
       n.node_status,
       COUNT(t.task_id) AS task_count
FROM nodes n
LEFT JOIN tasks t ON t.node_id = n.node_id
WHERE n.node_id = 101
GROUP BY n.node_id,
         n.node_type,
         n.node_name,
         n.location,
         n.open_time,
         n.node_status;

-- TC-BASE-03：服务类型最终状态
SELECT service_type_id,
       service_name,
       base_price,
       distance_rule,
       urgent_rule,
       type_status
FROM service_types
WHERE service_type_id = 101;

-- TC-BASE-04：服务节点规则最终状态
SELECT service_type_id, node_id
FROM service_node_rules
WHERE service_type_id = 101
  AND node_id = 101;

-- TC-BASE-04：未绑定组合提交不得落库
SELECT COUNT(*) AS rejected_task_count
FROM tasks
WHERE task_title = 'B40825142756未绑定组合';

-- TC-BASE-02：删除保护使用的历史任务
SELECT task_id,
       task_title,
       node_id,
       service_type_id,
       task_status
FROM tasks
WHERE task_id = 255;

-- TC-BASE-05 至 TC-BASE-08：跑腿员与账号最终状态
SELECT u.user_id,
       u.username,
       u.user_role,
       u.account_status,
       r.runner_id,
       r.real_name,
       r.audit_status,
       r.work_status,
       r.credit_score
FROM users u
JOIN runners r ON r.user_id = u.user_id
WHERE u.username IN ('b4runA8076859', 'b4runB8076859')
ORDER BY u.username;
