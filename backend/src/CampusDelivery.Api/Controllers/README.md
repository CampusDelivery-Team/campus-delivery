# 控制层

Controller 负责接收 MVC 请求、绑定 ViewModel、调用 Service，并返回 View、Redirect 或少量 JSON。

当前控制器：

```text
HomeController.cs       # 首页门户
NodeController.cs       # 节点管理
DatabaseController.cs   # 数据库连接检测
```

不要在 Controller 中直接写复杂 SQL，也不要直接访问 Oracle。
