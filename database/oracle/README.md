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
| `database-enhancement/01_views.sql` | 创建第五阶段业务查询视图 |
| `database-enhancement/02_triggers.sql` | 创建任务状态审计及支付、退款关键数据变更审计触发器 |
| `database-enhancement/03_procedures.sql` | 创建账号封禁/解封、默认地址、跑腿员审核和原子接单四个后端业务过程 |
| `database-enhancement/04_functions.sql` | 创建计价、接单资格和服务节点校验三个函数，清理旧信誉等级函数，并收紧信誉分约束 |
| `database-enhancement/05_test.sql` | 集中验证第五阶段视图、触发器、过程、函数和信誉分约束 |
| `database-enhancement/06_rollback_*.sql` | 按对象类型回滚第五阶段对象；信誉分归一化不提供伪恢复 |
| `database-enhancement/member2_triggers.md` | 组员2触发器设计、状态矩阵、测试结果和部署边界 |
| `database-enhancement/member4_functions.md` | 组员4函数接口、业务口径、执行方法和已知边界 |

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
database-enhancement/01_views.sql
database-enhancement/03_procedures.sql
database-enhancement/04_functions.sql
```

随后执行 `database-enhancement/05_test.sql`。过程测试会动态选择可用记录，并通过保存点回滚所有测试写入；但测试期间仍会短暂取得行锁，共享库应在低流量维护窗口执行。`04_functions.sql` 会把已有的超分记录统一截断为100，该归一化不会保存旧的超额部分，执行前必须备份并暂停评价、投诉等信誉写入。脚本中的其他数据冲突检查失败时，应先分析并修复历史数据，不得通过删除约束或跳过检查强行继续。

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

`database-enhancement/03_procedures.sql` 中的四个过程与 `04_functions.sql` 中的三个函数均已由后端直接调用。所有写过程只返回稳定结果码，不在过程内部提交或回滚，最终事务由 Repository 控制。应先在隔离库执行创建、测试、回滚和再次创建流程；通过后再由数据库负责人使用应用 schema 部署。数据库对象不会由后端启动过程自动创建，脚本加入仓库也不代表共享库已经部署。若未先部署这些对象，新版后端的账号管理、默认地址、跑腿员审核、任务发布和接单入口会因对象不存在而失败。

该脚本同时包含信誉分0至100约束迁移。应在暂停相关写入并备份超分记录后执行，再部署包含同样上下限规则的后端；`database-enhancement/05_test.sql` 通过后才能恢复写入。`06_rollback_functions.sql` 只删除三个函数，不删除新约束，也不尝试恢复未留存的历史超分。

## 自动审计触发器

`database-enhancement/02_triggers.sql` 创建任务状态审计触发器，以及支付、退款关键数据变更审计触发器。它不新增业务表，也不重复写入应用已经生成的 `task_status_logs`，而是把核验结果写入现有 `audit_logs` 和对应关联表。正式执行顺序为：

```text
database-enhancement/01_views.sql
database-enhancement/02_triggers.sql
database-enhancement/03_procedures.sql
database-enhancement/04_functions.sql
database-enhancement/05_test.sql
```

`database-enhancement/06_rollback_triggers.sql` 只删除三个触发器。设计理由、状态迁移矩阵、个人 Schema 隔离测试结果和 DBeaver 只读复核 SQL 见 `database-enhancement/member2_triggers.md` 与 `database-enhancement/member2_triggers_personal_verify.sql`。

截至 2026-09-11，触发器已在 `APP2452098` 的 `M2_` 隔离对象上测试通过，尚未部署到 `APPUSER` 正式 Schema。脚本进入仓库不代表共享库已经部署。

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
