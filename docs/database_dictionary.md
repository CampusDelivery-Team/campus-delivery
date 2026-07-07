# 数据库字典

## 基本说明

目前数据库字典仍不全面，只可做基本说明

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

### `nodes`

- 主键：`node_id`
- 作用：保存校内交接节点、驿站或分发点信息
- 关键字段：`node_type`、`node_name`、`location`、`open_time`、`node_status`

### `service_types`

- 主键：`service_type_id`
- 作用：保存服务类型及价格规则
- 关键字段：`service_name`、`base_price`、`distance_rule`、`urgent_rule`、`type_status`

### `tasks`

- 主键：`task_id`
- 作用：保存任务主单公共字段
- 外键：发布用户、服务类型、地址、交接节点
- 关键字段：`task_title`、`task_price`、`urgent_flag`、`task_status`、`created_at`、`completed_at`

### `assign_records`

- 主键：`record_id`
- 作用：保存任务接单、派单和重派记录
- 关键字段：`task_id`、`runner_id`、`operation_type`、`assigned_at`

### `payments`

- 主键：`payment_id`
- 作用：保存围绕接派记录产生的支付记录
- 关键字段：`order_amount`、`pay_amount`、`pay_method`、`third_trade_no`、`pay_status`

## 脚本来源

结构定义见：

```text
database/oracle/campus_runner_oracle_schema.sql
```

基础数据见：

```text
database/oracle/002_init_base_data.sql
```

## 维护建议

- 表结构变更后同步更新本字典
- 枚举值含义以 schema 中的约束和注释为准
- 页面展示中文名称时，不修改数据库英文代码本身
