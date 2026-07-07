# Oracle 脚本

## 标准建表脚本

```text
campus_runner_oracle_schema.sql
```

该脚本包含 24 张业务表。执行前请确认已连接到正确的 PDB，例如本机开发环境：

```text
localhost:1521/XEPDB1
```

建议使用 `APPUSER` 执行建表脚本。

## 当前未提供的脚本

仓库中没有以下文件：

```text
002_init_base_data.sql
003_init_test_data.sql
```

如后续需要基础数据或演示数据，应按上述命名新增，并在文档中补充执行顺序。

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
