# CampusDelivery

校园中转分发与跑腿服务管理系统。当前仓库采用前后端分离结构：

- 前端：`Vue 3 + Vite + TypeScript`
- 后端：`ASP.NET Core Web API`
- 数据库：`Oracle`

## 1. 项目结构

```text
CampusDelivery/
├─ frontend/                  # Vue 3 前端
├─ backend/                   # .NET 后端
│  ├─ TaskSystem.Api/         # Web API 项目
│  └─ campus_db.sql           # Oracle 建表脚本
├─ .gitignore
└─ README.md
```

## 2. 环境要求

请先确认本机安装了以下环境：

- Node.js：`20.19.0` 或更高版本
- npm：随 Node.js 一起安装
- .NET SDK：`9.0`
- Oracle Database：建议使用本地 `Oracle XE / 18c+`
- 一个可连接 Oracle 的数据库工具：
  `SQL Developer`、`Navicat`、`DBeaver`、`PL/SQL Developer` 均可

可用下面的命令快速检查：

```powershell
node -v
npm -v
dotnet --version
```

## 3. 第一次运行前必须知道的配置

当前项目默认使用以下本地配置：

- 前端开发地址：`http://127.0.0.1:5173`
- 后端开发地址：`http://localhost:5227`
- 后端 HTTPS 地址：`https://localhost:7161`
- Oracle 连接串位置：
  [backend/TaskSystem.Api/appsettings.json](backend/TaskSystem.Api/appsettings.json)

当前默认连接串如下：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=APPUSER;Password=App123456;Data Source=localhost:1521/XEPDB1;"
  }
}
```

如果你的 Oracle 用户名、密码、服务名或端口不同，后面第 5 步需要改成你自己的配置。

## 4. 初始化 Oracle 数据库

### 4.1 启动 Oracle 服务

确保本地 Oracle 实例已经启动，并且你知道以下信息：

- 主机：通常是 `localhost`
- 端口：通常是 `1521`
- Service Name：当前项目默认写的是 `XEPDB1`

### 4.2 创建业务用户（如果还没有）

如果你已经有自己的业务用户，可以跳过这一步。  
如果没有，可以用管理员账号连接 Oracle 后执行：

```sql
CREATE USER APPUSER IDENTIFIED BY App123456;
GRANT CONNECT, RESOURCE TO APPUSER;
ALTER USER APPUSER QUOTA UNLIMITED ON USERS;
```

如果你的 Oracle 版本或权限策略不同，`RESOURCE` 角色不可用时，可以改为按需授予建表权限。

### 4.3 执行建表脚本

使用数据库工具连接到 `APPUSER`，执行：

[backend/campus_db.sql](backend/campus_db.sql)

脚本会创建当前项目需要的核心表，例如：

- `users`
- `tasks`
- `runners`
- `nodes`
- `payments`
- `reviews`

### 4.4 验证建表是否成功

执行：

```sql
SELECT COUNT(*) FROM users;
```

如果能正常返回结果，哪怕是 `0`，也说明表已经创建成功。  
当前后端测试接口就是通过查询 `users` 表来验证数据库连通性的。

## 5. 配置后端连接 Oracle

打开：

[backend/TaskSystem.Api/appsettings.json](backend/TaskSystem.Api/appsettings.json)

根据你自己的 Oracle 实例修改连接串。常见格式如下：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=你的用户名;Password=你的密码;Data Source=localhost:1521/XEPDB1;"
  }
}
```

如果你不是本机数据库，也可以写成远程地址：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=your_user;Password=your_password;Data Source=192.168.1.10:1521/XEPDB1;"
  }
}
```

## 6. 启动后端

进入后端项目目录：

```powershell
cd backend/TaskSystem.Api
```

首次运行建议先还原依赖：

```powershell
dotnet restore
```

启动后端：

```powershell
dotnet run
```

当前项目的启动端口定义在：

[backend/TaskSystem.Api/Properties/launchSettings.json](backend/TaskSystem.Api/Properties/launchSettings.json)

默认地址：

- `http://localhost:5227`
- `https://localhost:7161`

### 6.1 验证后端是否启动成功

浏览器访问：

```text
http://localhost:5227/api/DbTest/ping
```

如果数据库连接正常，你应该能看到类似返回：

```json
{"message":"Oracle connected successfully","count":0}
```

这里的 `count` 是 `users` 表中的记录数。  
第一次建库后没有数据，返回 `0` 是正常的。

## 7. 启动前端

打开一个新的终端，进入前端目录：

```powershell
cd frontend
```

安装依赖：

```powershell
npm install
```

启动开发服务器：

```powershell
npm run dev
```

默认访问地址：

```text
http://127.0.0.1:5173
```

## 8. 前后端联调说明

前端已经配置好了开发代理：

[frontend/vite.config.ts](frontend/vite.config.ts)

当前规则是：

- 前端请求 `/api/...`
- Vite 自动转发到 `http://localhost:5227`

这意味着前端代码里不需要手写完整后端地址，只需要请求：

```ts
fetch('/api/DbTest/ping')
```

当前联调入口在：

- [frontend/src/App.vue](frontend/src/App.vue)
- [frontend/src/api/http.ts](frontend/src/api/http.ts)

页面加载后会自动请求后端，并展示三种状态：

- 加载中
- 请求成功：显示 `message` 和 `count`
- 请求失败：显示错误信息并可点击 `Retry`

## 9. 一次完整运行的推荐顺序

每次本地开发，推荐按这个顺序启动：

1. 启动 Oracle 数据库
2. 确认 `appsettings.json` 中的连接串正确
3. 在 `backend/TaskSystem.Api` 下运行 `dotnet run`
4. 在 `frontend` 下运行 `npm install`（首次）和 `npm run dev`
5. 打开 `http://127.0.0.1:5173`
6. 页面显示 Oracle 连接成功信息，即说明前后端联调打通

## 10. 常用命令

### 后端

```powershell
cd backend/TaskSystem.Api
dotnet restore
dotnet run
```

### 前端

```powershell
cd frontend
npm install
npm run dev
npm run build
```

## 11. 常见问题排查

### 11.1 页面提示请求失败

优先按下面顺序排查：

1. 后端是否正在运行
2. `http://localhost:5227/api/DbTest/ping` 能否直接访问
3. Oracle 服务是否启动
4. `appsettings.json` 中的连接串是否正确
5. `users` 表是否已经创建

### 11.2 报错找不到 `users` 表

说明数据库脚本还没执行，或者当前连接的不是正确的用户。  
重新连接到目标 Oracle 用户后执行：

[backend/campus_db.sql](backend/campus_db.sql)

### 11.3 前端能启动，但访问接口失败

通常是下面两种原因：

- 后端没有启动在 `5227` 端口
- 你修改了后端端口，但没有同步修改 [frontend/vite.config.ts](frontend/vite.config.ts) 里的代理目标

### 11.4 `npm install` 或 `vite` 启动异常

可以尝试：

```powershell
cd frontend
npm install
npm run dev
```

如果依赖目录已经损坏，可删除 `frontend/node_modules` 后重新执行 `npm install`。

### 11.5 `dotnet run` 启动失败

优先检查：

- 是否安装了 `.NET 9 SDK`
- 当前终端里 `dotnet --version` 是否可用
- Oracle 连接串是否写错
- Oracle 用户是否有访问目标表的权限

前端：

```powershell
cd frontend
npm run build
```

后端：

```powershell
cd backend/TaskSystem.Api
dotnet run
```
