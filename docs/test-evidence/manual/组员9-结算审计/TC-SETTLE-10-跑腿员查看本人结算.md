# TC-SETTLE-10：跑腿员查看本人结算

## 1. test-plan 原始要求

| 项目 | 内容 |
| --- | --- |
| 测试点 | 跑腿员查看本人结算 |
| 前置条件 | 跑腿员登录 |
| 测试步骤 | `/Settlement/My` |
| 预期结果 | 只见本人结算单，数据正确 |
| 证据 | 截图 |

## 2. 测试目标解释与最终口径

本用例验证跑腿员端的结算数据隔离。管理员可以查看所有结算，跑腿员只能查看自己账号对应的 `runner_id` 的结算。

当前后端通过登录态中的 `user_id` 查询 `runners.user_id`，再限制 `settlements.runner_id`。因此测试重点是：

- `/Settlement/My` 只展示当前跑腿员自己的结算。
- `/Settlement/MyDetails/{settlement_id}` 只能查看自己的结算详情。
- 手动修改 URL 访问其他跑腿员结算详情时，不应泄露数据。

## 3. 前置条件与测试数据

- 至少一名跑腿员账号可登录。
- 该跑腿员已有至少一张结算单。
- 最好另有一张属于其他跑腿员的结算单，用于测试越权访问。

## 4. 页面操作步骤

1. 使用跑腿员账号登录。
2. 进入 `/Settlement/My`。
3. 截图记录页面上的结算单列表、状态和金额。
4. 点击本人某张结算单，进入 `/Settlement/MyDetails/{settlement_id}`。
5. 使用 SQL 查询当前跑腿员对应的 `runner_id` 和结算数据，核对页面金额。
6. 准备一个其他跑腿员的 `settlement_id`。
7. 在浏览器地址栏手动访问 `/Settlement/MyDetails/{other_settlement_id}`。
8. 预期系统返回未找到、拒绝访问，或不展示他人结算数据。

## 5. SQL 验证

查询当前登录跑腿员的身份映射：

```sql
SELECT
    u.user_id,
    u.username,
    u.role,
    r.runner_id,
    r.real_name,
    r.audit_status,
    r.work_status
FROM APPUSER.users u
JOIN APPUSER.runners r
    ON r.user_id = u.user_id
WHERE u.username = :runner_username;
```

查询该跑腿员自己的结算单：

```sql
SELECT settlement_id, runner_id, order_total, platform_fee, net_income, settlement_status
FROM APPUSER.settlements
WHERE runner_id = :runner_id
ORDER BY settlement_id DESC;
```

查询某张结算单明细：

```sql
SELECT
    spi.settlement_id,
    spi.payment_id,
    p.record_id,
    p.pay_amount,
    p.pay_status,
    ar.runner_id,
    ar.task_id,
    t.task_title,
    t.task_status
FROM APPUSER.settlement_payment_items spi
JOIN APPUSER.payments p
    ON p.payment_id = spi.payment_id
JOIN APPUSER.assign_records ar
    ON ar.record_id = p.record_id
JOIN APPUSER.tasks t
    ON t.task_id = ar.task_id
WHERE spi.settlement_id = :sid
ORDER BY spi.payment_id;
```

查询其他跑腿员结算单，用于越权访问测试：

```sql
SELECT settlement_id, runner_id, order_total, settlement_status
FROM APPUSER.settlements
WHERE runner_id <> :runner_id
ORDER BY settlement_id DESC
FETCH FIRST 10 ROWS ONLY;
```

预期：页面中的结算单都属于当前跑腿员；访问其他跑腿员结算详情不会显示对方数据。

## 6. 证据截图位置

### 页面截图

<!-- TODO: 放置跑腿员 /Settlement/My 列表截图 -->

<!-- TODO: 放置跑腿员本人结算详情截图 -->

<!-- TODO: 放置访问其他跑腿员结算详情被拒绝或未找到的截图 -->

### SQL 截图

<!-- TODO: 放置当前账号 user_id 与 runner_id 映射查询截图 -->

<!-- TODO: 放置本人结算列表 SQL 截图 -->

<!-- TODO: 放置其他跑腿员结算单 SQL 截图 -->

## 7. 实际结果与结论

实际结果：待填写。

结论：待填写：通过 / 失败 / 阻塞 / 需确认。
