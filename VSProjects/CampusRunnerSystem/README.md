# CampusRunnerSystem

中文名称：校园中转分发与跑腿服务管理系统。

## 技术栈

- ASP.NET Core MVC（.NET 8）
- Oracle 19c
- Oracle.ManagedDataAccess.Core
- Bootstrap

## 数据库连接

项目通过 `CampusRunnerSystem/appsettings.json` 中的 `ConnectionStrings:OracleConnection` 读取 Oracle 连接串：

```json
"OracleConnection": "User Id=campus;Password=Campus123456;Data Source=localhost:1521/orclpdb;"
```

不要依赖 Visual Studio 服务器资源管理器中的数据库连接。

## 确认 Oracle 表已创建

1. 使用 campus 用户连接 `orclpdb`。
2. 执行：

```sql
SELECT table_name FROM user_tables ORDER BY table_name;
```

3. 确认存在 24 张表，包括 `users`、`nodes`、`tasks`、`payments`、`reports` 等。
4. SQL 文件位置：`CampusRunnerSystem/Database/campus_runner_oracle_schema_cn.sql`。

## 启动项目

1. 用 Visual Studio 2022 打开 `CampusRunnerSystem.sln`。
2. 确认 Oracle 19c 和 PDB `orclpdb` 正常运行。
3. 确认 `appsettings.json` 连接串正确。
4. 运行项目，默认进入 `/Account/Login`。

## 默认测试账号

- 用户名：`admin`
- 密码：`123456`
- 角色：`管理员`

注意：当前登录阶段暂时使用 `users.password_hash` 与输入密码做普通字符串比较，后续需要改为哈希验证。

## 测试节点管理页面

1. 使用 `admin / 123456` 登录。
2. 登录成功后进入管理员首页 `/AdminDashboard/Index`。
3. 点击“节点管理”，或直接访问 `/Node/Index`。
4. 页面会真实查询 Oracle 的 `nodes` 表并展示 `node_id`、`node_type`、`node_name`、`location`、`open_time`、`node_status`。

## 常见错误

- Oracle 连接失败：检查 Oracle 服务、PDB、端口和连接串。
- ORA-01017：检查 campus 用户名或密码。
- ORA-12514：检查 `orclpdb` 服务名是否正确。
- 表不存在：确认已执行 `Database/campus_runner_oracle_schema_cn.sql`，且当前连接用户是 campus。
- Session 失效：重新登录。
- 中文枚举写错：必须使用 SQL 文件中的中文值，如 `管理员`、`普通用户`、`正常`、`已创建`、`是`、`否`。
