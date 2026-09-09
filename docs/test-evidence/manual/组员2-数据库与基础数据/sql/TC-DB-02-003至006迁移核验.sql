-- TC-DB-02：003-006 迁移核验


-- 003：账号生命周期约束
SELECT constraint_name,
       status,
       validated,
       search_condition_vc
FROM user_constraints
WHERE table_name = 'USERS'
  AND constraint_name = 'CK_USERS_STATUS';


-- 004：REVIEWS.TASK_ID 非空
SELECT column_name,
       data_type,
       nullable
FROM user_tab_columns
WHERE table_name = 'REVIEWS'
  AND column_name = 'TASK_ID';


-- 004：评价完整性约束
SELECT constraint_name,
       constraint_type,
       status,
       validated
FROM user_constraints
WHERE constraint_name IN (
    'UK_ASSIGN_RECORD_TASK',
    'UK_REVIEWS_TASK',
    'FK_REVIEWS_RECORD_TASK'
)
ORDER BY constraint_name;


-- 005：PASSWORD_HASH 字段
SELECT column_name,
       data_type,
       char_length,
       char_used,
       nullable
FROM user_tab_columns
WHERE table_name = 'USERS'
  AND column_name = 'PASSWORD_HASH';


-- 005：密码哈希格式核验
SELECT COUNT(*) AS USER_COUNT,
       SUM(
           CASE
               WHEN REGEXP_LIKE(
                   password_hash,
                   '^AQ[A-Za-z0-9+/]{80}==$'
               )
               THEN 1
               ELSE 0
           END
       ) AS HASH_OK_COUNT,
       SUM(
           CASE
               WHEN REGEXP_LIKE(
                   password_hash,
                   '^AQ[A-Za-z0-9+/]{80}==$'
               )
               THEN 0
               ELSE 1
           END
       ) AS INVALID_COUNT
FROM users;


-- 006：唯一索引核验
SELECT index_name,
       uniqueness,
       status
FROM user_indexes
WHERE index_name IN (
    'UK_USER_ADDRESSES_ONE_DEFAULT',
    'UK_SERVICE_TYPES_NAME_CI'
)
ORDER BY index_name;