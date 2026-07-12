# 业务层

Service 负责业务规则、状态判断、事务边界协调和展示名称转换，不直接返回 Razor View。

```text
NodeService.cs
ServiceTypeService.cs
ServiceNodeRuleService.cs
RunnerService.cs
UserService.cs
DisplayNameService.cs
```

当前 `DisplayNameService` 统一转换节点类型/状态、服务类型状态、跑腿员审核状态和工作状态的中文显示值。
