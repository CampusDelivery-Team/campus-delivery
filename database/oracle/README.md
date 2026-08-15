# Oracle 脚本

## 标准建表脚本

```text
campus_runner_oracle_schema.sql
```

该脚本包含 24 张业务表。当前公共联调数据库服务名为 `orclpdb1`。

注意：`campus_runner_oracle_schema.sql` 包含 `DROP TABLE` 和重建表逻辑，只用于初始化空库、重建开发库，或经数据库负责人确认后的重建操作。不要在公共联调库中随意执行。

## 基础数据脚本

```text
002_init_base_data.sql
```

该脚本插入基础运行数据，不插入完整演示测试数据。

## 已有数据库迁移

现有数据库按顺序执行：

```text
003_add_account_lifecycle.sql
004_add_review_integrity.sql
005_hash_user_passwords.sql
```

`004_add_review_integrity.sql` 会为评价补充 `task_id`，增加“一项任务只能评价一次”的唯一约束，并通过复合外键保证评价绑定的接派记录属于同一任务。脚本执行前会检查历史数据；如果同一任务已经存在多条评价，脚本会停止并提示先处理冲突数据，不会自动删除历史评价。

`005_hash_user_passwords.sql` 会把 `users.password_hash` 扩展到 `VARCHAR2(256 CHAR)`，并将三个基础测试账号更新为 ASP.NET Core `PasswordHasher<User>` 生成的带盐哈希。如果现有库还包含其他明文密码账号，脚本会在修改数据前停止，要求先明确重置这些账号，不会在正式登录逻辑中保留明文兼容分支。

### 已有账号的统一密码迁移

公共联调库存在非种子账号时，不要直接修改 `005_hash_user_passwords.sql` 绕过保护检查，也不要用一份固定哈希覆盖所有人的密码。请使用：

```text
backend/tools/CampusDelivery.PasswordMigration
```

该工具会保留每个账号原来的登录密码，把旧值分别转换为随机盐 Identity 哈希。正式执行顺序为：停止账号写入、`--dry-run` 盘点、`--execute` 事务迁移、`--verify-backup` 校验 DPAPI 加密备份、`--verify-migrated` 逐账号校验原密码兼容性、再次 `--dry-run` 确认旧格式数量为零，最后启动应用做登录回归。具体命令和回滚方式见工具目录中的 `README.md`。

## 当前未提供的脚本

```text
003_init_test_data.sql
```

如后续需要完整业务演示数据，应按上述命名新增，并在文档中补充执行顺序。

## 中英文规则

数据库字段值存英文代码，例如：

```text
NORMAL
CLOSED
GATE
STATION
DISTRIBUTION
```

MVC 页面展示时由 Service 或 ViewModel 转换为中文。
