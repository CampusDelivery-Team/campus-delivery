# TC-SETTLE-04：退款/投诉中支付不结算

## 1. test-plan 原始要求

| 项目 | 内容 |
| --- | --- |
| 测试点 | 退款/投诉中支付不结算 |
| 前置条件 | 有 `REFUNDING` 或投诉中的支付 |
| 测试步骤 | 生成结算单 |
| 预期结果 | 该类支付不进入结算明细 |
| 证据 | SQL + 结果 |

## 2. 测试目标解释与最终口径

本用例验证“结算前已经存在争议”的支付记录不会进入新结算单。退款和投诉都表示该笔收入还不稳定，如果提前结算给跑腿员，后续退款或赔付会导致财务口径冲突。

当前实现中，结算候选会排除：

- `refunds.process_status IN ('APPLY', 'APPROVED', 'DONE')` 的支付记录。
- `complaints.process_status IN ('SUBMITTED', 'PROCESSING')` 的投诉记录。
- 任务本身不是 `FINISHED` 的记录，例如任务状态为 `REFUNDING` 时也不会进入候选。

注意：本用例测试的是“结算前争议拦截”。“已经结算后再发起售后”的场景不属于本用例主体，当前方案是普通退款入口阻止已结算订单继续退款，并提示用户通过投诉渠道交由管理员核查处理。

## 3. 前置条件与测试数据

至少准备以下数据之一：

- 一笔已支付订单，并且存在 `APPLY` 或 `APPROVED` 状态退款。
- 一笔已支付订单，并且存在 `SUBMITTED` 或 `PROCESSING` 状态投诉。
- 一笔任务状态已经进入 `REFUNDING` 的订单。

如果当前数据库没有此类数据，本用例不能直接写通过，应记录为“阻塞：缺少退款或投诉测试数据”。

## 4. 页面操作步骤

1. 使用 SQL 找到一笔有退款或投诉争议的支付记录，记录 `payment_id`、`record_id`、`task_id`、`runner_id`。
2. 使用管理员账号登录。
3. 进入 `/Settlement/Candidates`。
4. 查找该支付对应的跑腿员分组。
5. 确认该 `payment_id` 不出现在候选列表中。
6. 如果该跑腿员还有其他可结算记录，可以生成结算单。
7. 进入新结算单详情，确认明细中不包含该争议 `payment_id`。
8. 用 SQL 验证该 `payment_id` 没有写入新结算单明细。

## 5. SQL 验证

查询活动退款或已退款记录：

```sql
SELECT
    rf.refund_id,
    rf.payment_id,
    rf.refund_amount,
    rf.process_status,
    rf.refund_reason,
    p.record_id,
    p.pay_status,
    ar.task_id,
    ar.runner_id,
    t.task_status,
    t.task_title
FROM APPUSER.refunds rf
JOIN APPUSER.payments p
    ON p.payment_id = rf.payment_id
JOIN APPUSER.assign_records ar
    ON ar.record_id = p.record_id
JOIN APPUSER.tasks t
    ON t.task_id = ar.task_id
WHERE rf.process_status IN ('APPLY', 'APPROVED', 'DONE')
ORDER BY rf.refund_id DESC;
```

查询处理中投诉记录：

```sql
SELECT
    c.complaint_id,
    c.record_id,
    c.reason,
    c.process_status,
    p.payment_id,
    p.pay_status,
    ar.task_id,
    ar.runner_id,
    t.task_status,
    t.task_title
FROM APPUSER.complaints c
JOIN APPUSER.assign_records ar
    ON ar.record_id = c.record_id
LEFT JOIN APPUSER.payments p
    ON p.record_id = c.record_id
JOIN APPUSER.tasks t
    ON t.task_id = ar.task_id
WHERE c.process_status IN ('SUBMITTED', 'PROCESSING')
ORDER BY c.complaint_id DESC;
```

模拟后端候选过滤，验证指定争议支付不会出现在候选中：

```sql
SELECT
    p.payment_id,
    p.record_id,
    p.pay_status,
    ar.runner_id,
    ar.task_id,
    t.task_status,
    t.task_title
FROM APPUSER.payments p
JOIN APPUSER.assign_records ar
    ON ar.record_id = p.record_id
JOIN APPUSER.tasks t
    ON t.task_id = ar.task_id
WHERE p.payment_id = :payment_id
  AND p.pay_status = 'PAID'
  AND t.task_status = 'FINISHED'
  AND NOT EXISTS (
      SELECT 1
      FROM APPUSER.settlement_payment_items spi
      WHERE spi.payment_id = p.payment_id
  )
  AND NOT EXISTS (
      SELECT 1
      FROM APPUSER.complaints c
      WHERE c.record_id = p.record_id
        AND c.process_status IN ('SUBMITTED', 'PROCESSING')
  )
  AND NOT EXISTS (
      SELECT 1
      FROM APPUSER.refunds rf
      WHERE rf.payment_id = p.payment_id
        AND rf.process_status IN ('APPLY', 'APPROVED', 'DONE')
  );
```

确认该支付没有进入结算明细：

```sql
SELECT settlement_id, payment_id
FROM APPUSER.settlement_payment_items
WHERE payment_id = :payment_id;
```

预期：候选过滤 SQL 无结果；若该支付尚未结算，最后一条 SQL 也应无结果。

## 6. 证据截图位置

### 页面截图

<!-- TODO: 放置结算候选页面中不出现争议 payment_id 的截图 -->

<!-- TODO: 如生成了结算单，放置结算详情中不包含该 payment_id 的截图 -->

### SQL 截图

<!-- TODO: 放置退款或投诉测试数据查询截图 -->

<!-- TODO: 放置候选过滤 SQL 无结果截图 -->

<!-- TODO: 放置 settlement_payment_items 查询无结果截图 -->

## 7. 实际结果与结论

实际结果：待填写。

结论：待填写：通过 / 失败 / 阻塞 / 需确认。
