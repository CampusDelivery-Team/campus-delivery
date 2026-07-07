# 数据库层

数据库层保留数据库脚本和数据库设计资料。

当前标准脚本：

```text
database/oracle/campus_runner_oracle_schema.sql
database/oracle/002_init_base_data.sql
```

说明：

- `campus_runner_oracle_schema.sql`：建表脚本，包含删除旧表和重建逻辑。
- `002_init_base_data.sql`：基础运行数据脚本。
- `003_init_test_data.sql`：当前尚未提供。

当前公共联调数据库使用 Oracle 19c，服务名为 `orclpdb1`。真实数据库账号和密码由服务器负责人单独提供，不写入仓库。

数据库枚举值和状态值使用英文代码，页面展示时再转换成中文。
