## 本地通过 SSH 隧道连接云服务器 Oracle

本项目的 Oracle 数据库部署在云服务器上。开发人员在本地编写后端代码时，如果需要连接公共 Oracle 数据库，不直接开放数据库端口，而是通过 SSH 隧道连接。

这种方式的作用是：开发人员本地程序连接自己电脑上的 `127.0.0.1:15210`，实际请求会通过 SSH 隧道转发到云服务器内部的 Oracle 数据库。

连接关系如下：

```text
开发人员电脑
127.0.0.1:15210
  ↓
SSH 隧道
  ↓
云服务器内部 Oracle
127.0.0.1:1521
```

开发人员只需要完成以下步骤。

------

### 1. 生成 SSH 密钥

在自己电脑上打开 PowerShell，执行：

```powershell
mkdir $env:USERPROFILE\.ssh -Force

ssh-keygen -t ed25519 `
  -f "$env:USERPROFILE\.ssh\campus_dbtunnel_ed25519" `
  -C "学号-姓名-oracle-tunnel"
```

其中，`学号-姓名-oracle-tunnel` 用于标识这把公钥属于哪位同学。**请替换为自己的真实学号和姓名**，例如 `2251234-张三-oracle-tunnel`。该备注不会影响连接功能，只用于服务器负责人识别和管理公钥。

执行后会提示：

```text
Enter passphrase
```

这里可以直接按回车，不设置密码；也可以自己设置密码。如果设置了 passphrase，每次使用该私钥建立 SSH 隧道时都需要输入该密码。

生成完成后，会得到两个文件：

```text
C:\Users\你的用户名\.ssh\campus_dbtunnel_ed25519
C:\Users\你的用户名\.ssh\campus_dbtunnel_ed25519.pub
```

两个文件含义不同：

| 文件 | 作用 | 是否可以发给别人 |
| --- | --- | --- |
| `campus_dbtunnel_ed25519` | 私钥，用于本机登录 | **不可以** |
| `campus_dbtunnel_ed25519.pub` | 公钥，用于配置服务器登录权限 | 可以 |

**不要把没有 `.pub` 后缀的私钥发给任何人。**

------

### 2. 查看并发送公钥

继续在 PowerShell 执行：

```powershell
Get-Content "$env:USERPROFILE\.ssh\campus_dbtunnel_ed25519.pub"
```

会输出一整行内容，格式类似：

```text
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIxxxxxxxxxxxxxxxx 2251234-张三-oracle-tunnel
```

把这一整行公钥发给服务器负责人。只发送 `.pub` 文件输出的这一行，不发送私钥文件内容。

------

### 3. 等待服务器负责人配置公钥

公钥发给服务器负责人后，需要等待服务器负责人将公钥加入云服务器的隧道账号。

配置完成后，开发人员才能通过 SSH 隧道连接云服务器 Oracle。

------

### 4. 打开 SSH 隧道

公钥配置完成后，在自己电脑 PowerShell 执行：

```powershell
ssh -i "$env:USERPROFILE\.ssh\campus_dbtunnel_ed25519" `
  -N `
  -L 15210:127.0.0.1:1521 `
  dbtunnel@47.116.60.57
```

参数含义如下：

| 参数 | 含义 |
| --- | --- |
| `-i` | 指定本地私钥文件 |
| `-N` | 只建立隧道，不打开服务器命令行 |
| `-L 15210:127.0.0.1:1521` | 将本地 `15210` 端口转发到服务器内部 Oracle `1521` 端口 |
| `dbtunnel@47.116.60.57` | 使用专门的隧道账号连接云服务器 |

第一次连接时，可能会提示：

```text
Are you sure you want to continue connecting?
```

输入：

```text
yes
```

如果命令执行后窗口停在那里，没有继续输出，也没有返回 PowerShell 提示符，这是正常现象。这个窗口正在保持 SSH 隧道连接。

**不要关闭这个 PowerShell 窗口。关闭窗口后，隧道会断开。**

------

### 5. 测试隧道是否成功

**保持刚才的 SSH 隧道窗口不关闭。**
**重新打开一个新的 PowerShell 窗口**，执行：

```powershell
Test-NetConnection 127.0.0.1 -Port 15210
```

如果看到：

```text
TcpTestSucceeded : True
```

说明隧道已经建立成功。

------

### 6. 数据库工具连接参数

隧道成功后，可以使用 DBeaver、DataGrip、Navicat、Oracle SQL Developer 等工具连接数据库。

连接参数如下：

| 项目 | 填写内容 |
| --- | --- |
| 数据库类型 | Oracle |
| Host / 主机 | `127.0.0.1` |
| Port / 端口 | `15210` |
| Service Name / 服务名 | `orclpdb1` |
| 用户名 | 由服务器负责人单独提供 |
| 密码 | 由服务器负责人单独提供 |

注意这里使用的是：

```text
Service Name: orclpdb1
```

不要填成：

```text
XEPDB1
ORCLPDB
SID
```

如果数据库工具里有连接类型选择，应选择 `Service Name`，不要选择 `SID`。

------

### 7. 本地项目连接串

如果开发人员在本地运行 ASP.NET Core 后端项目，可以使用下面的连接串格式：

```text
User Id=<数据库用户名>;Password=<数据库密码>;Data Source=localhost:15210/orclpdb1;
```

普通查询和查看基础数据时，一般使用只读账号 `APPREAD`。需要调试新增、修改、删除等写入功能时，应单独向服务器负责人申请具备写权限的数据库账号。

在 PowerShell 中临时设置连接串并运行项目，可以这样写：

```powershell
$env:ConnectionStrings__OracleDb="User Id=<数据库用户名>;Password=<数据库密码>;Data Source=localhost:15210/orclpdb1;"

dotnet run --project backend/src/CampusDelivery.Api/CampusDelivery.Api.csproj
```

------

### 8. 每次开发时的使用流程

以后每次需要连接云数据库时，按下面流程操作：

```text
1. 打开 PowerShell
2. 执行 ssh -N -L 隧道命令
3. 保持该窗口不关闭
4. 打开数据库工具或启动本地后端项目
5. 使用 localhost:15210/orclpdb1 连接数据库
6. 调试结束后关闭 SSH 隧道窗口
```

不需要每次都重新生成密钥。密钥只需要生成一次。

------

## 9. 常见连接目的与使用边界

开发人员通过 SSH 隧道连接云服务器 Oracle，主要是为了本地开发和调试，不是为了直接管理服务器或随意修改公共数据库。

### 9.1 常见连接目的

常见用途包括：

- 查看表结构和字段含义
- 本地运行后端项目
- 调试 Repository 层 SQL
- 验证页面显示数据是否来自数据库
- 排查本地和服务器联调差异

### 9.2 数据库账号和权限边界

通过 SSH 隧道连接云数据库后，开发人员获得的是数据库访问能力，不是服务器管理权限。实际能查询或修改哪些数据，取决于使用的 Oracle 数据库账号。

当前数据库账号按用途区分：

| 账号 | 用途 | 权限边界 |
| --- | --- | --- |
| `APPUSER` | 服务器后端运行账号，也是当前业务表拥有者 | 不默认提供给普通开发人员 |
| `APPREAD` | 只读账号，用于查看表结构、字段和基础数据 | 只能查询，不能新增、修改或删除 |
| 写权限账号 | 用于调试新增、修改、删除等功能 | 由服务器负责人按模块和用途单独提供 |

普通开发人员如只需要查看表结构、查询基础数据或调试 `SELECT` 语句，应使用 `APPREAD`。需要调试写入逻辑的同学，应说明模块和用途后再申请写权限账号。

### 9.3 可以做的事情

在只读账号下可以做：

- 查看表结构
- 查询少量数据
- 调试 `SELECT` 语句
- 验证页面显示数据是否来自数据库

在获得写权限账号后，才可以做：

- 插入少量调试数据
- 修改自己创建的调试数据
- 调试新增、编辑、状态更新等写入流程

### 9.4 不应该做的事情

公共数据库是全组共享环境。任何写操作都有可能影响其他组员开发和服务器联调，因此以下操作不应由普通开发人员随意执行：

- 不要删除公共基础数据
- 不要清空表
- 不要修改表结构
- 不要随意修改英文枚举值
- 不要修改其他组员正在调试的数据
- 不要把数据库连接信息提交到 GitHub
- 不要把数据库工具当作业务后台使用

### 9.5 推荐使用习惯

- 查询前先确认表名和条件
- 写操作前先 `SELECT` 确认范围
- 避免无 `WHERE` 的 `UPDATE` 和 `DELETE`
- 临时数据命名尽量清楚
- 遇到表结构问题不要直接改库

### 9.6 简单总结

开发人员通过 SSH 隧道连接云服务器 Oracle，主要用于：

```text
查看表结构
查询样例数据
本地运行后端项目
调试 Repository SQL
排查联调问题
```

不用于：

```text
管理服务器
修改服务器配置
随意清空公共数据
直接改表结构
绕过系统页面处理业务数据
提交真实数据库密码
```

------

## 10. 常见问题

### 10.1 SSH 命令执行后卡住不动

这是正常现象。不要关闭该窗口。关闭后，数据库连接会断开。

### 10.2 提示 Permission denied

错误示例：

```text
Permission denied (publickey,gssapi-keyex,gssapi-with-mic)
```

本地可以检查公钥内容：

```powershell
Get-Content "$env:USERPROFILE\.ssh\campus_dbtunnel_ed25519.pub"
```

公钥应该是一整行，格式类似：

```text
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIxxxxxxxxxxxxxxxx 2251234-张三-oracle-tunnel
```

不要把它复制成：

```text
ssh-ed25519 ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIxxxxxxxxxxxxxxxx 2251234-张三-oracle-tunnel 2251234-张三-oracle-tunnel
```

### 10.3 Test-NetConnection 失败

如果执行：

```powershell
Test-NetConnection 127.0.0.1 -Port 15210
```

结果是：

```text
TcpTestSucceeded : False
```

优先检查：

1. SSH 隧道窗口是否还开着。
2. SSH 命令是否执行成功。
3. 本地 `15210` 端口是否被其他程序占用。
4. 是否使用了正确的私钥文件。
5. 是否已经将公钥发给服务器负责人并完成配置。

### 10.4 数据库工具连不上

如果 `Test-NetConnection` 显示 `True`，但数据库工具仍然连不上，通常不是隧道问题，而是数据库连接参数填写错误。

重点检查：

| 项目 | 正确值 |
| --- | --- |
| Host | `127.0.0.1` |
| Port | `15210` |
| 连接类型 | `Service Name` |
| Service Name | `orclpdb1` |
| User | 由服务器负责人提供，普通查询一般使用 `APPREAD` |
| Password | 由服务器负责人提供 |

### 10.5 私钥和公钥不要混淆

生成密钥后会有两个文件：

```text
campus_dbtunnel_ed25519
campus_dbtunnel_ed25519.pub
```

其中：

```text
campus_dbtunnel_ed25519      是私钥，不能发给别人
campus_dbtunnel_ed25519.pub  是公钥，可以发给服务器负责人
```
