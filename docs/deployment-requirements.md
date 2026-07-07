# 部署与运行要求

## 后端

- .NET SDK: 9.0
- 项目入口: `backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj`
- 架构: ASP.NET Core MVC

运行：

```powershell
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

## Oracle

本机开发默认配置：

```text
服务名：XEPDB1
地址：localhost:1521/XEPDB1
用户：APPUSER
密码：App123456
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

标准 24 表建表脚本：

```text
database/oracle/campus_runner_oracle_schema.sql
```

当前没有：

```text
database/oracle/002_init_base_data.sql
database/oracle/003_init_test_data.sql
```

如果后续新增这两个脚本，执行顺序应为：

```text
1. campus_runner_oracle_schema.sql
2. 002_init_base_data.sql
3. 003_init_test_data.sql
```

## 验证

启动后访问：

```text
/
/Database/Status
/Node
```

页面显示规则：数据库存英文代码，MVC 页面输出中文名称。
