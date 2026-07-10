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

## 当前已落地页面

| 地址 | 说明 |
| --- | --- |
| `/` | 项目主页面，作为系统门户和其他模块入口 |
| `/Node` | 节点管理示例模块，当前联调环境按只读方式展示 |
| `/Database/Status` | Oracle 连接检测页面 |

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
```

其中：

- `campus_runner_oracle_schema.sql`：建表脚本，包含 `DROP TABLE` 和重建表逻辑，只用于初始化空库或确认需要重建时执行。
- `002_init_base_data.sql`：基础运行数据脚本，插入管理员、普通用户、跑腿员、节点、服务类型和服务节点规则。
- `003_init_test_data.sql`：当前尚未提供，后续业务流程稳定后再补充演示数据。

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
