# 项目业务逻辑总览

## 文档目的

本文用于帮助项目成员快速理解整个系统的业务逻辑。当前数据库表结构已经部署到服务器，后续开发以现有表关系为基础，不通过修改表结构来改变主流程。

> 实现边界（2026-08-15）：账户、地址、配置、资格审核、任务、接派、配送、收货支付、退款、评价、投诉、结算、审计和报表均已有 MVC 路由及五层实现。本文仍包含目标业务规则；当前已验证项、待数据库验证项和已知缺口以 `system-test-report.md` 为准。

本项目是一个**校园中转分发与跑腿服务管理系统**。系统围绕校园内外卖分发、快递代取、私人跑腿等任务展开，支持用户发布任务、跑腿员接单配送、管理员派单和审核、送达后付款、评价投诉、退款售后、跑腿员结算、审计和报表。

## 核心业务定位

系统的核心不是“电商下单预付款”，而是“跑腿服务履约”。

因此项目采用以下业务口径：

```text
用户先发布任务
跑腿员或管理员再接派任务
跑腿员完成配送服务
用户送达后确认付款
平台围绕这次真实接派服务做评价、投诉、结算、审计和报表
```

支付记录不是任务发布时的预付订单，而是一次真实接派服务完成后的收款记录。

## 总体主流程

完整业务主流程如下：

```text
用户注册/登录
-> 维护收货地址
-> 发布任务
-> 任务进入待接单
-> 跑腿员抢单 / 管理员派单
-> 跑腿员取货、配送、送达
-> 用户确认收货并付款
-> 任务完成
-> 用户评价或投诉
-> 平台处理售后
-> 跑腿员收入结算
-> 管理员审计和生成报表
```

对应核心数据链路：

```text
users / user_addresses / service_types / nodes
-> tasks
-> assign_records
-> payments
-> refunds / reviews / complaints / settlements / audit / reports
```

## 角色与职责

### 普通用户

普通用户负责发布和确认任务。

主要行为：

- 注册和登录。
- 维护常用收货地址。
- 发布外卖分发、快递代取、私人跑腿任务。
- 查看任务进度。
- 送达后确认收货和付款。
- 对已完成服务进行评价。
- 对服务问题发起投诉或售后。

相关表：

- `users`
- `user_addresses`
- `tasks`
- 任务明细表
- `payments`
- `reviews`
- `complaints`
- `refunds`

### 跑腿员

跑腿员负责接单和配送。

主要行为：

- 提交跑腿员申请。
- 通过管理员审核后进入可接单状态。
- 在任务大厅抢单。
- 按流程更新配送状态。
- 送达后协助用户确认收货和付款。
- 等待平台结算收入。

相关表：

- `users`
- `runners`
- `assign_records`
- `task_status_logs`
- `payments`
- `settlements`

### 管理员

管理员负责系统配置、审核、派单、售后、结算和审计。

主要行为：

- 维护节点和服务类型。
- 维护服务类型与节点适用关系。
- 审核跑腿员申请。
- 对无人接单任务进行派单。
- 处理重派、投诉、退款。
- 生成跑腿员结算单。
- 做支付、退款、状态日志审计。
- 生成统计报表。

相关表：

- `nodes`
- `service_types`
- `service_node_rules`
- `runners`
- `assign_records`
- `refunds`
- `complaints`
- `settlements`
- `audit_logs`
- `reports`

## 基础资料逻辑

### 账号与地址

`users` 是系统账号主表。

用户角色包括：

| 角色 | 含义 |
| --- | --- |
| `USER` | 普通用户 |
| `RUNNER` | 跑腿员 |
| `ADMIN` | 管理员 |

`user_addresses` 是用户地址表，依附于 `users`。用户发布任务时必须选择属于自己的地址。

业务规则：

- 注册密码由 Service 使用 ASP.NET Core `PasswordHasher<User>` 生成带盐哈希后保存，登录使用 `VerifyHashedPassword` 校验。
- Controller、View 和 Repository 不实现密码算法；数据库不保存原始密码。
- 封禁账号 `account_status = 'BLOCKED'` 和注销账号 `account_status = 'CANCELLED'` 不允许登录；正常账号状态为 `NORMAL`。Cookie 每次认证时重新读取账号状态和角色，封禁后的旧登录态不能继续访问业务接口。
- 地址只属于对应用户，不允许跨用户使用；新增地址先锁定所属用户行再分配 `address_no`，默认地址切换、删除后的默认补位均在同一事务完成。
- 数据库函数唯一索引保证同一用户最多一条默认地址；设置不存在的地址时必须在清空原默认地址之前失败。
- 页面显示中文名称，数据库保存英文状态代码。

### 跑腿员资料

`runners` 保存跑腿员资格资料，通过 `user_id` 关联 `users`。

跑腿员审核状态：

| 状态 | 含义 |
| --- | --- |
| `PENDING` | 待审核 |
| `APPROVED` | 已通过 |
| `REJECTED` | 已拒绝 |

跑腿员工作状态：

| 状态 | 含义 |
| --- | --- |
| `FREE` | 可接单 |
| `BUSY` | 配送中 |
| `OFFLINE` | 离线 |

业务规则：

- 只有 `audit_status = 'APPROVED'` 的跑腿员可以接单。
- 跑腿员接单前应处于 `work_status = 'FREE'`。
- 接单后进入 `BUSY`。
- 完成任务后恢复为 `FREE`。

### 节点、服务类型和适用规则

`nodes` 保存校内交接节点、驿站或分发点。

`service_types` 保存服务类型和价格规则。

`service_node_rules` 表示某类服务可以在哪些节点使用。

业务规则：

- 发布任务时只能选择启用的服务类型。
- 发布任务时只能选择正常开放的节点。
- 任务选择的 `service_type_id` 和 `node_id` 必须存在于 `service_node_rules` 中。
- 如果后续某个节点或服务类型不再使用，应通过状态关闭，不建议删除历史数据。

## 任务发布逻辑

任务发布以 `tasks` 为主单，三类任务明细表保存不同任务类型的专有字段。

相关表：

- `tasks`
- `food_delivery_details`
- `express_pickup_details`
- `private_task_details`

任务类型与明细表：

| 任务类型 | 明细表 |
| --- | --- |
| 外卖分发 | `food_delivery_details` |
| 快递代取 | `express_pickup_details` |
| 私人跑腿 | `private_task_details` |

发布流程：

```text
用户选择服务类型、地址和节点
-> 系统校验服务类型、地址、节点和适用规则
-> 插入 tasks
-> 插入对应任务明细
-> 任务状态进入 WAITING
```

业务规则：

- 一个任务只能对应一种任务明细。
- 发布任务时不创建接派记录。
- 发布任务时不创建支付记录。
- 发布任务时不写状态日志。
- 如果系统没有草稿流程，发布成功后任务状态应为 `WAITING`。

## 任务状态逻辑

任务主状态保存在 `tasks.task_status`。

推荐主流程状态：

```text
WAITING -> ASSIGNED -> PICKED_UP -> DELIVERING -> WAIT_CONFIRM -> FINISHED
```

状态含义：

| 状态 | 含义 |
| --- | --- |
| `CREATED` | 已创建但未正式发布，或系统保留状态 |
| `WAITING` | 已发布，待接单 |
| `ASSIGNED` | 已接单或已派单 |
| `PICKED_UP` | 已取货或已取件 |
| `DELIVERING` | 配送中 |
| `WAIT_CONFIRM` | 已送达，待用户确认和付款 |
| `FINISHED` | 已付款并完成 |
| `CANCELLED` | 已取消 |
| `REFUNDING` | 退款或售后处理中 |
| `PAID` | 预付流程保留状态，当前货到付款主流程不使用 |

业务规则：

- 任务大厅展示 `WAITING` 状态任务。
- 跑腿员接单或管理员派单后，任务进入 `ASSIGNED`。
- 跑腿员配送过程中逐步更新为 `PICKED_UP`、`DELIVERING`。
- 送达后进入 `WAIT_CONFIRM`。
- 用户确认付款后进入 `FINISHED`。
- 当前主流程不使用 `PAID` 作为任务状态，支付是否成功由 `payments.pay_status` 表达。

## 接单、派单与重派逻辑

接派记录保存在 `assign_records`。

`assign_records` 是后续支付、评价、投诉和状态日志的核心连接点。它表示某个跑腿员对某个任务的一次接单、派单或重派记录。

操作类型：

| `operation_type` | 含义 |
| --- | --- |
| `SELF` | 跑腿员抢单 |
| `ADMIN` | 管理员派单 |
| `REASSIGN` | 管理员重派 |

接单流程：

```text
任务处于 WAITING
-> 跑腿员抢单或管理员派单
-> 插入 assign_records
-> 更新任务为 ASSIGNED
-> 更新跑腿员为 BUSY
-> 写入接单后的状态日志
```

重派流程：

```text
保留旧 assign_records
-> 新增一条 operation_type = 'REASSIGN' 的 assign_records
-> 后续支付、评价、投诉、结算绑定最新有效接派记录
```

业务规则：

- 已经接单的任务不能再作为普通待接单任务展示。
- 同一任务可能因为重派产生多条 `assign_records`。
- 当前接派记录按业务有效性取最新记录。
- 查询当前接派记录时，默认按 `assigned_at DESC, record_id DESC` 取最新有效记录。

## 配送状态日志逻辑

配送状态日志保存在 `task_status_logs`。

当前表结构要求 `task_status_logs.record_id` 必须关联 `assign_records.record_id`，所以状态日志只记录接单后的配送状态流转。

业务规则：

- `CREATED -> WAITING` 不写入 `task_status_logs`。
- 未接单任务取消时，不写入 `task_status_logs`。
- 从 `WAITING -> ASSIGNED` 开始写入 `task_status_logs`。
- 接单后每次状态变更都应写入 `task_status_logs`。
- `task_status_logs` 是“接派后的状态流转日志”，不是完整任务生命周期日志。

## 付款逻辑

项目采用货到付款或送达后补付。

相关表：

- `payments`
- `assign_records`
- `tasks`

付款流程：

```text
任务已接单
-> 跑腿员完成配送
-> 任务进入 WAIT_CONFIRM
-> 用户确认收货和付款
-> 创建或更新 payments
-> pay_status = PAID
-> 任务进入 FINISHED
```

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

业务规则：

- `payments` 必须绑定真实 `assign_records`。
- 未接单前不得创建 `payments`。
- 任务完成前必须存在有效支付记录。
- 任务进入 `FINISHED` 前，支付状态必须为 `PAID`。
- 现金付款时 `third_trade_no` 可以为空。
- 线上补付成功后应记录第三方交易流水号。

## 取消与异常逻辑

取消规则：

- `WAITING` 状态下，用户可以取消任务。
- 未接单任务取消只更新 `tasks.task_status = 'CANCELLED'`。
- 已接单任务取消需要绑定当前接派记录，并写入状态日志。
- `PICKED_UP` 或 `DELIVERING` 后不建议普通取消，应进入投诉、售后或管理员处理。

异常规则：

- 跑腿员无法继续配送时，管理员通过重派处理。
- 重派不删除旧接派记录，而是新增 `REASSIGN` 记录。
- 已付款后的售后问题通过投诉和退款处理。

## 退款逻辑

退款保存在 `refunds`，必须基于已有支付记录。

退款状态：

| `process_status` | 含义 |
| --- | --- |
| `APPLY` | 已申请 |
| `APPROVED` | 已同意 |
| `REJECTED` | 已拒绝 |
| `DONE` | 已完成 |

业务规则：

- 未付款任务不能退款，只能取消或投诉。
- 退款必须关联 `payments.payment_id`。
- 退款申请通过后再执行实际退款。
- 退款完成后，对应支付记录可更新为 `REFUNDED`。
- 已结算支付记录退款时，应走管理员审核或人工处理。

## 评价逻辑

评价保存在 `reviews`，同时绑定任务及该任务最终有效的真实接派记录。

业务规则：

- 只有任务发布者可以评价，用户身份从登录 Claims 获取。
- 只允许 `FINISHED` 且支付为 `PAID`、不存在 `APPLY/APPROVED` 活动退款的任务评价；一项任务最多评价一次。
- 跑腿员取该任务按 `assigned_at`、`record_id` 倒序排列的最终接派记录，不接收客户端指定值。
- 评价分数范围为 1 到 5，信誉变化依次为 `-2/-1/0/+1/+2`。
- 评价写入、编辑或删除与信誉分调整使用同一事务，信誉分更新后不得低于 0。

评价的业务对象是一项已完成任务对应的最终跑腿服务。

## 投诉逻辑

投诉保存在 `complaints`，绑定真实接派记录。

投诉状态：

| `process_status` | 含义 |
| --- | --- |
| `SUBMITTED` | 已提交 |
| `PROCESSING` | 处理中 |
| `DONE` | 已处理 |

业务规则：

- 投诉针对一次真实接派服务。
- 未接单任务没有接派记录，不适合写入当前 `complaints` 表。
- 管理员负责处理投诉，并写入处理结果。
- 投诉可能影响退款、结算或跑腿员信誉。

## 结算逻辑

跑腿员收入结算保存在 `settlements` 和 `settlement_payment_items`。

结算链路：

```text
runners
-> assign_records
-> payments
-> settlement_payment_items
-> settlements
```

结算状态：

| `settlement_status` | 含义 |
| --- | --- |
| `WAITING` | 待结算 |
| `DONE` | 已结算 |
| `BLOCKED` | 结算异常或被拦截 |

业务规则：

- 只有 `PAID` 支付记录可以进入结算。
- 已退款、退款中、投诉处理中或异常支付不自动进入普通结算。
- 同一 `payment_id` 只能结算一次。
- 结算时必须校验支付记录对应的跑腿员与结算单跑腿员一致。
- 状态只允许 `WAITING -> DONE`、`WAITING -> BLOCKED`、`BLOCKED -> WAITING`；`DONE` 是终态，状态变更在事务内锁定结算单。

## 审计逻辑

审计主表为 `audit_logs`。

审计对象：

| `audit_object` | 含义 |
| --- | --- |
| `LOG` | 状态日志 |
| `PAYMENT` | 支付记录 |
| `REFUND` | 退款记录 |

审计结果：

| `audit_result` | 含义 |
| --- | --- |
| `PASS` | 通过 |
| `ABNORMAL` | 异常 |

业务规则：

- 状态日志审计写入 `audit_status_log_checks`。
- 支付审计写入 `audit_payment_checks`。
- 退款审计写入 `audit_refund_checks`。
- 审计主表和审计明细表应在同一事务中写入。
- 审计只针对已经产生的业务记录。

## 报表逻辑

报表主表为 `reports`。

报表类型：

| `report_type` | 含义 |
| --- | --- |
| `ORDER` | 订单/任务报表 |
| `PAYMENT` | 支付报表 |
| `COMPLAINT` | 投诉报表 |

报表状态：

| `report_status` | 含义 |
| --- | --- |
| `GENERATED` | 已生成 |
| `EXPORTED` | 已导出 |

业务规则：

- 报表读取历史业务数据，不反向修改业务主数据。
- 报表如果基于审计结果生成，通过 `report_audit_items` 关联审计记录。
- 生成时按 `yyyy-MM` 周期查询订单、支付或投诉业务数据并计算指标；报表主体和审计依据在同一事务写入。
- 查看详情和导出时按既有表结构实时查询该周期业务明细；导出为 UTF-8 CSV，成功生成后把状态更新为 `EXPORTED`。
- 当前 `payments`、`complaints` 没有独立业务时间字段，因此支付按任务完成时间（缺失时使用创建时间）、投诉按关联任务创建时间归入月份，页面必须说明该口径。
- 删除报表时只删除报表和报表关联，不删除审计日志。

## 数据保留逻辑

系统应保留业务历史，不建议对进入业务链路的数据做物理删除。

推荐替代方式：

| 业务对象 | 替代删除方式 |
| --- | --- |
| 用户 | `users.account_status = 'BLOCKED'`（可恢复）或 `CANCELLED`（注销） |
| 节点 | `nodes.node_status = 'CLOSED'` |
| 服务类型 | `service_types.type_status = 'DISABLED'` |
| 任务 | `tasks.task_status = 'CANCELLED'` |
| 结算 | `settlements.settlement_status = 'BLOCKED'` |

这样可以避免破坏任务、接派、支付、退款、评价、投诉、结算、审计之间的历史关系。

## 事务边界

以下场景必须使用事务：

- 发布任务和写入任务明细。
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

## 最终总结

整个项目的业务逻辑可以概括为：

```text
用户发布校园跑腿任务
-> 跑腿员或管理员接派任务
-> 跑腿员完成配送
-> 用户送达后付款
-> 系统围绕真实接派服务完成评价、投诉、退款、结算、审计和报表
```

只要后续开发坚持“接派记录是服务履约核心，支付记录围绕真实接派产生”这个定义，当前数据库关系和业务逻辑就是自洽的。
