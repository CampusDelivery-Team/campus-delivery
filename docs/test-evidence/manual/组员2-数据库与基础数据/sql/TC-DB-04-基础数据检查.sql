-- TC-DB-04：基础数据完整


-- 节点
SELECT node_id,
       node_type,
       node_name,
       node_status
FROM nodes
ORDER BY node_id;


-- 服务类型
SELECT service_type_id,
       service_name,
       base_price,
       type_status
FROM service_types
ORDER BY service_type_id;


-- 服务节点适用规则
SELECT st.service_name,
       n.node_name
FROM service_node_rules r
JOIN service_types st
  ON st.service_type_id = r.service_type_id
JOIN nodes n
  ON n.node_id = r.node_id
ORDER BY st.service_name, n.node_name;


-- 必需基础规则数量
SELECT COUNT(*) AS REQUIRED_RULE_COUNT
FROM service_node_rules r
JOIN service_types st
  ON st.service_type_id = r.service_type_id
JOIN nodes n
  ON n.node_id = r.node_id
WHERE (st.service_name = '外卖分发'
       AND n.node_name = '外卖分发点')
   OR (st.service_name = '快递代取'
       AND n.node_name = '菜鸟驿站')
   OR (st.service_name = '私人跑腿'
       AND n.node_name IN (
           '嘉定校区南门',
           '外卖分发点'
       ));