# Oracle 脚本目录

这里存放 Oracle 19c 使用的数据库脚本。

当前已有：

```text
001_schema.sql
```

建议后续补充：

```text
002_init_base_data.sql
003_init_test_data.sql
```

执行顺序建议：

```text
1. 创建 APPUSER 用户并授权
2. 执行建表脚本
3. 执行基础数据脚本
4. 执行测试数据脚本
5. 使用 SQL Developer 验证关键表数据
```
