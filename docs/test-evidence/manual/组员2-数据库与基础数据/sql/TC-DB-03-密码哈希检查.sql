--- TC-DB-03：密码不明文存储

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