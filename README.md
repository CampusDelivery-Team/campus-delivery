# 校园中转分发与跑腿服务管理系统

当前仓库统一为 `ASP.NET Core MVC` 五层架构，项目主入口是后端 MVC 页面，不再包含 Vue/Vite 前端。

## 当前结构

```text
backend/                                  # ASP.NET Core MVC 应用
backend/src/CampusDelivery.Api/
  Controllers/                            # 控制层
  Services/                               # 业务层
  Repositories/                           # 持久层
  Persistence/Oracle/                     # Oracle 连接基础设施
  Models/                                 # 数据/领域模型
  Presentation/
    Views/                                # Razor 页面
    ViewModels/                           # 页面展示和表单模型
    wwwroot/                              # 静态资源
backend/tests/CampusDelivery.Tests/       # 高价值业务自动化测试

database/oracle/campus_runner_oracle_schema.sql
docs/
scripts/
```

## 运行方式

在仓库根目录执行：

```powershell
.\scripts\run-backend.ps1
```

或：

```powershell
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

默认访问：

```text
http://localhost:5227/
http://localhost:5227/Node
http://localhost:5227/Database/Status
```

公开演示入口：

```text
https://47.116.60.57/
```

公网使用 HTTPS 默认端口 443；应用内部端口 5227 不直接对公网开放。

## 当前已落地模块与页面

| 地址 | 说明 |
| --- | --- |
| `/` | 项目主页面，作为系统门户和其他模块入口 |
| `/Auth/Login`、`/Auth/Register` | 登录和注册页面 |
| `/User/Profile`、`/User/Edit` | 登录用户查看个人资料和修改联系电话 |
| `/Address` | 登录用户新增、编辑、删除和设置默认地址 |
| `/Account` | 管理员封禁、解封、注销和恢复账号 |
| `/Node` | 管理员新增、修改、关闭和恢复节点资料 |
| `/ServiceType` | 管理员维护服务类型、价格规则和启用状态 |
| `/ServiceNodeRule` | 管理员维护服务类型与适用节点绑定 |
| `/Runner` | 管理员查看跑腿员资格和工作状态 |
| `/Runner/Pending` | 管理员审核待处理的跑腿员申请 |
| `/Runner/Apply` | 登录用户提交或查看跑腿员申请 |
| `/Task`、`/Task/Create` | 我的任务、三类任务发布和取消 |
| `/Task/Hall`、`/Task/MyTasks` | 跑腿员任务大厅、抢单和配送状态流转 |
| `/Task/Receipt` | 发布者确认收货 |
| `/Task/AdminConsole` | 管理员派单和重派 |
| `/Payment/Status` | 收货后支付、稍后付款和支付状态查询 |
| `/Refund/Create`、`/Refund/AdminIndex` | 用户退款申请和管理员审核 |
| `/Review/MyReviews`、`/Review/All` | 用户评价管理和管理员评价查询 |
| `/Complaint/MyComplaints`、`/Complaint/Index` | 用户投诉记录和管理员处理 |
| `/Settlement`、`/Settlement/My` | 管理员生成结算单、跑腿员查看结算 |
| `/Audit` | 管理员对支付、退款和状态日志进行审计 |
| `/Report` | 管理员查看统计面板并生成报表记录 |
| `/Database/Status` | Oracle 连接检测页面 |

首页和导航根据访客、普通用户、跑腿员和管理员身份展示真实入口。任务发布、抢单/派单、配送、收货支付、退款、评价、投诉、结算、审计和统计报表均已有 Controller、Service、Repository 和 Razor 页面。当前验收边界与已知问题以 `docs/system-test-report.md` 为准。

## 数据库与环境

公共联调库使用 Oracle 19c，Service Name 为 `orclpdb1`。本地通过 SSH 隧道连接 `127.0.0.1:15210/orclpdb1`，真实账号、密码和私钥不进入版本控制。

2026-08-22 已实时确认共享库包含 24 张业务表，`003` 至 `006` 迁移全部生效，31 个账号均使用 Identity 密码哈希，默认地址、服务名称和评价关系完整性检查无异常。

连接配置、SSH 隧道、HTTPS 部署和权限边界统一见 `docs/environment-guide.md`；脚本执行顺序见 `database/oracle/README.md`；字段字典见 `docs/database_dictionary.md`。

## 构建检查

```powershell
dotnet build backend/CampusDelivery.sln
```

## 测试与质量验证

项目保留一个小型测试工程，覆盖并发抢单、任务状态机、三类任务字段、确认收货幂等、支付/评价事务、地址并发、结算状态机和报表生成导出等高价值规则，不使用大量无意义 CRUD 测试凑数。

在仓库根目录执行完整本地门禁：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-tests.ps1
```

2026-08-22 的 Release 结果为 39/39 项自动化测试通过；同时检查到 24 张关系表、46/46 个 POST Action 有防伪令牌、14 处仓储行锁语句，且 Controller/Service 未越过五层边界。

## 文档入口

| 文档 | 用途 |
| --- | --- |
| `docs/environment-guide.md` | 本地环境、SSH 隧道、共享库和 HTTPS 部署 |
| `docs/layered-architecture.md` | 五层架构、目录职责、命名和数据显示规则 |
| `docs/business-logic-overview.md` | 完整业务流程及必须遵守的状态、事务和完整性规则 |
| `docs/设计调整说明.md` | 原设计与当前实现的调整依据、建议修正项和扩展边界 |
| `database/oracle/README.md` | 数据库脚本、执行顺序和迁移状态 |
| `docs/database_dictionary.md` | 24 张业务表字段字典 |
| `docs/system-test-report.md` | 当前测试结论与剩余验收边界 |
| `docs/manual-system-test-guide.md` | 数据库端到端手工验收步骤 |

`docs/test-evidence/` 保存带日期的历史测试证据和原始输出，不作为当前环境配置说明。
