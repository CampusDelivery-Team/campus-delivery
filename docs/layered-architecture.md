# 校园跑腿系统五层架构说明

## 1. 架构目标

本项目按以下五层进行开发：

```text
表现层 -> 控制层 -> 业务层 -> 持久层（数据访问层） -> 数据库层
```

这样划分的目的是让每个组员知道自己的代码应该放在哪里，避免把页面、业务判断、SQL 和数据库脚本混在一起。

## 2. 五层目录总览

| 层级 | 主要目录 | 负责内容 |
| --- | --- | --- |
| 表现层 | `frontend/`、`backend/src/CampusDelivery.Api/Presentation/` | 页面展示、表单、静态资源、ViewModel |
| 控制层 | `backend/src/CampusDelivery.Api/Controllers/` | 接收请求、调用业务层、返回页面或 JSON |
| 业务层 | `backend/src/CampusDelivery.Api/Services/` | 业务规则、权限判断、状态流转 |
| 持久层 | `backend/src/CampusDelivery.Api/Repositories/`、`backend/src/CampusDelivery.Api/Persistence/` | SQL、数据库访问、Oracle 连接 |
| 数据库层 | `database/`、`database/oracle/` | 建表脚本、基础数据、测试数据 |

当前仓库仍保留 `frontend/` 作为已有前端主页面。如果后续课程要求完全使用 ASP.NET Core MVC 页面，则 MVC 页面放到 `backend/src/CampusDelivery.Api/Presentation/Views/`。

## 3. 表现层

### 3.1 存放位置

```text
frontend/
backend/src/CampusDelivery.Api/Presentation/
├── ViewModels/
├── Views/
└── wwwroot/
```

### 3.2 放什么

表现层放用户能看到或直接交互的内容：

```text
Presentation/Views/Auth/Login.cshtml
Presentation/Views/Task/Create.cshtml
Presentation/Views/Payment/Create.cshtml
Presentation/ViewModels/LoginViewModel.cs
Presentation/ViewModels/TaskCreateViewModel.cs
Presentation/wwwroot/css/site.css
Presentation/wwwroot/js/site.js
```

### 3.3 不放什么

表现层不要放：

1. SQL。
2. Oracle 连接代码。
3. 复杂业务判断。
4. 任务状态、支付状态、退款状态等核心状态变更逻辑。

### 3.4 示例

登录页面提交用户名和密码，但不判断密码是否正确。密码校验应交给业务层。

```text
Login.cshtml -> AuthController.Login -> UserService.Login -> UserRepository
```

## 4. 控制层

### 4.1 存放位置

```text
backend/src/CampusDelivery.Api/Controllers/
```

### 4.2 放什么

控制层放 Controller：

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

### 4.3 Controller 负责什么

Controller 负责：

1. 接收 GET / POST 请求。
2. 接收 ViewModel。
3. 做基础参数检查。
4. 调用 Service。
5. 返回 View、JSON 或 Redirect。
6. 处理提示信息。

### 4.4 Controller 不负责什么

Controller 不负责：

1. 写复杂 SQL。
2. 直接访问 Oracle。
3. 判断复杂业务状态。
4. 直接操作多个数据库表。

### 4.5 示例

```text
TaskController.CreateFood
只负责接收页面提交的外卖任务表单
然后调用 TaskService.CreateFoodTask
```

## 5. 业务层

### 5.1 存放位置

```text
backend/src/CampusDelivery.Api/Services/
```

### 5.2 放什么

业务层放 Service：

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

### 5.3 Service 负责什么

Service 负责系统最重要的业务规则：

1. 禁用账号不能登录。
2. 同一用户只能有一个默认地址。
3. 服务类型和节点必须匹配才能发布任务。
4. 未支付任务才允许支付。
5. 已支付任务才能被接单。
6. 同一任务不能重复抢单。
7. 任务状态必须按顺序流转。
8. 已完成任务才能评价。
9. 投诉成立后扣减跑腿员信誉分。
10. 已完成且无未处理投诉的任务才能结算。

### 5.4 Service 不负责什么

Service 不负责：

1. 页面布局。
2. Razor HTML。
3. 直接显示错误页面。
4. 数据库连接串配置。

## 6. 持久层（数据访问层）

### 6.1 存放位置

```text
backend/src/CampusDelivery.Api/Repositories/
backend/src/CampusDelivery.Api/Persistence/
```

### 6.2 Repositories 放什么

Repository 负责具体 SQL：

```text
UserRepository.cs
AddressRepository.cs
NodeRepository.cs
RunnerRepository.cs
TaskRepository.cs
PaymentRepository.cs
ReportRepository.cs
```

### 6.3 Persistence 放什么

Persistence 放数据库访问基础设施：

```text
Persistence/Oracle/OracleConnectionFactory.cs
Persistence/Oracle/OracleDbHelper.cs
Persistence/Oracle/OracleTransactionHelper.cs
```

目前已放入：

```text
backend/src/CampusDelivery.Api/Persistence/Oracle/OracleConnectionFactory.cs
```

### 6.4 持久层负责什么

持久层负责：

1. 创建 Oracle 连接。
2. 执行 SQL。
3. 查询数据库。
4. 插入、更新、删除数据。
5. 将数据库结果转换为 Model / DTO。

### 6.5 持久层不负责什么

持久层不负责：

1. 页面跳转。
2. 登录菜单显示。
3. 业务状态是否允许变化。
4. 用户看到的完整提示文案。

## 7. 数据库层

### 7.1 存放位置

```text
database/
database/oracle/
```

### 7.2 放什么

数据库层放 SQL 脚本和数据库说明：

```text
database/oracle/001_schema.sql
database/oracle/002_init_base_data.sql
database/oracle/003_init_test_data.sql
docs/schema.dbml
docs/database_dictionary.md
docs/chinese_enum_values.md
```

### 7.3 数据库层负责什么

数据库层负责：

1. 建表。
2. 建约束。
3. 建索引。
4. 插入基础数据。
5. 插入测试数据。
6. 维护中文枚举值。

### 7.4 数据库层不负责什么

数据库层不负责：

1. 页面展示。
2. Controller 路由。
3. Service 业务流程。
4. 用户操作按钮。

## 8. 调用规则

代码只能按下面方向调用：

```text
表现层 -> 控制层 -> 业务层 -> 持久层 -> 数据库层
```

允许：

```text
TaskController 调用 TaskService
TaskService 调用 TaskRepository
TaskRepository 调用 OracleConnectionFactory
OracleConnectionFactory 连接 Oracle
```

不允许：

```text
View 直接调用 Repository
Controller 直接写 SQL
Service 直接返回 cshtml 页面
Repository 判断登录菜单
```

## 9. 每个模块开发时怎么建文件

以任务发布模块为例：

```text
表现层：
Presentation/ViewModels/TaskCreateViewModel.cs
Presentation/ViewModels/TaskDetailViewModel.cs
Presentation/Views/Task/Create.cshtml
Presentation/Views/Task/Details.cshtml

控制层：
Controllers/TaskController.cs

业务层：
Services/TaskService.cs

持久层：
Repositories/TaskRepository.cs

数据库层：
database/oracle/001_schema.sql 中的 tasks 和三类 detail 表
```

以支付退款模块为例：

```text
表现层：
Presentation/ViewModels/PaymentViewModel.cs
Presentation/ViewModels/RefundViewModel.cs
Presentation/Views/Payment/Create.cshtml
Presentation/Views/Refund/Apply.cshtml

控制层：
Controllers/PaymentController.cs
Controllers/RefundController.cs

业务层：
Services/PaymentService.cs
Services/RefundService.cs

持久层：
Repositories/PaymentRepository.cs
Repositories/RefundRepository.cs

数据库层：
payments
refunds
```

## 10. 命名建议

Controller：

```text
XxxController.cs
```

Service：

```text
XxxService.cs
```

Repository：

```text
XxxRepository.cs
```

ViewModel：

```text
XxxViewModel.cs
XxxCreateViewModel.cs
XxxDetailViewModel.cs
```

View：

```text
Presentation/Views/Xxx/Index.cshtml
Presentation/Views/Xxx/Create.cshtml
Presentation/Views/Xxx/Edit.cshtml
Presentation/Views/Xxx/Details.cshtml
```

## 11. 模块归属建议

| 模块 | 表现层 | 控制层 | 业务层 | 持久层 | 数据库层 |
| --- | --- | --- | --- | --- | --- |
| 账户地址 | `Presentation/Views/Auth`、`Presentation/Views/Address` | `AuthController`、`AddressController` | `UserService`、`AddressService` | `UserRepository`、`AddressRepository` | `users`、`user_addresses` |
| 基础资料 | `Presentation/Views/Node`、`Presentation/Views/ServiceType` | `NodeController`、`ServiceTypeController` | `NodeService`、`ServiceTypeService` | `NodeRepository`、`ServiceTypeRepository` | `nodes`、`service_types` |
| 跑腿员 | `Presentation/Views/Runner` | `RunnerController` | `RunnerService` | `RunnerRepository` | `runners` |
| 任务发布 | `Presentation/Views/Task` | `TaskController` | `TaskService` | `TaskRepository` | `tasks`、三类明细表 |
| 接单状态 | `Presentation/Views/Assign`、`Presentation/Views/TaskStatus` | `AssignController`、`TaskStatusController` | `AssignService`、`TaskStatusService` | `AssignRepository`、`TaskStatusLogRepository` | `assign_records`、`task_status_logs` |
| 支付退款 | `Presentation/Views/Payment`、`Presentation/Views/Refund` | `PaymentController`、`RefundController` | `PaymentService`、`RefundService` | `PaymentRepository`、`RefundRepository` | `payments`、`refunds` |
| 评价投诉 | `Presentation/Views/Review`、`Presentation/Views/Complaint` | `ReviewController`、`ComplaintController` | `ReviewService`、`ComplaintService` | `ReviewRepository`、`ComplaintRepository` | `reviews`、`complaints` |
| 结算报表 | `Presentation/Views/Settlement`、`Presentation/Views/Audit`、`Presentation/Views/Report` | `SettlementController`、`AuditController`、`ReportController` | `SettlementService`、`AuditService`、`ReportService` | `SettlementRepository`、`AuditRepository`、`ReportRepository` | `settlements`、`audit_logs`、`reports` |

## 12. 当前已调整内容

本次结构调整已经完成：

1. 新增 `Presentation/` 表现层目录。
2. 新增 `Services/` 业务层目录。
3. 新增 `Repositories/` 持久层 Repository 目录。
4. 新增 `Persistence/` 持久层基础设施目录。
5. 将 `OracleConnectionFactory` 移动到 `Persistence/Oracle/`。
6. 新增数据库层 README。
7. 保留现有 `Controllers/` 作为控制层。
8. 新增本说明文档。

## 13. 开发前检查

每个组员提交代码前需要确认：

```powershell
dotnet build backend/CampusDelivery.sln
```

必须 0 个错误后再合并。
