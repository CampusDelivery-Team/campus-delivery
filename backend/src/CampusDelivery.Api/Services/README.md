# 业务层 Services

业务层负责系统的核心业务规则、状态判断、权限判断和事务流程。

本层应该存放：

```text
UserService.cs
AddressService.cs
RunnerService.cs
TaskService.cs
AssignService.cs
TaskStatusService.cs
PaymentService.cs
RefundService.cs
ReviewService.cs
ComplaintService.cs
SettlementService.cs
AuditService.cs
ReportService.cs
```

本层负责：

- 判断账号是否禁用。
- 判断用户角色是否有权限。
- 判断任务能否取消、支付、接单、签收。
- 判断跑腿员是否审核通过。
- 判断支付、退款、投诉、结算状态是否允许变更。
- 调用一个或多个 Repository 完成业务流程。

本层不应该：

- 直接返回 Razor View。
- 写页面 HTML。
- 直接拼接复杂 SQL。

调用方向：

```text
业务层 -> 持久层
```
