# 控制层 Controllers

控制层负责接收请求、绑定参数、调用业务层，并返回页面、JSON 或跳转结果。

本层应该存放：

```text
AuthController.cs
UserController.cs
AddressController.cs
RunnerController.cs
TaskController.cs
AssignController.cs
PaymentController.cs
RefundController.cs
ReviewController.cs
ComplaintController.cs
SettlementController.cs
AuditController.cs
ReportController.cs
```

本层可以：

- 接收 GET / POST 请求。
- 校验基础参数是否为空。
- 调用 Service。
- 处理 `View()`、`RedirectToAction()`、`Ok()` 等返回。

本层不应该：

- 编写复杂业务规则。
- 直接写 SQL。
- 直接访问 Oracle。
- 绕过 Service 修改核心业务状态。

调用方向：

```text
控制层 -> 业务层
```
