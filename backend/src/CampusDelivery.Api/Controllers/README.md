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
AccountController.cs          # 管理员账号生命周期管理
AddressController.cs          # 用户地址维护
TaskController.cs             # 任务发布、取消、接派、配送和收货
PaymentController.cs          # 收货后支付和支付状态
RefundController.cs           # 退款申请和管理员审核
ReviewController.cs           # 评价管理
ComplaintController.cs        # 投诉提交和管理员处理
SettlementController.cs       # 结算管理和跑腿员结算查询
AuditController.cs            # 支付、退款和状态日志审计
ReportController.cs           # 统计面板和报表生成记录
DatabaseController.cs         # Oracle 连接检测
```

管理端 Controller 使用 `[Authorize(Roles = "ADMIN")]` 限制访问；`RunnerController.Apply` 只要求登录。
