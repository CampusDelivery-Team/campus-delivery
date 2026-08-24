# Oracle 数据库脚本

本文是数据库脚本、执行顺序和共享库迁移状态的唯一权威说明。字段字典见 `docs/database_dictionary.md`。

## 脚本清单

| 脚本 | 用途 |
| --- | --- |
| `campus_runner_oracle_schema.sql` | 创建 24 张业务表、序列、约束和索引；包含 DROP/重建逻辑 |
| `002_init_base_data.sql` | 插入基础账号、节点、服务类型和服务节点规则 |
| `003_add_account_lifecycle.sql` | 统一账号生命周期状态 |
| `004_add_review_integrity.sql` | 增加评价任务列、任务唯一约束和复合外键 |
| `005_hash_user_passwords.sql` | 扩展密码字段并迁移基础账号 Identity 哈希 |
| `006_harden_business_integrity.sql` | 增加单默认地址和服务名称唯一索引 |
| `007_restore_required_service_node_rules.sql` | 幂等恢复三类基础服务所需的服务节点绑定 |

## 新建或重建数据库

只在空库、专用开发库，或经数据库负责人确认需要重建时执行：

```text
1. campus_runner_oracle_schema.sql
2. 002_init_base_data.sql
```

`campus_runner_oracle_schema.sql` 已包含当前完整结构。它会删除并重建业务表，禁止直接在共享库执行。

基础数据脚本不写入完整任务、支付、退款、评价、投诉、结算或审计闭环数据。需要端到端验收时，应使用隔离库通过页面形成数据；仓库目前不提供 `003_init_test_data.sql`。

## 升级已有数据库

已有旧结构数据库在备份和预检查后按顺序执行：

```text
003_add_account_lifecycle.sql
004_add_review_integrity.sql
005_hash_user_passwords.sql
006_harden_business_integrity.sql
007_restore_required_service_node_rules.sql
```

脚本中的数据冲突检查失败时，应先分析并修复历史数据，不得通过删除约束或跳过检查强行继续。

## 共享库当前状态

2026-08-22 通过 SSH 隧道和应用成员账号完成只读核验：

- `APPUSER` 拥有 24 张业务表和 14 个序列；
- `CK_USERS_STATUS` 已启用并验证；
- `REVIEWS.TASK_ID` 为非空，`UK_REVIEWS_TASK`、`FK_REVIEWS_RECORD_TASK` 和相关索引有效；
- 31 个账号全部使用 ASP.NET Core Identity 格式密码哈希；
- `UK_USER_ADDRESSES_ONE_DEFAULT`、`UK_SERVICE_TYPES_NAME_CI` 均为有效唯一索引；
- 重复默认地址、重复服务名称、评价任务空值和评价接派关系异常均为 0。

因此 `003` 至 `006` 已完成，不再属于当前迁移待办。

`007_restore_required_service_node_rules.sql` 是针对基础绑定数据漂移的补丁；各环境执行后应确认四条基础绑定均存在。未取得共享库写权限前，不得把“脚本已加入仓库”等同于“共享库已完成迁移”。

## 密码迁移工具

如果数据库包含脚本无法安全处理的历史密码，使用：

```text
backend/tools/CampusDelivery.PasswordMigration
```

工具支持只读盘点、事务迁移、DPAPI 加密备份、迁移后校验和恢复。具体命令见该工具目录的 `README.md`。正式执行前必须暂停账号写入并保存可验证备份。

## 连接和权限

公共联调库使用 Oracle 19c，Service Name 为 `orclpdb1`。本地通过 SSH 隧道连接 `127.0.0.1:15210/orclpdb1`；完整配置和安全边界见 `docs/environment-guide.md`。

真实账号、密码和私钥不得写入 SQL、配置、文档或提交记录。普通结构查看优先使用只读账号；DDL、迁移和共享库写入需要数据库负责人明确授权。

## 数据值约定

数据库状态和枚举统一保存英文代码。Repository 原样读写英文值，Service/ViewModel 准备中文显示名称，Razor View 只负责展示。详细分层规则见 `docs/layered-architecture.md`。
