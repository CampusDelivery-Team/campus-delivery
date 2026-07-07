# 部署与运行要求

## 环境

| 项 | 要求 |
| --- | --- |
| .NET SDK | 9.0 |
| 后端框架 | ASP.NET Core MVC |
| 数据库 | Oracle |
| 默认 PDB | `XEPDB1` |

## 运行后端

```powershell
cd D:\delivery-backend
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

## Oracle 配置

本机开发默认配置：

```text
服务名：XEPDB1
地址：localhost:1521/XEPDB1
用户：APPUSER
密码：App123456
```

应用配置文件：

```text
backend/src/CampusDelivery.Api/appsettings.json
```

连接字符串：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=APPUSER;Password=App123456;Data Source=localhost:1521/XEPDB1;"
  }
}
```

## 数据库脚本

当前唯一标准建表脚本：

```text
database/oracle/campus_runner_oracle_schema.sql
```

当前仓库没有：

```text
database/oracle/002_init_base_data.sql
database/oracle/003_init_test_data.sql
```

如后续新增初始化脚本，建议执行顺序：

```text
1. campus_runner_oracle_schema.sql
2. 002_init_base_data.sql
3. 003_init_test_data.sql
```

## 验证地址

```text
/                 项目主页面
/Node             节点管理
/Database/Status  数据库连接检测
```

## 构建验证

```powershell
dotnet build backend/CampusDelivery.sln
```
