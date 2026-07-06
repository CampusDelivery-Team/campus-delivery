# 持久层 Persistence

Persistence 存放数据库访问基础设施，是持久层的公共支撑。

本层目前包含：

```text
Oracle/OracleConnectionFactory.cs
```

后续可以增加：

```text
Oracle/OracleDbHelper.cs
Oracle/OracleTransactionHelper.cs
```

职责：

- 读取数据库连接串。
- 创建 Oracle 连接。
- 封装公共数据库执行方法。
- 为 Repository 提供统一数据访问入口。

普通业务模块不要在 Controller 或 View 中直接使用本层，应通过 Service 和 Repository 间接访问。
