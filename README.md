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

## 数据库连接

当前公共联调数据库使用 Oracle 19c，服务名为：

```text
orclpdb1
```

开发人员本地连接云服务器 Oracle 时，应先建立 SSH 隧道，然后使用：

```text
Host: 127.0.0.1
Port: 15210
Service Name: orclpdb1
User: 由服务器负责人提供
Password: 由服务器负责人提供
```

本地后端连接串示例：

```text
User Id=<database_user>;Password=<database_password>;Data Source=localhost:15210/orclpdb1;
```

本地运行前可通过环境变量覆盖连接串：

```powershell
$env:ConnectionStrings__OracleDb="User Id=<database_user>;Password=<database_password>;Data Source=localhost:15210/orclpdb1;"
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

仓库支持可选的 `appsettings.Local.json` 本地覆盖文件，适合个人本机调试使用；该文件已被 `.gitignore` 忽略，不要提交真实凭据。

数据库账号和密码不写入 GitHub 文档、README、代码或提交记录。

## 数据库账号说明

当前数据库账号按用途区分：

| 账号 | 用途 | 是否默认提供给开发人员 |
| --- | --- | --- |
| `APPUSER` | 服务器后端运行账号，也是当前业务表拥有者 | 否 |
| `APPREAD` | 只读账号，用于查看表结构、字段和基础数据 | 是，只读查询场景按需提供 |

普通开发人员如只需要查看表结构和查询基础数据，使用 `APPREAD`。需要调试新增、修改、删除等写入逻辑的同学，应单独向服务器负责人说明用途后再提供相应账号。

## 数据库脚本

当前数据库脚本包括：

```text
database/oracle/campus_runner_oracle_schema.sql
database/oracle/002_init_base_data.sql
database/oracle/003_add_account_lifecycle.sql
database/oracle/004_add_review_integrity.sql
database/oracle/005_hash_user_passwords.sql
database/oracle/006_harden_business_integrity.sql
```

其中：

- `campus_runner_oracle_schema.sql`：建表脚本，包含 `DROP TABLE` 和重建表逻辑，只用于初始化空库或确认需要重建时执行。
- `002_init_base_data.sql`：基础运行数据脚本，插入管理员、普通用户、跑腿员、节点、服务类型和服务节点规则。
- `003_add_account_lifecycle.sql`：为既有数据库补充账号生命周期状态与相关约束。
- `004_add_review_integrity.sql`：为评价数据补充唯一性和接派关联完整性约束。
- `005_hash_user_passwords.sql`：既有演示账号密码哈希迁移脚本。
- `006_harden_business_integrity.sql`：为默认地址和服务类型名称增加并发下的最终唯一性保护。

基础脚本不写入完整业务闭环数据；端到端测试数据应按 `docs/manual-system-test-guide.md` 在隔离测试库中通过页面操作形成。

## 数据显示规则

数据库保存英文代码，页面显示中文名称。例如：

| 数据库存储 | 页面显示 |
| --- | --- |
| `GATE` | 校门 |
| `STATION` | 驿站 |
| `DISTRIBUTION` | 分发点 |
| `NORMAL` | 正常 |
| `CLOSED` | 关闭 |

Repository 只读写英文代码；Service/ViewModel 负责准备中文显示字段；Razor View 只展示中文字段。

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

2026-08-15 的本地结果为 39/39 项自动化测试通过；同时检查到 24 张关系表、46/46 个 POST Action 有防伪令牌、14 处仓储行锁语句，且 Controller/Service 未越过五层边界。
