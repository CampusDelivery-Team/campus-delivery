# 数据库层

数据库层只存放数据库脚本和数据库设计相关文件。

当前标准 Oracle 建表脚本：

```text
database/oracle/campus_runner_oracle_schema.sql
```

该脚本包含 24 张业务表，是本仓库唯一保留的建表脚本。

当前没有基础数据和测试数据脚本：

```text
002_init_base_data.sql
003_init_test_data.sql
```

数据库中的枚举值和状态值使用英文代码，页面展示时再转换成中文。
