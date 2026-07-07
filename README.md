# 校园中转分发与跑腿服务管理系统

本仓库已统一为 ASP.NET Core MVC 五层架构，不再使用 Vue/Vite 前端作为主入口。

## 架构分层

```text
表现层 Presentation
  backend/src/CampusDelivery.Api/Presentation/Views
  backend/src/CampusDelivery.Api/Presentation/ViewModels
  backend/src/CampusDelivery.Api/Presentation/wwwroot

控制层 Controllers
  backend/src/CampusDelivery.Api/Controllers

业务层 Services
  backend/src/CampusDelivery.Api/Services

持久层 Repositories / Persistence
  backend/src/CampusDelivery.Api/Repositories
  backend/src/CampusDelivery.Api/Persistence

数据库层 database/oracle
  database/oracle/campus_runner_oracle_schema.sql
```

调用方向固定为：

```text
Razor View -> Controller -> Service -> Repository -> OracleConnectionFactory -> Oracle
```

## 数据库

本机开发默认 Oracle PDB 服务名为 `XEPDB1`：

```text
User Id=APPUSER;Password=App123456;Data Source=localhost:1521/XEPDB1;
```

24 张表的标准建表脚本是：

```text
database/oracle/campus_runner_oracle_schema.sql
```

当前仓库没有 `002_init_base_data.sql` 和 `003_init_test_data.sql`。也就是说现在只维护表结构脚本，基础数据和测试数据还没有单独脚本。

如果需要初始化数据库，可先创建 `APPUSER`，再用 `APPUSER` 执行上述 24 表脚本。

## 数据值与页面显示

数据库中的枚举/状态值统一存英文代码，页面输出中文名称。例如：

| 数据库存储 | 页面显示 |
| --- | --- |
| `GATE` | 校门 |
| `STATION` | 驿站 |
| `DISTRIBUTION` | 分发点 |
| `NORMAL` | 正常 |
| `CLOSED` | 关闭 |

这个转换放在业务层或展示模型中完成，Repository 只负责读写数据库英文值。

## 运行后端

```powershell
cd D:\delivery-backend
.\scripts\run-backend.ps1
```

或直接：

```powershell
dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

默认 MVC 入口：

```text
/
/Node
/Database/Status
```

## 当前已落地模块

`Node` 模块已经按五层完整打通：

```text
Presentation/Views/Node/*
Presentation/ViewModels/Node*
Controllers/NodeController.cs
Services/NodeService.cs
Repositories/NodeRepository.cs
database/oracle/campus_runner_oracle_schema.sql 中的 nodes 表
```

`Node` 页面提交到数据库的是英文值，列表页显示的是中文名称。

其他模块可以按同样模式继续扩展。

## 构建检查

```powershell
dotnet build backend/CampusDelivery.sln
```
