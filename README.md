# 校园中转分发与跑腿服务管理系统

当前仓库已经统一为 ASP.NET Core MVC 五层架构。项目主入口是后端 MVC 页面，不再包含 Vue/Vite 前端。

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

```powershell
cd D:\delivery-backend
.\scripts\run-backend.ps1
```

或：

```powershell
cd D:\delivery-backend
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
| `/` | 蓝粉色项目主页面，作为系统门户和其他模块入口 |
| `/Node` | 节点管理示例模块，已打通 MVC 五层 |
| `/Database/Status` | Oracle 连接检测页面 |

## 数据库

本机开发默认 Oracle PDB：

```text
localhost:1521/XEPDB1
```

连接字符串：

```text
User Id=APPUSER;Password=App123456;Data Source=localhost:1521/XEPDB1;
```

当前唯一标准建表脚本：

```text
database/oracle/campus_runner_oracle_schema.sql
```

该脚本包含 24 张业务表。当前仓库没有 `002_init_base_data.sql` 和 `003_init_test_data.sql`。

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
