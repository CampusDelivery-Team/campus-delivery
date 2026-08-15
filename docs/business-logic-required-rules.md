# 后续业务逻辑必须遵守的规则

## 目的

当前数据库表结构已经部署到服务器，后续开发不通过修改表结构解决流程问题。所有业务代码必须遵守本文规则，避免出现外键冲突、流程死结或历史数据无法追溯的问题。

> 本文是后续任务、订单和履约模块的强制业务规则，不表示这些页面已经实现。当前未提供对应 Controller 时，不应在首页或导航中伪造发布任务、任务大厅或订单入口。

核心业务模式：

```text
发布任务 -> 待接单 -> 接单/派单 -> 配送状态流转 -> 送达确认 -> 付款 -> 完成 -> 评价/投诉 -> 结算/审计/报表
```

系统采用**货到付款 / 送达后补付**流程，不采用“先支付、后接单”的预付流程。

## 总原则

1. 不修改已部署表结构。
2. 不物理删除已进入业务链路的数据。
3. 数据库保存英文状态代码，页面显示中文名称。
4. `Repository` 只负责 SQL 和原始数据读写。
5. `Service` 负责业务规则、状态流转、事务控制和中文显示转换。
6. 所有跨多表写入必须放在事务中。

## 禁止事项

以下行为后续业务代码不得实现：

- 未接单前创建 `payments`。
- 未接单前写入 `task_status_logs`。
- 将任务完成状态 `FINISHED` 写在付款成功之前。
- 删除已有业务历史的 `tasks`、`assign_records`、`payments`、`refunds`、`reviews`、`complaints`、`settlements`。
- 删除已经被历史任务引用的 `users`、`runners`、`nodes`、`service_types`、`service_node_rules`。
- 让一个任务同时写入多种任务明细表。
- 结算时把不属于该跑腿员的 `payment_id` 挂到该跑腿员的结算单。

## 任务发布规则

相关表：

- `tasks`
- `food_delivery_details`
- `express_pickup_details`
- `private_task_details`

必须遵守：

1. 发布任务时，先写 `tasks`。
2. 再且只写一种任务明细表。
3. 如果没有草稿流程，发布成功后任务状态必须为 `WAITING`。
4. 发布任务事务中，`tasks` 和任务明细必须一起成功或一起回滚。
5. 发布阶段不得创建 `assign_records`、`task_status_logs`、`payments`。

任务类型与明细表对应关系：

| 任务类型 | 明细表 |
| --- | --- |
| 外卖分发 | `food_delivery_details` |
| 快递代取 | `express_pickup_details` |
| 私人跑腿 | `private_task_details` |

## 任务大厅规则

任务大厅只展示：

```text
tasks.task_status = 'WAITING'
```

不得展示：

- `CREATED`
- `ASSIGNED`
- `PICKED_UP`
- `DELIVERING`
- `WAIT_CONFIRM`
- `FINISHED`
- `CANCELLED`
- `REFUNDING`

## 接单与派单规则

相关表：

- `assign_records`
- `tasks`
- `runners`
- `task_status_logs`

必须遵守：

1. 只有 `WAITING` 状态的任务可以被接单或派单。
2. 只有 `runners.audit_status = 'APPROVED'` 的跑腿员可以接单。
3. 跑腿员接单前应处于 `work_status = 'FREE'`。
4. 接单或派单时必须创建 `assign_records`。
5. 创建 `assign_records` 后，任务状态更新为 `ASSIGNED`。
6. 接单成功后，跑腿员状态更新为 `BUSY`。
7. 从 `WAITING -> ASSIGNED` 开始写入 `task_status_logs`。

操作类型：

| `operation_type` | 含义 |
| --- | --- |
| `SELF` | 跑腿员抢单 |
| `ADMIN` | 管理员派单 |
| `REASSIGN` | 管理员重派 |

接单事务必须包含：

```text
检查任务状态 WAITING
检查跑腿员可接单
插入 assign_records
更新 tasks.task_status = ASSIGNED
更新 runners.work_status = BUSY
插入 task_status_logs
提交事务
```

## 重派规则

当前表结构没有 `assign_records.is_current` 或 `assign_records.status` 字段。

必须遵守：

1. 重派时不得删除旧 `assign_records`。
2. 重派时新增一条 `operation_type = 'REASSIGN'` 的 `assign_records`。
3. 后续支付、评价、投诉、结算必须绑定最新有效的接派记录。
4. 查询当前接派记录时，按业务有效性取最新记录。

默认当前接派记录查询规则：

```text
同一 task_id 下
按 assigned_at DESC, record_id DESC
取第一条有效记录
```

## 状态流转规则

推荐主流程状态：

```text
WAITING -> ASSIGNED -> PICKED_UP -> DELIVERING -> WAIT_CONFIRM -> FINISHED
```

状态含义：

| 状态 | 含义 |
| --- | --- |
| `CREATED` | 已创建但未正式发布，或保留状态 |
| `WAITING` | 已发布，待接单 |
| `ASSIGNED` | 已接单或已派单 |
| `PICKED_UP` | 已取货或已取件 |
| `DELIVERING` | 配送中 |
| `WAIT_CONFIRM` | 已送达，待用户确认和付款 |
| `FINISHED` | 已付款并完成 |
| `CANCELLED` | 已取消 |
| `REFUNDING` | 退款或售后处理中 |
| `PAID` | 预付流程保留状态，货到付款主流程不使用 |

必须遵守：

1. `task_status_logs` 只记录接单后的状态流转。
2. `CREATED -> WAITING` 不写 `task_status_logs`。
3. 未接单任务取消时，不写 `task_status_logs`。
4. 已接单后的每次配送状态变化都要写 `task_status_logs`。
5. 更新 `tasks.task_status` 和插入 `task_status_logs` 必须在同一个事务中。

## 付款规则

相关表：

- `payments`
- `assign_records`
- `tasks`

必须遵守：

1. `payments` 必须在真实 `assign_records` 产生后才能创建。
2. 发布任务时不创建 `payments`。
3. 推荐在任务进入 `WAIT_CONFIRM` 后创建或确认 `payments`。
4. 付款成功前，任务不得进入 `FINISHED`。
5. 任务进入 `FINISHED` 前，必须存在绑定当前接派记录的 `PAID` 支付记录。

付款方式：

| `pay_method` | 含义 |
| --- | --- |
| `CASH` | 现金货到付款 |
| `WECHAT` | 微信补付 |
| `ALIPAY` | 支付宝补付 |

支付状态：

| `pay_status` | 含义 |
| --- | --- |
| `UNPAID` | 待付款或待确认收款 |
| `PAID` | 已付款 |
| `FAILED` | 支付失败 |
| `REFUNDED` | 已退款 |

确认付款并完成事务必须包含：

```text
查询当前 assign_records
创建或更新 payments
确认 payments.pay_status = PAID
更新 tasks.task_status = FINISHED
更新 tasks.completed_at
更新 runners.work_status = FREE
插入 task_status_logs
提交事务
```

## 取消规则

必须遵守：

1. `WAITING` 状态任务可以取消。
2. 未接单任务取消时，只更新 `tasks.task_status = 'CANCELLED'`。
3. 已接单任务取消必须绑定当前接派记录，并写入 `task_status_logs`。
4. `PICKED_UP` 或 `DELIVERING` 后不建议普通取消，应进入投诉、售后或人工处理。

## 退款规则

相关表：

- `payments`
- `refunds`

必须遵守：

1. 未付款任务不能退款，只能取消或投诉。
2. 退款必须基于已有 `payments`。
3. `refunds.payment_id` 必须指向真实支付记录。
4. 已结算支付记录退款时，必须走人工审核或售后流程。

退款状态：

| `process_status` | 含义 |
| --- | --- |
| `APPLY` | 已申请 |
| `APPROVED` | 已同意 |
| `REJECTED` | 已拒绝 |
| `DONE` | 已完成 |

## 评价规则

相关表：

- `reviews`
- `tasks`
- `assign_records`
- `runners`

必须遵守：

1. 只有任务发布者可以评价，评价者身份必须来自登录 Claims。
2. 只允许 `FINISHED` 任务评价。
3. 评价必须绑定任务最终有效的真实接派记录，一项任务只能评价一次。
4. 评价产生的 `credit_delta` 由评分自动计算，并同步影响跑腿员信誉分。
5. 新增、编辑、删除评价与信誉分调整必须使用同一数据库事务。
6. 更新信誉分时不得低于 0；触及下限时记录实际生效的信誉变化，以便后续一致性处理。

## 投诉规则

相关表：

- `complaints`
- `assign_records`

必须遵守：

1. 投诉必须绑定真实接派记录。
2. 当前投诉表适合处理“针对一次接派服务”的投诉。
3. 未接单任务的投诉或反馈，当前表结构没有专门承载表，后续若需要只能通过业务规则另行处理。

投诉状态：

| `process_status` | 含义 |
| --- | --- |
| `SUBMITTED` | 已提交 |
| `PROCESSING` | 处理中 |
| `DONE` | 已处理 |

## 结算规则

相关表：

- `settlements`
- `settlement_payment_items`
- `payments`
- `assign_records`

必须遵守：

1. 只有 `payments.pay_status = 'PAID'` 的支付记录可以进入结算。
2. 已退款、退款中、投诉处理中或异常支付不得自动进入普通结算。
3. `settlements.runner_id` 必须与支付记录对应的 `assign_records.runner_id` 一致。
4. 插入 `settlement_payment_items` 前必须通过 join 校验支付记录属于该跑腿员。
5. 同一 `payment_id` 只能结算一次。

结算查询必须从跑腿员出发：

```text
runners
-> assign_records
-> payments
-> settlement_payment_items
```

不得只凭前端传入的 `payment_id` 直接插入结算明细。

## 审计规则

相关表：

- `audit_logs`
- `audit_status_log_checks`
- `audit_payment_checks`
- `audit_refund_checks`

必须遵守：

1. 审计必须针对已经存在的业务记录。
2. `audit_object = 'LOG'` 时，只写 `audit_status_log_checks`。
3. `audit_object = 'PAYMENT'` 时，只写 `audit_payment_checks`。
4. `audit_object = 'REFUND'` 时，只写 `audit_refund_checks`。
5. 审计主表和审计明细表必须在同一个事务中写入。

## 报表规则

相关表：

- `reports`
- `report_audit_items`
- `audit_logs`

必须遵守：

1. 报表只读取历史业务数据，不反向修改业务主数据。
2. 如果报表基于审计结果生成，需要写入 `report_audit_items`。
3. 删除报表只允许删除报表记录和报表审计关联，不得删除审计日志本身。

## 数据删除规则

后续开发默认不做业务主数据物理删除。

替代方式：

| 业务对象 | 替代删除方式 |
| --- | --- |
| 用户 | `users.account_status = 'BLOCKED'`（可恢复）或 `CANCELLED`（注销） |
| 节点 | `nodes.node_status = 'CLOSED'` |
| 服务类型 | `service_types.type_status = 'DISABLED'` |
| 任务 | `tasks.task_status = 'CANCELLED'` |
| 结算 | `settlements.settlement_status = 'BLOCKED'` |

## 事务边界汇总

必须使用事务的场景：

- 发布任务和写入明细。
- 接单或派单。
- 重派。
- 配送状态更新。
- 确认付款并完成任务。
- 退款申请和审核。
- 评价并更新信誉分。
- 投诉处理。
- 跑腿员结算。
- 审计主表和审计明细写入。
- 报表和报表审计关联写入。

## 最终要求

后续新增 Controller、Service、Repository 时，必须按本文规则实现。

如果业务需求和本文规则冲突，应优先保持现有数据库关系自洽，不得在代码中强行实现“先支付、后接单”等会导致外键依赖冲突的流程。
