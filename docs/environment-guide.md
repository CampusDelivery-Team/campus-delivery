# 开发与联调环境说明

本文档面向校园中转分发与跑腿服务管理系统的全体开发成员，用于说明当前项目的开发环境、云服务器联调环境、数据库连接方式和环境一致性要求。

当前项目采用“本地开发 + GitHub 协作 + 云服务器联调”的方式推进。开发成员平时在自己的电脑上完成代码开发，代码合并到 `main` 分支后，再由云服务器运行 `main` 分支代码，供全组统一查看和验证效果。

本文档重点说明：开发人员本地环境不需要和云服务器完全一样，但**项目分支、.NET 版本、数据库表结构、枚举值、连接串口径和代码分层方式**必须和公共联调环境保持一致。

**截至 2026.7.7，服务器当前运行 `main` 分支。后续如有调整，以项目负责人通知或仓库说明为准。**

------

## 1. 当前联调基准

当前公共联调环境以 GitHub 仓库中的 `main` 分支为准。

也就是说：

```text
服务器运行 main 分支代码
数据库以云服务器 Oracle 19c 为准
表结构以 database/oracle/campus_runner_oracle_schema.sql 为准
基础数据以 database/oracle/002_init_base_data.sql 为准
数据库内部枚举值以英文代码为准
页面中文显示由后端映射完成
```

------

## 2. 当前云服务器环境

当前云服务器主要用于公共联调和课程演示。

### 2.1 基本环境

| 项目 | 当前配置 |
| --- | --- |
| 操作系统 | CentOS 7 |
| 数据库 | Oracle 19c |
| 后端框架 | ASP.NET Core MVC |
| .NET 版本 | .NET 9 |
| 后端运行方式 | Docker 容器 |
| 当前运行分支-2026.7.7 | `main` |
| 后端容器名 | `campus-delivery-web` |
| 公网访问端口 | `5227` |
| PDB | `ORCLPDB1` |
| 数据库服务名 | `orclpdb1` |
| 当前业务表数量 | 24 张 |

当前访问地址：

```text
http://47.116.60.57:5227/
```

数据库连接检测页面：

```text
http://47.116.60.57:5227/Database/Status
```

------

## 3. 当前云服务器结构

当前云服务器上的部署结构可以理解为：

```text
云服务器
├── Oracle 19c 数据库
│   └── ORCLPDB1 / orclpdb1 / 24 张业务表
│
└── Docker 容器
    └── campus-delivery-web
        └── .NET 9
            └── ASP.NET Core MVC 后端项目
```

Oracle 数据库运行在云服务器宿主机上。ASP.NET Core MVC 后端运行在 Docker 容器中。后端通过 Oracle 连接串访问服务器本机 Oracle 数据库。

------

## 4. 后端运行环境

### 4.1 后端技术栈

当前后端使用：

```text
ASP.NET Core MVC
C#
Razor View
自定义 CSS
Oracle.ManagedDataAccess.Core
Oracle 19c
```

页面样式当前以自定义 CSS 为主，后续模块如需统一表单或表格样式，可按项目规范引入 Bootstrap 或复用现有样式。

### 4.2 项目入口

解决方案文件：

```text
backend/CampusDelivery.sln
```

后端项目入口：

```text
backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

### 4.3 本地运行命令

开发人员在自己电脑上进入项目根目录后，可以执行：

```bash
dotnet restore backend/CampusDelivery.sln
dotnet build backend/CampusDelivery.sln
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

------

## 5. .NET 版本要求

当前项目目标框架为：

```text
.NET 9
```

开发人员本地建议安装 `.NET 9 SDK`。如本地只有 .NET 6、.NET 7 或 .NET 8，可能无法直接运行当前项目。

------

## 6. Oracle 驱动与数据库访问

当前项目通过 Oracle 官方 .NET 驱动访问数据库：

```text
Oracle.ManagedDataAccess.Core 23.8.0
```

推荐分层方式：

```text
Controller：接收请求，调用 Service，返回页面或结果
Service：处理业务逻辑，组织 ViewModel
Repository：执行 SQL，读写 Oracle 数据库
ViewModel：准备页面展示字段
View：展示页面，不直接处理复杂业务逻辑
```

------

## 7. 数据库环境

### 7.1 云服务器数据库信息

当前公共联调数据库为 Oracle 19c。

| 项目 | 当前值 |
| --- | --- |
| 数据库版本 | Oracle 19c |
| CDB | `ORCLCDB` |
| PDB | `ORCLPDB1` |
| 服务名 | `orclpdb1` |
| 当前业务表数量 | 24 张 |

开发人员需要注意：当前**实际可用服务名**是：

```text
orclpdb1
```

在连接工具中，应选择 `Service Name`，不要选择 `SID`。

### 7.2 数据库账号说明

当前公共联调数据库中，账号按用途区分：

| 账号 | 用途 | 是否默认提供给开发人员 |
| --- | --- | --- |
| `APPUSER` | 服务器后端运行账号，也是当前业务表拥有者 | 否 |
| `APPREAD` | 只读账号，用于查看表结构、字段和基础数据 | 是，只读查询场景按需提供 |

普通开发人员如只需要查看表结构、查询基础数据或调试 `SELECT` 语句，使用 `APPREAD`。需要调试新增、修改、删除等写入逻辑的同学，应单独向服务器负责人说明用途，再提供具备写权限的数据库账号。

数据库账号和密码由服务器负责人单独提供，不写入 GitHub 文档、README、代码、提交记录或公开沟通记录中。

------

## 8. 数据库连接串口径

### 8.1 服务器后端容器使用的连接方式

云服务器上的后端容器连接服务器本机 Oracle，连接地址使用服务器内部地址：

```text
Data Source=localhost:1521/orclpdb1
```

完整格式为：

```text
User Id=<数据库用户名>;Password=<数据库密码>;Data Source=localhost:1521/orclpdb1;
```

### 8.2 本地通过 SSH 隧道连接云数据库

开发人员在自己电脑上通过 SSH 隧道连接云服务器 Oracle 时，本地连接串使用：

```text
Data Source=localhost:15210/orclpdb1
```

完整格式为：

```text
User Id=<数据库用户名>;Password=<数据库密码>;Data Source=localhost:15210/orclpdb1;
```

普通查询和查看基础数据时，一般使用 `APPREAD`。需要调试新增、修改、删除等写入功能时，应单独向服务器负责人申请具备写权限的数据库账号。

------

## 9. appsettings.json 与环境变量

云服务器运行后端时，真实 Oracle 连接串通过环境变量覆盖：

```text
ConnectionStrings__OracleDb
```

Windows PowerShell 示例：

```powershell
$env:ConnectionStrings__OracleDb="User Id=<数据库用户名>;Password=<数据库密码>;Data Source=localhost:15210/orclpdb1;"

dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

Linux 或 macOS 示例：

```bash
export ConnectionStrings__OracleDb="User Id=<数据库用户名>;Password=<数据库密码>;Data Source=localhost:15210/orclpdb1;"

dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

不要为了本地调试，把真实数据库密码直接写入 `appsettings.json` 后提交到 GitHub。

------

## 10. 数据库脚本基准

数据库脚本位于：

```text
database/oracle/
```

当前主要脚本如下：

- `database/oracle/campus_runner_oracle_schema.sql`
- `database/oracle/002_init_base_data.sql`

其中建表脚本包含删表和重建逻辑，普通开发人员不应在公共联调数据库上随意执行。

------

## 11. 数据库枚举值口径

当前数据库内部枚举值**统一使用英文代码**。页面显示中文名称时，由后端 Service、ViewModel 或显示名称映射逻辑完成。

推荐分层：

```text
Repository：读写数据库中的英文代码
Service / ViewModel：准备中文显示字段
Razor View：展示中文字段
```

------

## 12. 当前项目结构

当前 `main` 分支采用 ASP.NET Core MVC 结构，后端主项目目录为：

```text
backend/src/CampusDelivery.Api/
```

开发人员应尽量按现有目录结构开发，不要随意新建另一套平行结构。

------

## 13. 本地开发如何对齐服务器环境

本地开发不要求操作系统和服务器完全一致，但以下内容必须和服务器联调环境保持一致：

```text
使用与服务器相同的分支作为开发基准
项目目标框架为 .NET 9
使用当前项目目录结构
数据库表结构按 campus_runner_oracle_schema.sql
基础数据按 002_init_base_data.sql
数据库服务名按 orclpdb1 口径理解
数据库枚举值使用英文代码
数据库访问代码按 Oracle 方言编写
不要把本地连接串和真实密码提交到 GitHub
```

------

## 14. 本地开发与服务器联调的区别

| 场景 | 代码来源 | 数据库连接 | 用途 |
| --- | --- | --- | --- |
| 本地开发 | 个人功能分支 | 本地 Oracle 或 SSH 隧道 | 写代码、调试模块 |
| 服务器联调 | `main` 分支 | 云服务器 Oracle | 全组查看合并效果 |
| 最终演示 | 稳定的 `main` 或演示分支 | 云服务器 Oracle | 课程展示 |

不要把服务器当成个人开发机。

------

## 15. 云服务器使用边界

组员可以直接访问：

```text
http://47.116.60.57:5227/
```

但应遵循：

```text
看公共效果：访问云服务器
开发个人模块：在自己电脑本地开发
本地开发需要数据库：通过 SSH 隧道连接云 Oracle
功能完成后：提交 GitHub 并合并到 main
合并后联调：服务器更新 main 后统一查看
```

------

## 16. 当前服务器已确认状态

当前云服务器已经确认以下状态：

```text
main 分支代码可以运行
002_init_base_data.sql 已经合入 main 分支
Docker 后端容器 campus-delivery-web 正常运行
/Database/Status 可以访问
后端可以连接 Oracle
users 表当前基础数据数量为 3
/Node 页面可以访问，当前联调环境下新增、编辑、删除入口已关闭
```

------

## 17. 环境说明总结

当前项目的开发和联调环境可以概括为：

```text
本地电脑用于个人开发
GitHub 用于代码协作
main 分支作为公共联调基准
云服务器运行 main 分支代码
云服务器 Oracle 作为公共联调数据库
后端通过 ASP.NET Core MVC 提供页面和服务
数据库内部使用英文枚举代码
页面通过后端映射显示中文
需要本地连接云数据库时走 SSH 隧道
```

**截至 2026.7.7，服务器当前运行 `main` 分支。后续如有调整，以项目负责人通知或仓库说明为准。**
