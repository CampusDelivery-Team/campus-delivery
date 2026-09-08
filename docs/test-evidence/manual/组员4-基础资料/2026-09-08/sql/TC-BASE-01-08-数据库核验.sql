/* TC-BASE-01：节点最终状态 */
SELECT node_id, node_type, node_name, location, open_time, node_status
FROM nodes
WHERE node_id = 123;

/* TC-BASE-02：节点仍存在，且已被成功发布的任务引用 */
SELECT n.node_id, n.node_name, n.node_status,
       t.task_id, t.task_title, t.task_status
FROM nodes n
JOIN tasks t ON t.node_id = n.node_id
WHERE n.node_id = 123
  AND t.task_id = 341;

/* TC-BASE-03：服务类型最终恢复启用，基础价格正确 */
SELECT service_type_id, service_name, base_price,
       distance_rule, urgent_rule, type_status
FROM service_types
WHERE service_type_id = 123;

/* TC-BASE-04：最终绑定关系存在 */
SELECT snr.service_type_id, st.service_name,
       snr.node_id, n.node_name
FROM service_node_rules snr
JOIN service_types st ON st.service_type_id = snr.service_type_id
JOIN nodes n ON n.node_id = snr.node_id
WHERE snr.service_type_id = 123
  AND snr.node_id = 123;

/* TC-BASE-04：解绑时的失败发布未落库，重新绑定后的任务成功落库 */
SELECT task_id, task_title, service_type_id, node_id,
       task_price, task_status
FROM tasks
WHERE task_title LIKE 'M4R260908542211%'
ORDER BY task_id;

/* TC-BASE-05～08：申请链最终状态 */
SELECT u.user_id, u.username, u.user_role, u.account_status,
       r.runner_id, r.audit_status, r.work_status, r.credit_score,
       r.real_name, r.identity_info
FROM users u
JOIN runners r ON r.user_id = u.user_id
WHERE u.username = 'm4r260908542211';

