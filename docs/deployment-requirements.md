# 开发与联调环境说明

## 环境

| 项 | 要求 |
| --- | --- |
| .NET SDK | 9.0 |
| 后端框架 | ASP.NET Core MVC |
| 数据库 | Oracle 19c |
| PDB | `ORCLPDB1` |
| Service Name | `orclpdb1` |

## 运行后端

在仓库根目录执行：

```powershell
.\scripts\run-backend.ps1
```

或：

```powershell
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

默认地址来自 `launchSettings.json`：

```text
http://localhost:5227
https://localhost:7161
```

## Oracle 连接口径

云服务器公共联调库：

```text
Service Name: orclpdb1
```

本地通过 SSH 隧道连接云数据库：

```text
Host: 127.0.0.1
Port: 15210
Service Name: orclpdb1
User: 由服务器负责人提供
Password: 由服务器负责人提供
```

服务器后端通过环境变量 `ConnectionStrings__OracleDb` 覆盖仓库中的默认配置，真实数据库密码不写入仓库。

示例：

```powershell
$env:ConnectionStrings__OracleDb="User Id=<database_user>;Password=<database_password>;Data Source=localhost:15210/orclpdb1;"
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

应用默认配置文件：

```text
backend/src/CampusDelivery.Api/appsettings.json
```

本地个人覆盖配置文件：

```text
backend/src/CampusDelivery.Api/appsettings.Local.json
```

`appsettings.Local.json` 不纳入版本控制，仅用于个人联调。

## 数据库脚本

当前数据库脚本包括：

```text
database/oracle/campus_runner_oracle_schema.sql
database/oracle/002_init_base_data.sql
```

执行顺序：

```text
1. campus_runner_oracle_schema.sql
2. 002_init_base_data.sql
```

说明：

- `campus_runner_oracle_schema.sql` 会删除并重建当前用户下的业务表，只能在空库、重建开发库，或经数据库负责人确认后执行。
- `002_init_base_data.sql` 只插入基础运行数据，不插入完整业务演示数据。
- `003_init_test_data.sql` 当前未提供，不属于当前服务器前期搭建必须项。

## 联调地址

```text
/                 项目主页面
/Node             节点管理（当前联调环境只读）
/Database/Status  数据库连接检测
```

## 账号说明

| 账号 | 用途 |
| --- | --- |
| `APPUSER` | 服务器后端运行账号，也是当前业务表拥有者 |
| `APPREAD` | 只读账号，用于查看表结构、字段和基础数据 |

默认不在公开文档中记录真实用户名密码；如需使用，由服务器负责人单独提供。

## 构建验证

```powershell
dotnet build backend/CampusDelivery.sln
```
