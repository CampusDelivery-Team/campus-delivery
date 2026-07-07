# 业务层

Service 负责业务规则、状态判断、流程控制和展示名称转换。

当前文件：

```text
NodeService.cs
DisplayNameService.cs
```

Service 可以调用一个或多个 Repository，但不直接返回 Razor View。

数据库存英文代码、页面显示中文名称时，转换逻辑优先放在 Service 或专门的显示名称辅助类中。
