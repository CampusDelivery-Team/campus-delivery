# 环境、部署与数据库连接指南

本文是项目环境和部署的唯一权威说明，合并了原来的开发环境、部署要求和 SSH 隧道文档。最后核验日期为 2026-08-22。

## 当前环境

| 项目 | 当前配置 |
| --- | --- |
| 应用框架 | ASP.NET Core MVC，.NET 9 |
| 数据库 | Oracle 19c |
| PDB / Service Name | `ORCLPDB1` / `orclpdb1` |
| 公网入口 | `https://47.116.60.57/` |
| 公网端口 | HTTPS 默认端口 443 |
| 本地应用默认地址 | `http://localhost:5227`、`https://localhost:7161` |
| 本地 Oracle 隧道地址 | `127.0.0.1:15210/orclpdb1` |

公网不直接开放应用内部端口 5227。外部请求通过 HTTPS 443 进入反向代理，再转发到 ASP.NET Core 应用，因此不要再使用旧地址 `http://47.116.60.57:5227/`。

2026-08-22 已确认：首页和登录页返回 200；需要登录的任务、评价页面会正常跳转到登录页。

## 本地运行

仓库要求 .NET SDK 9，`global.json` 固定当前 SDK 基准。在仓库根目录执行：

```powershell
.\scripts\run-backend.ps1
```

或：

```powershell
dotnet restore backend/CampusDelivery.sln
dotnet build backend/CampusDelivery.sln
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

应用入口：

```text
backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

## 连接共享 Oracle

共享 Oracle 不直接开放公网数据库端口。本地程序连接 `127.0.0.1:15210`，SSH 将请求转发到服务器内部的 `127.0.0.1:1521`。

```text
本地应用 -> 127.0.0.1:15210 -> SSH 隧道 -> 服务器 Oracle 127.0.0.1:1521
```

首次使用时生成独立 SSH 密钥，并将公钥交给服务器负责人：

```powershell
ssh-keygen -t ed25519 `
  -f "$env:USERPROFILE\.ssh\campus_dbtunnel_ed25519" `
  -C "学号-姓名-oracle-tunnel"
```

建立隧道：

```powershell
ssh -N `
  -i "$env:USERPROFILE\.ssh\campus_dbtunnel_ed25519" `
  -L 15210:127.0.0.1:1521 `
  dbtunnel@47.116.60.57
```

命令保持运行且没有输出是正常现象。另开 PowerShell 验证：

```powershell
Test-NetConnection 127.0.0.1 -Port 15210
```

`TcpTestSucceeded` 必须为 `True`。

数据库工具使用以下参数：

| 参数 | 值 |
| --- | --- |
| Host | `127.0.0.1` |
| Port | `15210` |
| Service Name | `orclpdb1` |
| 用户名和密码 | 由服务器负责人提供 |

## 应用连接配置

仓库中的 `appsettings.json` 不保存真实凭据。本地开发可创建已被 Git 忽略的：

```text
backend/src/CampusDelivery.Api/appsettings.Local.json
```

示例：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=<database_user>;Password=<database_password>;Data Source=127.0.0.1:15210/orclpdb1;"
  }
}
```

也可以使用环境变量覆盖：

```powershell
$env:ConnectionStrings__OracleDb="User Id=<database_user>;Password=<database_password>;Data Source=127.0.0.1:15210/orclpdb1;"
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

`appsettings.Local.json` 只在 Development 环境加载。真实账号、密码、私钥和连接串不得提交到仓库。

## 数据库账号边界

| 账号类型 | 用途 |
| --- | --- |
| `APPUSER` | 24 张业务表和 14 个序列的所有者 |
| 应用成员账号 | 通过授权和同义词访问 `APPUSER` 对象 |
| `APPREAD` | 只读查询和结构检查 |

2026-08-22 已通过成员应用账号实时确认：24 张业务表具备 `SELECT/INSERT/UPDATE/DELETE` 权限，14 个序列可查询，38 个私有同义词可用。

共享库只能在明确授权范围内写入。结构重建、批量清理、迁移和测试数据写入必须先备份并由数据库负责人确认。

## 当前迁移状态

共享库现有 24 张业务表。2026-08-22 已只读核验以下迁移全部生效：

- `003_add_account_lifecycle.sql`：账号状态约束已启用并验证；
- `004_add_review_integrity.sql`：评价任务列、唯一约束、复合外键和索引有效；
- `005_hash_user_passwords.sql`：31 个账号均使用 Identity 密码哈希；
- `006_harden_business_integrity.sql`：单默认地址和服务名称唯一索引有效。

重复默认地址、重复服务名称、评价任务空值及评价接派关系异常均为 0。详细脚本用途见 `database/oracle/README.md`。

## 部署核验

公网入口：

```text
https://47.116.60.57/
```

基础检查：

```powershell
Test-NetConnection 47.116.60.57 -Port 443
curl.exe -I https://47.116.60.57/
```

未登录访问受保护页面时返回 302 并跳转 `/Auth/Login` 是正常行为。发布时应同时记录部署分支和 Git commit；仅看到页面可访问不能证明服务器与指定提交完全一致。

## 常见问题

- `15210` 连接失败：确认 SSH 命令仍在运行、本地端口未被占用、密钥和隧道账号正确。
- Oracle 报服务名错误：使用 Service Name `orclpdb1`，不要把它当 SID。
- 本地能连库但应用不能：确认运行环境为 Development，或改用 `ConnectionStrings__OracleDb` 环境变量。
- 公网 443 可达但页面报错：检查反向代理、应用容器日志和数据库连接配置。
- 旧的 `:5227` 公网地址超时：该内部端口当前不对公网开放，应使用 HTTPS 入口。
