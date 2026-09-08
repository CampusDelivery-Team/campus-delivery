# TC-BASE-05：跑腿员申请

## 1. test-plan 原始要求

| 项目 | 内容 |
| --- | --- |
| 前置条件 | 普通用户登录 |
| 测试步骤 | `/Runner/Apply` 提交申请 |
| 预期结果 | `runners` 新增记录，审核状态为 `PENDING` |
| 证据要求 | 截图 + SQL |

## 2. 实际操作

使用本轮隔离普通用户填写真实姓名和身份信息并提交跑腿员申请。

## 3. 页面证据

![跑腿员申请待审核](screenshots/TC-BASE-05-跑腿员申请待审核.png)

## 4. SQL 核验

```sql
SELECT u.user_id, u.username, u.user_role,
       r.runner_id, r.audit_status, r.work_status,
       r.real_name, r.identity_info
FROM users u
JOIN runners r ON r.user_id = u.user_id
WHERE u.username = 'm4r260908542211';
```

## 5. SQL 实际结果

查询确认 `runners` 已生成 `runner_id=462`，真实姓名和身份信息均已保存。由于同一记录随后又完成驳回、重申和审核通过，当前查询显示最终状态 `APPROVED`，无法用最终快照反推提交瞬间的 `PENDING`；待审核过程由提交后立即保存的页面截图证明。

## 6. 结论

通过。申请成功写入数据库，提交后页面进入待审核状态。

