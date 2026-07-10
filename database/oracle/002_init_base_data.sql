/*
  校园中转分发与跑腿服务管理系统
  基础数据初始化脚本

  说明：
  1. 数据库内部枚举值统一使用英文代码。
  2. 页面显示中文由后端或前端映射完成。
  3. 本脚本只插入基础运行数据，不插入完整业务演示数据。
  4. password_hash 当前为登录模块未接入前的占位值，不代表真实密码存储方案。
*/

SET DEFINE OFF;

INSERT INTO users (username, phone, password_hash, user_role, account_status)
SELECT 'admin', '13000000000', '123456', 'ADMIN', 'NORMAL'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE username = 'admin'
);

INSERT INTO users (username, phone, password_hash, user_role, account_status)
SELECT 'user001', '13100000001', '123456', 'USER', 'NORMAL'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE username = 'user001'
);

INSERT INTO users (username, phone, password_hash, user_role, account_status)
SELECT 'runner001', '13200000001', '123456', 'RUNNER', 'NORMAL'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE username = 'runner001'
);

INSERT INTO user_addresses (
    user_id, address_no, contact_name, contact_phone,
    campus, building_room, is_default
)
SELECT u.user_id, 1, '测试用户', '13100000001',
       '嘉定校区', '学生宿舍1号楼101', 'Y'
FROM users u
WHERE u.username = 'user001'
  AND NOT EXISTS (
      SELECT 1
      FROM user_addresses a
      WHERE a.user_id = u.user_id
        AND a.address_no = 1
  );

INSERT INTO user_addresses (
    user_id, address_no, contact_name, contact_phone,
    campus, building_room, is_default
)
SELECT u.user_id, 2, '测试用户', '13100000001',
       '嘉定校区', '教学楼A区大厅', 'N'
FROM users u
WHERE u.username = 'user001'
  AND NOT EXISTS (
      SELECT 1
      FROM user_addresses a
      WHERE a.user_id = u.user_id
        AND a.address_no = 2
  );

INSERT INTO runners (
    user_id, real_name, identity_info,
    audit_status, work_status, credit_score
)
SELECT u.user_id, '测试跑腿员', '同济大学学生证-测试编号001',
       'APPROVED', 'FREE', 100
FROM users u
WHERE u.username = 'runner001'
  AND NOT EXISTS (
      SELECT 1
      FROM runners r
      WHERE r.user_id = u.user_id
  );

INSERT INTO nodes (
    node_type, node_name, location, open_time, node_status
)
SELECT 'GATE', '嘉定校区南门', '嘉定校区南门入口', '08:00-22:00', 'NORMAL'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM nodes WHERE node_name = '嘉定校区南门'
);

INSERT INTO nodes (
    node_type, node_name, location, open_time, node_status
)
SELECT 'STATION', '菜鸟驿站', '嘉定校区生活区快递驿站', '09:00-21:00', 'NORMAL'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM nodes WHERE node_name = '菜鸟驿站'
);

INSERT INTO nodes (
    node_type, node_name, location, open_time, node_status
)
SELECT 'DISTRIBUTION', '外卖分发点', '嘉定校区生活区外卖集中取餐点', '10:00-22:00', 'NORMAL'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM nodes WHERE node_name = '外卖分发点'
);

INSERT INTO service_types (
    service_name, base_price, distance_rule, urgent_rule, type_status
)
SELECT '外卖分发', 3.00, '校内基础配送费3元，跨区域可加价', '加急加收2元', 'ENABLED'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM service_types WHERE service_name = '外卖分发'
);

INSERT INTO service_types (
    service_name, base_price, distance_rule, urgent_rule, type_status
)
SELECT '快递代取', 4.00, '校内基础代取费4元，重件或远距离可加价', '加急加收2元', 'ENABLED'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM service_types WHERE service_name = '快递代取'
);

INSERT INTO service_types (
    service_name, base_price, distance_rule, urgent_rule, type_status
)
SELECT '私人任务', 5.00, '按任务复杂度和距离综合计费', '加急加收3元', 'ENABLED'
FROM dual
WHERE NOT EXISTS (
    SELECT 1 FROM service_types WHERE service_name = '私人任务'
);

INSERT INTO service_node_rules (service_type_id, node_id)
SELECT st.service_type_id, n.node_id
FROM service_types st
JOIN nodes n ON n.node_name = '外卖分发点'
WHERE st.service_name = '外卖分发'
  AND NOT EXISTS (
      SELECT 1
      FROM service_node_rules r
      WHERE r.service_type_id = st.service_type_id
        AND r.node_id = n.node_id
  );

INSERT INTO service_node_rules (service_type_id, node_id)
SELECT st.service_type_id, n.node_id
FROM service_types st
JOIN nodes n ON n.node_name = '菜鸟驿站'
WHERE st.service_name = '快递代取'
  AND NOT EXISTS (
      SELECT 1
      FROM service_node_rules r
      WHERE r.service_type_id = st.service_type_id
        AND r.node_id = n.node_id
  );

INSERT INTO service_node_rules (service_type_id, node_id)
SELECT st.service_type_id, n.node_id
FROM service_types st
JOIN nodes n ON n.node_name IN ('嘉定校区南门', '外卖分发点')
WHERE st.service_name = '私人任务'
  AND NOT EXISTS (
      SELECT 1
      FROM service_node_rules r
      WHERE r.service_type_id = st.service_type_id
        AND r.node_id = n.node_id
  );

COMMIT;

PROMPT Base data initialized successfully.
