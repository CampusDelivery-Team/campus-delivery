# 数据库字典

## 基本说明

本文覆盖当前结构脚本中的全部 24 张关系表及核心约束。字段、枚举和外键的最终依据为 `database/oracle/campus_runner_oracle_schema.sql`。

当前数据库模型共 24 张关系表，按功能大致分为：

- 账户与地址：`users`、`user_addresses`、`runners`
- 基础资料：`nodes`、`service_types`、`service_node_rules`
- 任务主流程：`tasks`、`food_delivery_details`、`express_pickup_details`、`private_task_details`
- 接派与状态：`assign_records`、`task_status_logs`
- 资金与售后：`payments`、`refunds`、`reviews`、`complaints`、`settlements`、`settlement_payment_items`
- 审计与报表：`audit_logs`、`audit_status_log_checks`、`audit_payment_checks`、`audit_refund_checks`、`reports`、`report_audit_items`

## 核心表

### `users`

- 主键：`user_id`
- 作用：保存系统用户账号、角色和账号状态
- 关键字段：`username`、`phone`、`password_hash`、`user_role`、`account_status`
- `password_hash` 使用 `VARCHAR2(256 CHAR)`，保存 ASP.NET Core `PasswordHasher<User>` 生成的带盐哈希，不保存原始密码

### `user_addresses`

- 联合主键：`user_id + address_no`
- 作用：保存用户地址弱实体；地址序号只在所属用户范围内有意义
- 函数唯一索引：`uk_user_addresses_one_default`，只对 `is_default = 'Y'` 的行索引 `user_id`，保证每个用户最多一个默认地址
- 应用层在用户行锁和同一事务内完成地址编号分配及默认地址切换；数据库索引负责最终一致性

### `runners`

- 主键：`runner_id`
- 作用：保存跑腿员审核、工作状态及信誉分
- `credit_score` 默认100，通过 `ck_runners_credit` 限制在0至100

### `nodes`

- 主键：`node_id`
- 作用：保存校内交接节点、驿站或分发点信息
- 关键字段：`node_type`、`node_name`、`location`、`open_time`、`node_status`

### `service_types`

- 主键：`service_type_id`
- 作用：保存服务类型及价格规则
- 关键字段：`service_name`、`base_price`、`distance_rule`、`urgent_rule`、`type_status`
- 基础数据中的外卖分发、快递代取、私人跑腿基础价分别为3元、4元、5元；运行时以表中当前值为准
- 发布页只展示这三种固定服务，并分别映射到外卖、快递和私人跑腿明细表；页面不再另行接收任务明细类型
- 函数唯一索引：`uk_service_types_name_ci`，对 `UPPER(TRIM(service_name))` 唯一，防止并发请求写入语义相同的名称

### `tasks`

- 主键：`task_id`
- 作用：保存任务主单公共字段
- 外键：发布用户、服务类型、地址、交接节点
- 关键字段：`task_title`、`task_price`、`urgent_flag`、`task_status`、`created_at`、`completed_at`
- `task_price` 是数据库计价函数按当前 `service_types.base_price` 加发布者填写的非负附加费生成的最终总价；页面预览值不直接入库

### `assign_records`

- 主键：`record_id`
- 作用：保存任务接单、派单和重派记录
- 关键字段：`task_id`、`runner_id`、`operation_type`、`assigned_at`

### `payments`

- 主键：`payment_id`
- 作用：保存围绕接派记录产生的支付记录
- 关键字段：`order_amount`、`pay_amount`、`pay_method`、`third_trade_no`、`pay_status`

### `reviews`

- 主键：`review_id`
- 唯一约束：`task_id`，保证一项任务最多一条评价
- 复合外键：`record_id + task_id`，关联 `assign_records` 的同一接派记录和任务
- 作用：保存任务发布者对最终有效接派服务的评分、文字反馈及系统计算后实际生效的信誉分变化
- 关键字段：`task_id`、`record_id`、`rating`、`anonymous_flag`、`comment_text`、`reviewed_at`、`credit_delta`

### `settlements`

- 主键：`settlement_id`
- 作用：保存一次结算批次及其财务结果；`settlement_payment_items` 保存该结果对应的支付明细
- `order_total`、`platform_fee`、`net_income` 是结算生成时的财务快照，而不是用于替代支付明细的重复主数据
- 保留快照可避免后续费率调整、退款人工处理或展示逻辑变化改写已经确认的历史结算金额

## 第三范式与受控冗余说明

主体业务表按实体和联系拆分，非主属性依赖各自主键。以下字段是为完整性或历史审计保留的受控冗余/快照：

- `reviews.record_id` 标识实际被评价的最终接派服务；`reviews.task_id` 可经接派记录推导，但保留它是为了直接实施 `UNIQUE(task_id)` 的“一单一评”规则，并通过 `(record_id, task_id)` 复合外键防止把评价绑定到其他任务的接派记录。
- `reviews.credit_delta` 保存考虑信誉分0至100上下限后实际生效的变化值。它可能不同于评分规则的理论变化值，编辑或删除评价时必须依靠该快照精确补差和回滚。
- `settlements.order_total`、`platform_fee`、`net_income` 保存结算发生时的财务口径；来源支付记录仍由 `settlement_payment_items` 逐条保留，可独立复核。

这些字段不作为可独立修改的重复事实：全部由 Service 在事务中计算，页面和客户端不能直接指定。该设计在保持主体第三范式的基础上，为唯一约束、跨表一致性和历史可追溯性保留最小必要快照。

## 脚本来源

结构定义见：

```text
database/oracle/campus_runner_oracle_schema.sql
```

基础数据见：

```text
database/oracle/002_init_base_data.sql
```

已有数据库的完整性约束迁移见：

```text
database/oracle/004_add_review_integrity.sql
database/oracle/006_harden_business_integrity.sql
database/oracle/database-enhancement/04_functions.sql
```

## 维护建议

- 表结构变更后同步更新本字典
- 枚举值含义以 schema 中的约束和注释为准
- 页面展示中文名称时，不修改数据库英文代码本身
