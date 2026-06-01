# CampusRunnerSystem 项目说明

CampusRunnerSystem，中文名称为“校园中转分发与跑腿服务管理系统”，是一个基于 ASP.NET Core MVC 的课程设计项目。当前项目已经完成基础框架、登录权限、管理员首页、节点管理、服务类型管理、服务节点规则绑定等基础资料模块。

## 一、项目使用的技术

- 开发框架：ASP.NET Core MVC
- .NET 版本：.NET 8
- 开发工具：Visual Studio 2022
- 数据库：Oracle 19c
- Oracle 驱动：Oracle.ManagedDataAccess.Core
- 前端样式：Bootstrap
- 页面模板：Razor View（`.cshtml`）
- 会话管理：ASP.NET Core Session
- 项目结构方式：Controller -> Service -> Repository -> OracleDbHelper 分层

## 二、项目目录结构

项目根目录为：

```text
CampusRunnerSystem/
```

根目录下主要内容如下：

```text
CampusRunnerSystem.sln                 Visual Studio 解决方案文件
README.md                              项目说明文件
CampusRunnerSystem/                    ASP.NET Core MVC 项目主体
```

## 三、内层项目目录说明

内层项目目录为：

```text
CampusRunnerSystem/CampusRunnerSystem/
```

下面说明每个主要文件夹和文件的作用。

### 1. Controllers

位置：

```text
CampusRunnerSystem/Controllers/
```

该文件夹存放 MVC 控制器，负责接收浏览器请求、调用 Service 层，并返回页面。

主要文件：

- `AccountController.cs`：登录、注册占位、退出登录、无权限页面。
- `AdminDashboardController.cs`：管理员首页。
- `UserDashboardController.cs`：普通用户首页。
- `RunnerDashboardController.cs`：跑腿员首页。
- `NodeController.cs`：节点管理，包含列表、新增、修改、删除、详情。
- `ServiceTypeController.cs`：服务类型管理，包含列表、新增、修改、删除、详情。
- `ServiceNodeRuleController.cs`：服务类型与节点绑定规则管理。
- `HomeController.cs`：ASP.NET Core MVC 默认示例控制器，当前不是主要入口。
- `AccountModuleController.cs`、`TaskModuleController.cs`、`PaymentModuleController.cs` 等：为其他组员预留的模块占位控制器。

注意：Controller 不直接写 SQL，数据库操作统一交给 Repository。

### 2. Services

位置：

```text
CampusRunnerSystem/Services/
```

该文件夹存放业务逻辑层。Service 层负责业务判断、状态校验、删除前检查等。

主要文件：

- `IAccountService.cs`、`AccountService.cs`：登录业务逻辑。
- `INodeService.cs`、`NodeService.cs`：节点管理业务逻辑。
- `IServiceTypeService.cs`、`ServiceTypeService.cs`：服务类型业务逻辑。
- `IServiceNodeRuleService.cs`、`ServiceNodeRuleService.cs`：服务节点绑定规则业务逻辑。

例如删除节点时，Service 会先检查该节点是否被 `tasks` 或 `service_node_rules` 引用，如果被引用就返回中文友好提示。

### 3. Repositories

位置：

```text
CampusRunnerSystem/Repositories/
```

该文件夹存放数据访问层，负责写 SQL 并调用 `OracleDbHelper` 执行。

主要文件：

- `IAccountRepository.cs`、`AccountRepository.cs`：查询 `users` 表中的登录用户信息。
- `INodeRepository.cs`、`NodeRepository.cs`：操作 `nodes` 表。
- `IServiceTypeRepository.cs`、`ServiceTypeRepository.cs`：操作 `service_types` 表。
- `IServiceNodeRuleRepository.cs`、`ServiceNodeRuleRepository.cs`：操作 `service_node_rules` 表，并联查服务类型和节点名称。

要求：

- SQL 必须使用参数化查询。
- 不在 Controller 中拼接 SQL。
- 表名和字段名必须以数据库 SQL 文件为准。

### 4. Helpers

位置：

```text
CampusRunnerSystem/Helpers/
```

主要文件：

- `OracleDbHelper.cs`

作用：

- 从 `appsettings.json` 读取 Oracle 连接字符串。
- 使用 `Oracle.ManagedDataAccess.Client` 连接 Oracle。
- 提供通用方法：`QueryDataTable`、`ExecuteNonQuery`、`ExecuteScalar`。

这是整个项目连接 Oracle 数据库的公共入口。

### 5. Models

位置：

```text
CampusRunnerSystem/Models/
```

该文件夹存放公共模型和常量。

主要文件：

- `Result.cs`：统一返回结果类，包含 `Success`、`Message`、`Data`。
- `SystemConstants.cs`：系统常量，包括角色、账号状态、任务状态、是否标志、节点状态、服务类型状态。
- `ErrorViewModel.cs`：默认错误页面模型。

项目中角色和状态必须使用中文值，例如：

- `管理员`
- `普通用户`
- `跑腿员`
- `正常`
- `关闭`
- `启用`
- `禁用`

不要使用 `ADMIN`、`NORMAL`、`CLOSED` 等英文枚举值。

### 6. ViewModels

位置：

```text
CampusRunnerSystem/ViewModels/
```

该文件夹存放页面展示模型，用于 Controller 和 View 之间传递数据。

主要文件：

- `LoginUserViewModel.cs`：登录用户信息。
- `NodeViewModel.cs`：节点信息。
- `ServiceTypeViewModel.cs`：服务类型信息。
- `ServiceNodeRuleViewModel.cs`：服务节点规则信息。

### 7. Filters

位置：

```text
CampusRunnerSystem/Filters/
```

主要文件：

- `RoleAuthorizeAttribute.cs`

作用：

- 判断用户是否已经登录。
- 判断当前用户角色是否允许访问某个控制器。
- 未登录时跳转到登录页。
- 角色不匹配时跳转到无权限页面。

例如管理员页面会使用：

```csharp
[RoleAuthorize(SystemConstants.Roles.Admin)]
```

### 8. Views

位置：

```text
CampusRunnerSystem/Views/
```

该文件夹存放 Razor 页面。

主要子文件夹：

- `Views/Account/`：登录、注册占位、无权限页面。
- `Views/AdminDashboard/`：管理员首页。
- `Views/UserDashboard/`：普通用户首页。
- `Views/RunnerDashboard/`：跑腿员首页。
- `Views/Node/`：节点管理页面，包含列表、新增、修改、删除、详情。
- `Views/ServiceType/`：服务类型管理页面，包含列表、新增、修改、删除、详情。
- `Views/ServiceNodeRule/`：服务类型与节点绑定规则页面。
- `Views/ModuleTodo/`：其他模块暂未实现时的占位页面。
- `Views/Shared/`：公共布局页面和公共视图。
- `Views/Home/`：ASP.NET Core MVC 默认示例页面。

重要文件：

- `Views/Shared/_Layout.cshtml`：公共布局，包含顶部栏、当前登录用户信息、退出按钮、左侧菜单。
- `Views/_ViewImports.cshtml`：Razor 公共引用。
- `Views/_ViewStart.cshtml`：默认布局配置。

### 9. Database

位置：

```text
CampusRunnerSystem/Database/
```

主要文件：

- `campus_runner_oracle_schema_cn.sql`

作用：

- 创建项目所需的 24 张 Oracle 表。
- 定义主键、外键、检查约束。
- 定义中文枚举值。

开发时必须参考这个 SQL 文件，不要自己编造表名或字段名。

当前基础资料模块涉及的表：

- `nodes`
- `service_types`
- `service_node_rules`

登录涉及的表：

- `users`

### 10. Docs

位置：

```text
CampusRunnerSystem/Docs/
```

该文件夹存放项目文档和小组协作说明，例如项目运行说明、详细分工、其他组员准备工作等。

### 11. wwwroot

位置：

```text
CampusRunnerSystem/wwwroot/
```

该文件夹存放静态资源。

主要子文件夹：

- `wwwroot/css/`：项目 CSS 文件，例如 `site.css`。
- `wwwroot/js/`：项目 JavaScript 文件，例如 `site.js`。
- `wwwroot/lib/`：前端库文件，包括 Bootstrap、jQuery、jQuery Validation。
- `wwwroot/favicon.ico`：网站图标。

### 12. Properties

位置：

```text
CampusRunnerSystem/Properties/
```

主要文件：

- `launchSettings.json`

作用：

- Visual Studio 启动配置。
- 包含本地运行地址、环境变量等。

### 13. bin 和 obj

位置：

```text
CampusRunnerSystem/bin/
CampusRunnerSystem/obj/
```

这两个文件夹是编译生成目录。

- `bin/`：编译后的程序文件。
- `obj/`：中间编译文件。

一般不需要手动修改，也不要提交手写代码到这两个目录。

## 四、重要配置文件说明

### 1. appsettings.json

位置：

```text
CampusRunnerSystem/appsettings.json
```

主要配置 Oracle 连接字符串：

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=campus;Password=Campus123456;Data Source=localhost:1521/orclpdb;"
  }
}
```

项目通过 `OracleDbHelper` 读取这个连接字符串。

### 2. Program.cs

位置：

```text
CampusRunnerSystem/Program.cs
```

主要作用：

- 注册 MVC。
- 启用 Session。
- 注册 Repository 和 Service。
- 配置静态文件。
- 配置默认路由。

当前默认路由为：

```text
/Account/Login
```

### 3. CampusRunnerSystem.csproj

位置：

```text
CampusRunnerSystem/CampusRunnerSystem.csproj
```

项目工程文件，里面声明了：

- 目标框架：`net8.0`
- NuGet 包：`Oracle.ManagedDataAccess.Core`

## 五、数据库准备

运行项目前，请确认 Oracle 数据库已经准备好：

1. Oracle 19c 已安装并运行。
2. PDB 服务名为 `orclpdb`。
3. 数据库用户为 `campus`。
4. 数据库密码为 `Campus123456`。
5. 已经在 campus 用户下执行：

```text
CampusRunnerSystem/Database/campus_runner_oracle_schema_cn.sql
```

可以用下面 SQL 检查表是否创建成功：

```sql
SELECT table_name FROM user_tables ORDER BY table_name;
```

项目要求共 24 张表。

## 六、初步运行项目

### 方式一：使用 Visual Studio 2022

1. 打开解决方案文件：

```text
CampusRunnerSystem.sln
```

2. 确认启动项目是 `CampusRunnerSystem`。
3. 确认 `appsettings.json` 中 Oracle 连接字符串正确。
4. 点击运行，或按 `F5` / `Ctrl + F5`。
5. 浏览器会进入登录页：

```text
/Account/Login
```

### 方式二：使用命令行

进入解决方案目录：

```powershell
cd D:\delivery-backend\VSProjects\CampusRunnerSystem
```

编译项目：

```powershell
dotnet build
```

运行项目：

```powershell
dotnet run --project CampusRunnerSystem
```

如果需要指定端口，可以使用：

```powershell
dotnet run --project CampusRunnerSystem --urls http://127.0.0.1:5086
```

然后在浏览器打开：

```text
http://127.0.0.1:5086/Account/Login
```

## 七、登录测试

当前基础登录逻辑从 `users` 表读取数据。

测试管理员账号要求：

```text
用户名：admin
密码：123456
角色：管理员
账号状态：正常
```

对应 `users` 表中的字段应满足：

```text
username = admin
password_hash = 123456
user_role = 管理员
account_status = 正常
```

说明：当前课程设计阶段，密码暂时使用普通字符串比较。后续可以改为密码哈希验证。

## 八、当前可以测试的功能

### 1. 登录与退出

- 访问 `/Account/Login`。
- 输入 `admin / 123456`。
- 登录成功后进入管理员首页。
- 点击右上角“退出登录”可以清空 Session 并返回登录页。

### 2. 管理员首页

地址：

```text
/AdminDashboard/Index
```

可以进入：

- 节点管理
- 服务类型管理
- 服务节点规则
- 用户管理占位页
- 跑腿员审核占位页
- 投诉处理占位页
- 统计报表占位页

### 3. 节点管理

地址：

```text
/Node/Index
```

对应 Oracle 表：

```text
nodes
```

支持：

- 节点列表
- 新增节点
- 修改节点
- 删除节点
- 查看节点详情

节点状态只能使用：

```text
正常
关闭
```

### 4. 服务类型管理

地址：

```text
/ServiceType/Index
```

对应 Oracle 表：

```text
service_types
```

支持：

- 服务类型列表
- 新增服务类型
- 修改服务类型
- 删除服务类型
- 查看服务类型详情

类型状态只能使用：

```text
启用
禁用
```

### 5. 服务节点规则

地址：

```text
/ServiceNodeRule/Index
```

对应 Oracle 表：

```text
service_node_rules
```

支持：

- 选择一个服务类型。
- 选择一个节点。
- 绑定服务类型和节点。
- 查看所有绑定规则。
- 删除绑定规则。
- 重复绑定时提示“该服务类型与节点已存在绑定关系”。

列表中会联查显示：

- 服务类型编号
- 服务名称
- 节点编号
- 节点名称
- 节点类型

## 九、当前项目的分层调用关系

以节点列表为例：

```text
浏览器请求 /Node/Index
        ↓
NodeController
        ↓
NodeService
        ↓
NodeRepository
        ↓
OracleDbHelper
        ↓
Oracle 数据库 nodes 表
```

这种结构方便小组成员继续开发其他模块，也方便总集成时排查问题。

## 十、常见问题

### 1. ORA-01017

用户名或密码错误。

优先检查：

- `appsettings.json` 中的 `User Id`
- `Password`
- Oracle 用户是否是 `campus`

### 2. ORA-12514

Oracle 服务名不正确。

优先检查：

- PDB 是否启动。
- 服务名是否是 `orclpdb`。
- 连接串是否是 `localhost:1521/orclpdb`。

### 3. 表不存在

可能原因：

- 没有执行 SQL 文件。
- SQL 文件执行到了其他用户下。
- 当前连接用户不是 `campus`。

### 4. 中文状态插入失败

可能原因：

- 使用了英文状态。
- 中文枚举值和 SQL 文件中的检查约束不一致。

例如节点状态只能是：

```text
正常
关闭
```

服务类型状态只能是：

```text
启用
禁用
```

### 5. 登录后访问页面显示无权限

可能原因：

- `users.user_role` 不是 `管理员`、`普通用户`、`跑腿员` 中的一个。
- Session 已过期。
- 当前账号角色和页面要求角色不匹配。

## 十一、后续开发建议

后续组员开发时建议继续遵守：

- Controller 不直接写 SQL。
- Service 负责业务判断。
- Repository 负责数据库访问。
- View 只负责页面显示。
- SQL 使用参数化查询。
- 表名、字段名、中文枚举值以 `Database/campus_runner_oracle_schema_cn.sql` 为准。
- 不要修改 `bin/` 和 `obj/` 中的编译输出文件。

目前预留给其他组员继续实现的模块包括：

- 用户资料和地址管理。
- 跑腿员资料提交和审核。
- 任务发布和任务详情。
- 接单、派单、重派。
- 任务状态日志。
- 支付、退款、评价、投诉。
- 跑腿员结算。
- 审计日志。
- 统计报表。
