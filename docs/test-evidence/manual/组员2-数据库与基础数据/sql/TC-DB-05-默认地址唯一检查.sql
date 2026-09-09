-- TC-DB-05：默认地址唯一约束


-- 检查是否存在一个用户多个默认地址
SELECT user_id,
       COUNT(*) AS DEFAULT_COUNT
FROM user_addresses
WHERE is_default = 'Y'
GROUP BY user_id
HAVING COUNT(*) > 1;


-- 检查数据库唯一索引
SELECT index_name,
       uniqueness,
       status
FROM user_indexes
WHERE index_name = 'UK_USER_ADDRESSES_ONE_DEFAULT';