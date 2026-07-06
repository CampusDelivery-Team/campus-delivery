# 校园中转分发与跑腿服务管理系统

本项目是校园中转分发与跑腿服务管理系统，采用 **ASP.NET Core + C# + Oracle 19c** 作为后端与数据库基础，并按五层架构组织代码：

```text
表现层 -> 控制层 -> 业务层 -> 持久层（数据访问层） -> 数据库层
```

当前仓库保留 `frontend/` 作为项目主页面和演示入口；后续如果课程要求使用 ASP.NET Core MVC 页面，则页面文件统一放到 `backend/src/CampusDelivery.Api/Presentation/Views/`。

## 项目结构

```text
delivery-backend/
├── backend/
│   ├── CampusDelivery.sln
│   └── src/
│       └── CampusDelivery.Api/
│           ├── Controllers/          # 控制层
│           ├── Services/             # 业务层
│           ├── Repositories/         # 持久层：业务 Repository
│           ├── Persistence/          # 持久层：Oracle 连接和公共数据访问
│           ├── Presentation/         # 表现层：MVC Views、ViewModels、wwwroot
│           ├── Models/               # 业务模型
│           ├── Dtos/                 # API/检查接口 DTO
│           ├── Program.cs
│           └── appsettings.json
├── frontend/                         # 当前项目主页面
├── database/
│   └── oracle/                       # Oracle 脚本
├── docs/                             # 项目文档
├── scripts/                          # 辅助脚本
└── README.md
```

## 环境要求

- Visual Studio 2022 或 VS Code
- .NET SDK 9.0
- Node.js 20.19.0 或更高版本
- npm
- Oracle Database 19c

## 本地数据库

当前本地数据库连接信息：

```text
Oracle 服务：ORCLPDB
用户名：APPUSER
密码：App123456
连接地址：localhost:1521/ORCLPDB
```

后端连接串位置：

```text
backend/src/CampusDelivery.Api/appsettings.json
```

当前连接串：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=APPUSER;Password=App123456;Data Source=localhost:1521/ORCLPDB;"
  }
}
```

## 启动后端

```powershell
cd D:\delivery-backend\backend\src\CampusDelivery.Api
dotnet restore
dotnet run
```

后端默认地址：

```text
http://localhost:5227
```

检查接口：

```text
http://localhost:5227/api/health
http://localhost:5227/api/DbTest/ping
```

## 启动前端主页面

如果 `5173` 被占用，建议使用 `5174`：

```powershell
cd D:\delivery-backend\frontend
npm.cmd install
npm.cmd run dev -- --port 5174
```

打开：

```text
http://127.0.0.1:5174
```

## 推荐开发顺序

```text
1. 确认 Oracle 和 APPUSER 可用
2. 启动后端并检查 /api/health
3. 检查 /api/DbTest/ping
4. 启动前端主页面
5. 按五层架构开发各业务模块
6. 每次提交前运行 dotnet build
```

## 文档索引

- [五层架构说明](docs/layered-architecture.md)
- [MVC 接口与模块开发规范](docs/api-frontend-backend-spec.md)
- [命名规范](docs/naming-conventions.md)
- [部署与开发要求](docs/deployment-requirements.md)
- [数据库层说明](database/README.md)
- [Oracle 脚本说明](database/oracle/README.md)

## 构建检查

```powershell
dotnet build D:\delivery-backend\backend\CampusDelivery.sln
```

如果后端程序正在运行，可能会锁住 `bin/Debug` 下的 dll/exe。此时先停止正在运行的后端，再重新构建。
