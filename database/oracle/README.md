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
