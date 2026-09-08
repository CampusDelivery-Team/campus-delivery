# TC-BASE-06：管理员审核通过

## 1. test-plan 原始要求

| 项目 | 内容 |
| --- | --- |
| 前置条件 | 存在 `PENDING` 申请 |
| 测试步骤 | 管理员在 `/Runner/Pending` 审核通过 |
| 预期结果 | `APPROVED`；用户角色为 `RUNNER`；工作状态为 `FREE` |
| 证据要求 | 截图 + SQL |

## 2. 实际操作

管理员在跑腿员待审核列表中找到本轮隔离用户，并执行审核通过。

## 3. 页面证据

![管理员审核通过](screenshots/TC-BASE-06-管理员审核通过.png)

## 4. SQL 核验

```sql
SELECT u.user_id, u.username, u.user_role, u.account_status,
       r.runner_id, r.audit_status, r.work_status, r.credit_score
FROM users u
JOIN runners r ON r.user_id = u.user_id
WHERE u.username = 'm4r260908542211';
```

## 5. SQL 实际结果

查询返回：`user_id=585`、`user_role=RUNNER`、`account_status=NORMAL`、`runner_id=462`、`audit_status=APPROVED`、`work_status=FREE`、`credit_score=100`。

## 6. 结论

通过。审核状态、用户角色和工作状态三项同步正确。

