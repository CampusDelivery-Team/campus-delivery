# CampusDelivery Database

校园中转分发与跑腿服务管理系统数据库设计与 Oracle 建库脚本。

本仓库当前主要交付数据库相关文件：Oracle 建库 SQL、DBML 关系模型和数据库设计文档。SQL 文件会创建 24 张业务表，并包含主键、外键、唯一约束、检查约束、表/字段注释和常用索引。

## 文件说明

```text
delivery-backend/
├─ campus_runner_oracle_schema（24张表）.sql     # Oracle 建库脚本
├─ schema.dbml                                  # DBML 关系模型
├─ 校园中转分发与跑腿服务管理系统-数据库设计文档.docx
├─ LICENSE
└─ README.md
```

## 环境要求

- Oracle Database：建议 `18c` 或更高版本
- 数据库客户端：`SQL Developer`、`Navicat`、`DBeaver`、`PL/SQL Developer` 或 `SQL*Plus`
- 执行用户需要具备建表、建索引、创建约束和添加注释的权限

## 快速初始化

### 1. 创建业务用户

如果还没有业务用户，可以使用管理员账号连接 Oracle 后执行：

```sql
CREATE USER APPUSER IDENTIFIED BY App123456;
GRANT CONNECT, RESOURCE TO APPUSER;
ALTER USER APPUSER QUOTA UNLIMITED ON USERS;
```

如果当前 Oracle 权限策略不允许直接授予 `RESOURCE`，请按需授予 `CREATE TABLE`、`CREATE SEQUENCE`、`CREATE VIEW` 等项目实际需要的权限。

### 2. 执行建库脚本

使用业务用户连接 Oracle，执行根目录下的：

[campus_runner_oracle_schema（24张表）.sql](D:/delivery-backend/campus_runner_oracle_schema（24张表）.sql)

脚本开头会按外键依赖反向删除旧表：

```sql
DROP TABLE ... CASCADE CONSTRAINTS PURGE
```

因此重复执行会重建全部表结构，同时清空旧数据。正式或含数据环境执行前请先备份。

### 3. 验证执行结果

执行下面的查询确认 24 张表已创建：

```sql
SELECT table_name
FROM user_tables
WHERE table_name IN (
  'USERS', 'USER_ADDRESSES', 'RUNNERS', 'NODES', 'SERVICE_TYPES',
  'SERVICE_NODE_RULES', 'TASKS', 'FOOD_DELIVERY_DETAILS',
  'EXPRESS_PICKUP_DETAILS', 'PRIVATE_TASK_DETAILS', 'ASSIGN_RECORDS',
  'TASK_STATUS_LOGS', 'PAYMENTS', 'REFUNDS', 'REVIEWS', 'COMPLAINTS',
  'SETTLEMENTS', 'SETTLEMENT_PAYMENT_ITEMS', 'AUDIT_LOGS',
  'AUDIT_STATUS_LOG_CHECKS', 'AUDIT_PAYMENT_CHECKS',
  'AUDIT_REFUND_CHECKS', 'REPORTS', 'REPORT_AUDIT_ITEMS'
)
ORDER BY table_name;
```

也可以直接查看数量：

```sql
SELECT COUNT(*) AS table_count
FROM user_tables
WHERE table_name IN (
  'USERS', 'USER_ADDRESSES', 'RUNNERS', 'NODES', 'SERVICE_TYPES',
  'SERVICE_NODE_RULES', 'TASKS', 'FOOD_DELIVERY_DETAILS',
  'EXPRESS_PICKUP_DETAILS', 'PRIVATE_TASK_DETAILS', 'ASSIGN_RECORDS',
  'TASK_STATUS_LOGS', 'PAYMENTS', 'REFUNDS', 'REVIEWS', 'COMPLAINTS',
  'SETTLEMENTS', 'SETTLEMENT_PAYMENT_ITEMS', 'AUDIT_LOGS',
  'AUDIT_STATUS_LOG_CHECKS', 'AUDIT_PAYMENT_CHECKS',
  'AUDIT_REFUND_CHECKS', 'REPORTS', 'REPORT_AUDIT_ITEMS'
);
```

返回 `24` 即表示表结构已创建完整。

## 数据表概览

### 账户与基础资料

| 表名 | 说明 |
| --- | --- |
| `users` | 系统用户账号、角色和账号状态 |
| `user_addresses` | 用户常用地址弱实体表，主键为 `user_id + address_no` |
| `runners` | 跑腿员资格资料、审核状态、接单状态和信誉分 |

### 节点与服务规则

| 表名 | 说明 |
| --- | --- |
| `nodes` | 校内交接节点、驿站或分发点 |
| `service_types` | 服务类型及基础价格、距离规则、加急规则 |
| `service_node_rules` | 服务类型与节点的适用关系，多对多联系表 |

### 任务与任务明细

| 表名 | 说明 |
| --- | --- |
| `tasks` | 任务主单，关联发布用户、服务类型、地址和交接节点 |
| `food_delivery_details` | 外卖分发任务专有明细 |
| `express_pickup_details` | 快递代取任务专有明细 |
| `private_task_details` | 私人物品或私人跑腿任务专有明细 |

`tasks` 通过联合外键保证任务所选地址属于发布用户，并通过 `service_type_id + node_id` 保证服务类型只能选择适用节点。

### 接派与状态流转

| 表名 | 说明 |
| --- | --- |
| `assign_records` | 任务接单、派单和重派记录 |
| `task_status_logs` | 接派记录对应的任务状态流转日志 |

### 支付、退款、评价、投诉与结算

| 表名 | 说明 |
| --- | --- |
| `payments` | 围绕接派记录产生的支付记录 |
| `refunds` | 支付记录衍生的退款申请和处理信息 |
| `reviews` | 服务评价及信誉变动结果，一个接派记录最多一条评价 |
| `complaints` | 针对一次接派服务的投诉及处理结果 |
| `settlements` | 跑腿员收入结算主表 |
| `settlement_payment_items` | 结算单与支付记录之间的结算依据联系 |

### 审计与报表

| 表名 | 说明 |
| --- | --- |
| `audit_logs` | 运营审计日志基本信息 |
| `audit_status_log_checks` | 审计日志与任务状态日志之间的抽查联系 |
| `audit_payment_checks` | 审计日志与支付记录之间的核验联系 |
| `audit_refund_checks` | 审计日志与退款记录之间的核验联系 |
| `reports` | 统计报表的类型、周期和生成状态 |
| `report_audit_items` | 统计报表与审计日志之间的生成依据联系 |

## 主要状态枚举

| 字段 | 允许值 |
| --- | --- |
| `users.user_role` | `USER`、`RUNNER`、`ADMIN` |
| `users.account_status` | `NORMAL`、`DISABLED` |
| `runners.audit_status` | `PENDING`、`APPROVED`、`REJECTED` |
| `runners.work_status` | `FREE`、`BUSY`、`OFFLINE` |
| `nodes.node_status` | `NORMAL`、`CLOSED` |
| `service_types.type_status` | `ENABLED`、`DISABLED` |
| `tasks.task_status` | `CREATED`、`PAID`、`WAITING`、`ASSIGNED`、`PICKED_UP`、`DELIVERING`、`WAIT_CONFIRM`、`FINISHED`、`CANCELLED`、`REFUNDING` |
| `assign_records.operation_type` | `SELF`、`ADMIN`、`REASSIGN` |
| `payments.pay_method` | `WECHAT`、`ALIPAY`、`CASH` |
| `payments.pay_status` | `UNPAID`、`PAID`、`FAILED`、`REFUNDED` |
| `refunds.process_status` | `APPLY`、`APPROVED`、`REJECTED`、`DONE` |
| `complaints.process_status` | `SUBMITTED`、`PROCESSING`、`DONE` |
| `settlements.settlement_status` | `WAITING`、`DONE`、`BLOCKED` |
| `audit_logs.audit_object` | `LOG`、`PAYMENT`、`REFUND` |
| `audit_logs.audit_result` | `PASS`、`ABNORMAL` |
| `reports.report_type` | `ORDER`、`PAYMENT`、`COMPLAINT` |
| `reports.report_status` | `GENERATED`、`EXPORTED` |

## 约束与索引设计

- 所有核心实体表使用主键约束，部分弱实体和联系表使用联合主键。
- 用户名、手机号、跑腿员关联用户、第三方支付流水号等字段设置唯一约束。
- 金额、评分、信誉分、时间先后关系和状态枚举均通过 `CHECK` 约束限制。
- 关键业务关系通过外键表达，包括用户地址、服务节点规则、任务接派、支付退款、结算、审计和报表依据。
- 脚本末尾创建 20 个常用查询索引，覆盖任务状态、创建时间、接派记录、支付状态、退款状态、投诉状态、结算状态、审计对象和报表周期等场景。

## DBML 模型

[schema.dbml](D:/delivery-backend/schema.dbml) 与 SQL 表结构对应，可导入支持 DBML 的工具生成 ER 图或辅助查看关系模型。

## 常见问题

### 执行脚本时报表不存在或表不存在

脚本开头的删除语句会自动忽略 Oracle 的 `ORA-00942` 表不存在错误。第一次执行时出现内部忽略是正常逻辑，只要后续建表成功即可。

### 再次执行后数据消失

这是预期行为。脚本会先删除旧表再创建新表，适合开发、课程设计和结构重建场景。需要保留数据时，请不要直接在生产数据用户下重复执行。

### 外键插入失败

请按依赖关系插入数据。通常顺序为：`users`、`user_addresses`、`runners`、`nodes`、`service_types`、`service_node_rules`、`tasks`，再插入接派、支付、评价、投诉、结算、审计和报表相关数据。

### 中文文件名执行不方便

可以在本地复制一份脚本并改成英文文件名，例如 `campus_runner_oracle_schema.sql`。脚本内容不依赖文件名。
