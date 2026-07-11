# 持久层 Repository

Repository 负责参数化 SQL 和数据库读写，通过 `Persistence/Oracle/OracleConnectionFactory.cs` 获取 Oracle 连接。

```text
NodeRepository.cs             # nodes
ServiceTypeRepository.cs      # service_types
ServiceNodeRuleRepository.cs  # service_node_rules 及关联查询
RunnerRepository.cs           # runners/users 审核与状态写入
UserRepository.cs             # 用户登录、注册和资料查询/更新
```

Repository 原样读写数据库英文代码，不负责中文显示名称、页面跳转或 Razor View。
