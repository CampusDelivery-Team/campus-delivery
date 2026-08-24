SET DEFINE OFF;
SET SERVEROUTPUT ON;
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK;

PROMPT Restoring required service-to-node rules...

-- This migration is intentionally idempotent. It repairs rows that may have been
-- omitted during deployment or removed later, without relying on environment-specific IDs.
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
WHERE st.service_name = '私人跑腿'
  AND NOT EXISTS (
      SELECT 1
      FROM service_node_rules r
      WHERE r.service_type_id = st.service_type_id
        AND r.node_id = n.node_id
  );

DECLARE
    required_rule_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO required_rule_count
    FROM service_node_rules r
    JOIN service_types st ON st.service_type_id = r.service_type_id
    JOIN nodes n ON n.node_id = r.node_id
    WHERE (st.service_name = '外卖分发' AND n.node_name = '外卖分发点')
       OR (st.service_name = '快递代取' AND n.node_name = '菜鸟驿站')
       OR (st.service_name = '私人跑腿' AND n.node_name IN ('嘉定校区南门', '外卖分发点'));

    IF required_rule_count <> 4 THEN
        RAISE_APPLICATION_ERROR(
            -20007,
            'Required service types or nodes are missing; expected 4 baseline service-node rules.'
        );
    END IF;
END;
/

COMMIT;

PROMPT Required service-to-node rules restored successfully.
