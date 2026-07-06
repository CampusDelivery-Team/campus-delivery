# 命名规范

## 1. 总体原则

1. 文件名、类名使用清晰业务含义。
2. C# 类名使用 `PascalCase`。
3. 方法名使用 `PascalCase`。
4. 私有字段使用 `_camelCase`。
5. 数据库表名使用 `snake_case`。
6. 页面和按钮显示文本优先使用中文。
7. 业务状态枚举统一使用中文值。

## 2. 仓库目录

```text
backend/       后端项目
frontend/      当前项目主页面
database/      数据库脚本
docs/          项目文档
scripts/       辅助脚本
output/        生成产物，例如 PDF
```

## 3. 后端项目命名

| 类型 | 命名 |
| --- | --- |
| Solution | `CampusDelivery.sln` |
| Project | `CampusDelivery.Api` |
| Root Namespace | `CampusDelivery.Api` |
| Controller | `XxxController.cs` |
| Service | `XxxService.cs` |
| Repository | `XxxRepository.cs` |
| ViewModel | `XxxViewModel.cs` |
| DTO | `XxxResponse.cs`、`XxxRequest.cs` |
| Model | `Xxx.cs` |

示例：

```text
TaskController.cs
TaskService.cs
TaskRepository.cs
TaskCreateViewModel.cs
TaskDetailViewModel.cs
Task.cs
```

## 4. 五层目录命名

```text
Controllers/       控制层
Services/          业务层
Repositories/      持久层：业务 Repository
Persistence/       持久层：数据库连接和公共数据访问
Presentation/      表现层：Views、ViewModels、wwwroot
Models/            业务模型
Dtos/              API 或检查接口 DTO
```

## 5. Controller 命名

Controller 文件必须以 `Controller` 结尾：

```text
AuthController.cs
AddressController.cs
TaskController.cs
PaymentController.cs
ReportController.cs
```

Action 命名建议：

```text
Index
Create
Edit
Details
Delete
Login
Logout
Approve
Reject
Generate
```

## 6. Service 命名

Service 文件必须以 `Service` 结尾：

```text
UserService.cs
TaskService.cs
PaymentService.cs
RefundService.cs
```

Service 方法命名应体现业务动作：

```text
Login
CreateFoodTask
CancelTask
AcceptTask
UpdateTaskStatus
ApplyRefund
GenerateSettlement
```

## 7. Repository 命名

Repository 文件必须以 `Repository` 结尾：

```text
UserRepository.cs
TaskRepository.cs
PaymentRepository.cs
ReportRepository.cs
```

Repository 方法命名建议：

```text
FindById
FindByUsername
GetList
Insert
Update
Delete
Exists
```

## 8. ViewModel 命名

ViewModel 放在：

```text
backend/src/CampusDelivery.Api/Presentation/ViewModels/
```

命名示例：

```text
LoginViewModel.cs
RegisterViewModel.cs
TaskCreateViewModel.cs
TaskDetailViewModel.cs
PaymentViewModel.cs
ReportViewModel.cs
```

## 9. View 命名

View 放在：

```text
backend/src/CampusDelivery.Api/Presentation/Views/
```

建议按模块分目录：

```text
Views/Auth/Login.cshtml
Views/Address/Index.cshtml
Views/Task/Create.cshtml
Views/Task/Details.cshtml
Views/Payment/Create.cshtml
Views/Report/Index.cshtml
```

## 10. 前端主页面命名

当前 `frontend/` 用作项目主页面：

```text
frontend/src/App.vue
frontend/src/style.css
frontend/src/api/http.ts
```

Vue 组件使用 `PascalCase`，普通 TypeScript 工具文件使用 `camelCase`。

## 11. 数据库命名

数据库表名使用 `snake_case`：

```text
users
user_addresses
tasks
task_status_logs
assign_records
payments
refunds
reviews
complaints
settlements
reports
```

约束命名：

```text
pk_表名
fk_表名_关联含义
uk_表名_字段
ck_表名_字段
```

索引命名：

```text
idx_表名_字段
```

## 12. SQL 脚本命名

```text
001_schema.sql
002_init_base_data.sql
003_init_test_data.sql
```

脚本编号用于保证执行顺序。

## 13. Git 分支命名

```text
main
dev
feature/base-db
feature/account-address
feature/base-runner
feature/task
feature/assign-status
feature/payment-refund
feature/review-complaint
feature/settlement-report
feature/test-docs
```

## 14. 提交信息

提交信息使用中文，说明实际完成内容：

```text
完成用户登录功能
完成地址管理页面
完成任务发布主单插入
修复抢单重复判断
补充支付测试数据
整理答辩演示截图
```

不要使用：

```text
update
fix
111
test
改了一点
```
