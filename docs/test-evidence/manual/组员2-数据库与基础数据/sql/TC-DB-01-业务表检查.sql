-- TC-DB-01：24 张业务表齐全

SELECT COUNT(*) AS TABLE_COUNT
FROM user_tables;

SELECT table_name
FROM user_tables
ORDER BY table_name;