# 控制层

Controller 负责接收 MVC 请求、绑定 ViewModel、调用 Service，并返回 View、Redirect 或少量 HTTP 结果；不要在 Controller 中直接写复杂 SQL 或直接访问 Oracle。

```text
HomeController.cs             # 首页、错误页和拒绝访问页
AuthController.cs             # 登录、注册和退出登录
UserController.cs             # 个人资料查看和联系电话修改
NodeController.cs             # 节点同页新增、编辑、关闭/恢复
ServiceTypeController.cs      # 服务类型同页新增、编辑、启用/停用
ServiceNodeRuleController.cs  # 服务节点绑定和受限解除
RunnerController.cs           # 跑腿员申请、审核与状态管理
DatabaseController.cs         # Oracle 连接检测
```

管理端 Controller 使用 `[Authorize(Roles = "ADMIN")]` 限制访问；`RunnerController.Apply` 只要求登录。
