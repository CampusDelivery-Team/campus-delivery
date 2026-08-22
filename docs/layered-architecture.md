# MVC 五层架构与开发约定

本文是项目架构、目录职责、命名和数据显示规则的唯一权威说明。

## 五层结构

| 层级 | 目录 | 职责 |
| --- | --- | --- |
| 表现层 | `Presentation/Views`、`Presentation/ViewModels`、`Presentation/wwwroot` | Razor 页面、表单/展示模型和静态资源 |
| 控制层 | `Controllers` | 接收请求、绑定参数、调用业务层并返回 View、Redirect 或 HTTP 结果 |
| 业务层 | `Services`、`Services/Interfaces` | 业务规则、权限和归属检查、状态机、事务协调和显示名称转换 |
| 持久层 | `Repositories`、`Repositories/Interfaces`、`Persistence` | 参数化 SQL、Oracle 连接、行锁和数据库读写 |
| 数据库层 | `database/oracle` | Oracle 表、序列、约束、索引、基础数据和迁移脚本 |

固定调用方向：

```text
View -> Controller -> IService -> Service -> IRepository -> Repository
     -> OracleConnectionFactory -> Oracle
```

## 依赖边界

- Controller 只注入 Service 接口，不直接引用 Repository 或 Oracle 类型。
- Service 只依赖 Repository 接口，不创建 Oracle Command/Connection，不返回 Razor View。
- Repository 负责 SQL、参数绑定、行锁和数据映射，不处理页面跳转或中文展示。
- View 只展示 ViewModel 并收集输入，不访问 Service/Repository，不实现复杂业务规则。
- 跨表写操作由 Service 组织，并通过统一 Repository 事务在同一连接和事务中完成。

`scripts/run-tests.ps1` 会检查 Controller/Service 的关键越层行为和 Repository 行锁语句。

## 目录约定

```text
backend/src/CampusDelivery.Api/
  Controllers/
  Models/
  Services/
    Interfaces/
  Repositories/
    Interfaces/
  Persistence/Oracle/
  Presentation/
    ViewModels/
    Views/
    wwwroot/
```

`Persistence/Oracle/OracleConnectionFactory.cs` 是 Repository 创建 Oracle 连接的统一入口。页面使用 `Presentation/wwwroot/css/site.css`，当前不加载 Bootstrap。

## 命名规则

| 类型 | 命名 |
| --- | --- |
| Controller | `XxxController.cs` |
| Service / 接口 | `XxxService.cs` / `IXxxService.cs` |
| Repository / 接口 | `XxxRepository.cs` / `IXxxRepository.cs` |
| Model | `Xxx.cs` |
| 创建表单 ViewModel | `XxxCreateViewModel.cs` |
| 编辑表单 ViewModel | `XxxEditViewModel.cs` |
| 页面组合 ViewModel | `XxxIndexViewModel.cs`、`XxxDetailsViewModel.cs` |
| 列表项 ViewModel | `XxxListItemViewModel.cs` |
| Razor 页面 | `Presentation/Views/Xxx/Action.cshtml` |

新增和编辑可以使用独立页面，也可以在 `Index.cshtml` 中完成；POST Action 仍使用明确的 `Create`、`Edit` 等业务名称。

## 英文入库、中文显示

数据库枚举和状态字段统一保存英文代码，页面显示中文名称：

| 层级 | 职责 |
| --- | --- |
| 数据库 | 保存英文代码，并用 CHECK 约束限制取值 |
| Repository | 原样读写英文代码 |
| Service | 按需转换中文显示名称 |
| ViewModel | 通过 `XxxDisplayName` 承载展示值 |
| Razor View | 展示中文字段，表单 `value` 提交英文代码 |

示例：

```text
nodes.node_type = GATE
NodeTypeDisplayName = 校门
```

```html
<option value="NORMAL">正常</option>
```

统一显示转换优先放在 `DisplayNameService` 或展示型 ViewModel，避免在多个 View 中重复判断。

## 安全和一致性约定

- 所有修改状态的 MVC POST Action 必须使用 `[ValidateAntiForgeryToken]`。
- 管理页面使用 `[Authorize(Roles = "ADMIN")]`；用户和跑腿员操作同时检查登录角色与对象归属。
- 密码只保存 ASP.NET Core `PasswordHasher<User>` 哈希。
- 抢单、支付、退款、评价、地址默认切换、结算等竞争写操作必须有事务和必要的 `FOR UPDATE` 行锁。
- 任务、支付、退款、投诉和结算状态必须按明确状态机转换，不能接受前端任意目标状态。
- 删除基础资料前先检查业务引用；历史业务记录优先保留并使用状态关闭。

## 新增模块检查清单

1. Model 与数据库字段、英文状态代码一致。
2. Repository 接口只暴露持久化所需操作，SQL 使用参数绑定。
3. Service 实现对象归属、权限、状态机和事务规则。
4. Controller 只负责 HTTP/MVC 协调，并为 POST 添加防伪校验。
5. ViewModel 承担表单校验和中文展示字段。
6. Razor View 使用真实路由，不使用 `#` 或跳回首页的占位链接。
7. 在 `Program.cs` 注册 Repository 和 Service 接口。
8. 高风险业务规则补充自动化测试，并运行 `scripts/run-tests.ps1`。

业务规则详情见 `business-logic-overview.md`；当前功能与验收状态见 `system-test-report.md`。
