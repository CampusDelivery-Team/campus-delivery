# 部署与开发要求

## 1. 文档目的

本文档用于统一校园跑腿系统的本地开发、数据库初始化、后续部署和联调要求。

项目当前按五层架构开发：

```text
表现层 -> 控制层 -> 业务层 -> 持久层（数据访问层） -> 数据库层
```

当前仓库保留 `frontend/` 作为主页面和演示入口；后续 MVC 页面放到 `backend/src/CampusDelivery.Api/Presentation/Views/`。

## 2. 项目组成

| 模块 | 目录 | 说明 |
| --- | --- | --- |
| 表现层 | `frontend/`、`backend/src/CampusDelivery.Api/Presentation/` | 项目主页面、MVC View、ViewModel、静态资源 |
| 控制层 | `backend/src/CampusDelivery.Api/Controllers/` | Controller 和路由入口 |
| 业务层 | `backend/src/CampusDelivery.Api/Services/` | 业务规则、权限判断、状态流转 |
| 持久层 | `backend/src/CampusDelivery.Api/Repositories/`、`backend/src/CampusDelivery.Api/Persistence/` | Repository、SQL、Oracle 连接 |
| 数据库层 | `database/`、`database/oracle/` | Oracle 建表脚本、基础数据、测试数据 |
| 文档 | `docs/` | 架构、接口、命名和部署说明 |

## 3. 环境要求

### 3.1 后端环境

- .NET SDK 9.0
- Visual Studio 2022 或 VS Code
- Oracle.ManagedDataAccess.Core
- Oracle Database 19c

后端默认地址：

```text
http://localhost:5227
```

检查接口：

```text
GET /api/health
GET /api/DbTest/ping
```

### 3.2 前端环境

- Node.js 20.19.0 或更高版本
- npm

当前主页面启动地址建议：

```text
http://127.0.0.1:5174
```

### 3.3 数据库环境

当前本地 Oracle 配置：

```text
服务名：ORCLPDB
用户：APPUSER
密码：App123456
地址：localhost:1521/ORCLPDB
```

后端连接串：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=APPUSER;Password=App123456;Data Source=localhost:1521/ORCLPDB;"
  }
}
```

## 4. 本地运行顺序

### 4.1 启动 Oracle

确认本机 Oracle 服务正在运行：

```text
OracleServiceORCL
OracleOraDB19Home1TNSListener
```

### 4.2 启动后端

```powershell
cd D:\delivery-backend\backend\src\CampusDelivery.Api
dotnet run
```

验证：

```text
http://localhost:5227/api/health
http://localhost:5227/api/DbTest/ping
```

### 4.3 启动前端主页面

```powershell
cd D:\delivery-backend\frontend
npm.cmd install
npm.cmd run dev -- --port 5174
```

打开：

```text
http://127.0.0.1:5174
```

## 5. 开发要求

### 5.1 五层调用要求

允许：

```text
View / 前端页面 -> Controller -> Service -> Repository -> OracleConnectionFactory -> Oracle
```

禁止：

```text
View 直接访问数据库
Controller 直接写复杂 SQL
Service 直接返回页面
Repository 处理页面跳转
```

### 5.2 数据库要求

1. 表结构由数据库层统一维护。
2. 修改表结构前必须同步更新 SQL 脚本和文档。
3. 业务状态枚举统一使用中文。
4. 基础数据和测试数据建议分脚本维护。
5. 每个模块完成后必须能用 SQL Developer 验证数据库变化。

### 5.3 代码提交要求

提交前必须确认：

```powershell
dotnet build D:\delivery-backend\backend\CampusDelivery.sln
```

如果后端正在运行导致构建文件被锁住，需要先停止后端程序再构建。

## 6. 后续部署要求

### 6.1 数据库部署

部署到服务器或阿里云时，需要准备：

1. Oracle 数据库实例。
2. 数据库服务名、地址和端口。
3. 业务用户 `APPUSER` 或正式业务用户。
4. 建表脚本。
5. 基础数据脚本。
6. 网络白名单和安全组。

部署流程：

```text
创建数据库实例
创建业务用户
授予 CONNECT / RESOURCE 等权限
执行建表脚本
执行基础数据脚本
验证核心表
```

### 6.2 后端部署

后端部署后，只修改连接串，不修改业务代码。

生产环境建议使用环境变量或服务器配置保存数据库密码，不要把正式密码提交到仓库。

### 6.3 表现层部署

如果使用当前 `frontend/` 主页面：

1. 构建前端静态文件。
2. 配置后端 API 地址或反向代理。
3. 保证 `/api/health` 和 `/api/DbTest/ping` 可访问。

如果后续改为完整 MVC 页面：

1. 页面放在 `Presentation/Views/`。
2. 静态资源放在 `Presentation/wwwroot/` 或 ASP.NET Core 默认 `wwwroot/`。
3. 由 Controller 返回 View。

## 7. 验收要求

部署或开发阶段至少满足：

1. 后端可以启动。
2. `/api/health` 返回正常。
3. `/api/DbTest/ping` 可以连接 Oracle。
4. `APPUSER` 下存在 24 张业务表。
5. 主页面可以打开。
6. 每个业务模块按五层结构放置文件。
7. 中文枚举和数据库约束保持一致。
8. `dotnet build` 通过。
