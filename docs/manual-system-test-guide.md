# 数据库手工系统测试与证据留存指南

## 1. 使用范围

本指南用于执行 `system-test-report.md` 中标记为 `PENDING-DB` 的用例。它面向 ASP.NET Core MVC 页面和 Oracle 数据库，重点验证真实事务、行锁、权限、主业务闭环和异常输入。

不要在共享生产库或未获授权的云端库执行本指南。应使用可以重建、可以清理的隔离 Oracle 测试库。

## 2. 执行前准备

### 2.1 环境

1. 在隔离库执行 `database/oracle/campus_runner_oracle_schema.sql`。
2. 执行 `database/oracle/002_init_base_data.sql` 准备基础用户、地址、跑腿员、节点和服务类型。
3. 通过 `appsettings.Local.json` 或环境变量设置测试库连接串，不提交真实凭据。
4. 启动应用并打开 `/Database/Status`，确认连接成功。
5. 先执行 `scripts/run-tests.ps1`，保证自动化和静态门禁通过。

基础脚本只提供演示账号数据和密码哈希，不在仓库保存明文密码。测试人员应使用已知的本地测试密码，或通过注册页面创建账号。

### 2.2 账号与数据

至少准备以下独立账号：

| 代号 | 角色/状态 | 用途 |
| --- | --- | --- |
| U1 | USER / NORMAL | 发布任务、确认收货、支付、评价、投诉 |
| U2 | USER / NORMAL | 越权和对象归属测试 |
| R1 | RUNNER / APPROVED / FREE | 正常接单和配送 |
| R2 | RUNNER / APPROVED / FREE | 并发抢单和重派 |
| R3 | RUNNER / PENDING 或 REJECTED | 资格限制测试 |
| A1 | ADMIN / NORMAL | 配置、审核、派单、退款、结算、审计、报表 |

准备三类启用服务、正常节点和有效服务节点规则。每次并发用例使用新建的 `WAITING` 任务，避免复用已被其他用例改变状态的数据。

## 3. 证据规范

建议按以下目录留存证据，截图文件本次不预先伪造：

```text
docs/test-evidence/manual/YYYY-MM-DD/
  environment.md
  TC029-before.png
  TC029-runner1.png
  TC029-runner2.png
  TC029-database.txt
  main-flow.md
```

每个用例至少记录：

- 用例编号、执行人、时间和构建提交号。
- 前置数据 ID，敏感账号信息打码。
- 操作步骤和页面实际提示。
- 关键页面截图；涉及事务时附只读 SQL 查询结果。
- PASS/FAIL；FAIL 必须附实际结果、日志摘要和缺陷编号。

禁止为了形成证据直接修改业务表状态。测试数据必须通过系统操作形成；SQL 仅用于准备隔离环境或只读核验。

## 4. 主业务闭环

按以下顺序至少完整执行一次：

1. U1 注册、登录并新增两条地址，切换默认地址。
2. 普通用户申请跑腿员资格，A1 审核通过；另保留 R3 为未通过状态。
3. A1 新增/启停节点和服务类型，绑定服务节点规则。
4. U1 分别发布外卖、快递、私人跑腿任务，验证三类专属字段。
5. 取消一个 `WAITING` 任务；对另一个任务执行 R1 抢单。
6. A1 对一个任务派单，并将一个进行中的任务从 R1 重派给 R2。
7. 当前跑腿员按 `PICKED_UP -> DELIVERING -> WAIT_CONFIRM` 更新状态。
8. U1 确认收货并立即支付，核验任务 FINISHED、支付 PAID、跑腿员 FREE。
9. U1 提交评价；再对另一个完成任务提交投诉，A1 分别验证成立和驳回处理。
10. 对 PAID 任务发起退款，A1 审核；分别验证通过和驳回分支。
11. A1 生成结算单，核验争议款、退款款和已结算款不会重复进入候选。
12. A1 对支付、退款、状态日志各生成一条审计记录。
13. A1 查看报表面板并生成 ORDER、PAYMENT、COMPLAINT 三类报表记录。

关键状态链应符合：

```text
WAITING -> ASSIGNED -> PICKED_UP -> DELIVERING -> WAIT_CONFIRM -> FINISHED
WAITING -> CANCELLED
FINISHED -> REFUNDING -> FINISHED
```

支付、跑腿员和退款状态联动：

```text
Runner:  FREE -> BUSY -> FREE
Payment: UNPAID -> PAID -> REFUNDED
Refund:  APPLY -> APPROVED / REJECTED
```

## 5. 并发抢单专项测试

### 5.1 页面准备

1. 使用两个独立浏览器配置文件，分别以 R1、R2 登录，避免共享 Cookie。
2. 确认两名跑腿员均为 `APPROVED/FREE`。
3. U1 发布一个新任务，记录 `task_id`，确认状态为 `WAITING`。
4. R1、R2 同时打开任务大厅并定位同一个任务。
5. 同时点击抢单。若需要更精确，可在两台终端/两名测试人员倒计时提交。

### 5.2 预期页面结果

- 一个会话提示接单成功，另一个提示任务已被接走或当前不可接单。
- 成功跑腿员进入 `BUSY`；失败跑腿员保持 `FREE`。
- 刷新任务大厅后，该任务不再可抢。

### 5.3 数据库核验 SQL

将 `:task_id` 替换为本次测试任务编号，仅执行查询：

```sql
SELECT task_id, task_status
FROM APPUSER.tasks
WHERE task_id = :task_id;

SELECT record_id, task_id, runner_id, operation_type, assigned_at
FROM APPUSER.assign_records
WHERE task_id = :task_id
ORDER BY record_id;

SELECT COUNT(*) AS assign_count,
       COUNT(DISTINCT runner_id) AS runner_count
FROM APPUSER.assign_records
WHERE task_id = :task_id;

SELECT l.status_before, l.status_after, l.operator_user_id, l.operated_at
FROM APPUSER.task_status_logs l
JOIN APPUSER.assign_records ar ON ar.record_id = l.record_id
WHERE ar.task_id = :task_id
ORDER BY l.log_id;

SELECT runner_id, audit_status, work_status
FROM APPUSER.runners
WHERE runner_id IN (:runner_id_1, :runner_id_2)
ORDER BY runner_id;
```

通过标准：

- `tasks.task_status = 'ASSIGNED'`。
- `assign_count = 1` 且 `runner_count = 1`。
- 只有一条 `WAITING -> ASSIGNED` 日志。
- 只有成功者为 `BUSY`，失败者仍为 `FREE`。

代码侧证据位于 `AssignService.GrabTaskAsync` 和 `AssignRepository.GetTaskStatusWithLockAsync`：事务内通过 `SELECT ... FOR UPDATE` 锁任务，再重新判断状态。数据库级结论必须以上述真实双会话结果为准。

## 6. 状态机负向测试

| 对象 | 当前状态 | 非法操作 | 预期 |
| --- | --- | --- | --- |
| 任务 | WAITING | 直接更新为 PICKED_UP/DELIVERING | 拒绝，无状态日志 |
| 任务 | ASSIGNED | 直接更新为 DELIVERING | 拒绝，仍为 ASSIGNED |
| 任务 | DELIVERING | 再次抢单或派单 | 拒绝，不新增接派记录 |
| 任务 | WAIT_CONFIRM | 非发布者确认收货 | 拒绝，无确认日志 |
| 支付 | 无确认日志 | 提交支付 | 回滚，不新增支付 |
| 支付 | PAID | 再次支付 | 拒绝，仍为 PAID |
| 支付 | REFUNDED | 再次支付 | 拒绝，仍为 REFUNDED |
| 退款 | APPLY | 重复申请 | 拒绝第二条有效申请 |
| 退款 | APPROVED/REJECTED | 重复审核 | 拒绝，结果不变 |
| 评价 | 非 FINISHED | 提交评价 | 拒绝 |
| 评价 | UNPAID + FINISHED | 提交评价 | 当前已知缺陷，应记录 FAIL，不得改写为 PASS |
| 投诉 | 非 FINISHED | 提交投诉 | 拒绝 |
| 投诉 | DONE | 再次处理 | 拒绝，不重复扣信誉分 |
| 结算 | DONE | 回退 WAITING | 当前已知缺陷，应记录 FAIL |

## 7. 异常输入与越权测试

对每个写操作至少选择一项执行：

1. 必填字段为空或仅含空格。
2. 枚举传入未定义值，例如非法支付方式、审核决定、目标状态。
3. 文本超过 ViewModel 或数据库允许长度，包含中文多字节文本。
4. ID 为 `0`、负数、极大但不存在的正数。
5. 用户 U2 提交 U1 的地址、任务、支付、退款、评价或投诉 ID。
6. USER 访问 ADMIN Action，RUNNER 更新并非自己承接的任务。
7. 双击提交按钮或重复发送同一 POST。
8. 删除或篡改 `__RequestVerificationToken` 后提交 POST，应返回 400。
9. 分页 `page=-1&pageSize=100000`，系统应归一化，不读取无界数据。
10. 输入单引号、百分号和常见 SQL 片段，系统应按参数值处理，不改变查询结构。

页面应显示可理解的中文提示。错误页不得显示 SQL、连接字符串、堆栈或真实凭据。

## 8. Postman 集合执行说明

本项目是 MVC 表单应用，不是无状态 Bearer Token API。Postman 测试需要同时维护 Cookie 和 Anti-forgery Token：

1. 建立环境变量 `baseUrl`、`username`、`password`、`requestVerificationToken`、`taskId`。
2. 先 GET `/Auth/Login`，由测试脚本从响应 HTML 提取隐藏字段 `__RequestVerificationToken`；Postman Cookie Jar 保存防伪 Cookie。
3. POST `/Auth/Login` 时使用 `application/x-www-form-urlencoded`，同时提交账号、密码和隐藏令牌。
4. 每次访问写表单前先 GET 对应页面，刷新隐藏令牌，再 POST 表单字段。
5. R1、R2 并发测试使用两个环境或两个独立 Cookie Jar，不能共用登录会话。
6. 在 Collection Runner 中关闭自动重试，避免把客户端重试误判成后端重复提交。
7. Tests 脚本至少断言状态码、重定向地址、成功/失败提示，以及后续 GET 页面状态。

由于防伪令牌和测试账号均是环境动态数据，本仓库不提交带固定 Cookie、固定令牌或真实凭据的集合文件。答辩需要 Postman 演示时，可按本节创建本地集合并只导出不含环境密钥的集合定义。

## 9. 结果回填模板

```markdown
### TC029 两名跑腿员并发抢单

- 执行人：
- 执行时间：
- Git 提交：
- 环境：Oracle 19c 隔离测试库 / 浏览器版本
- task_id：
- runner_id_1 / runner_id_2：
- 页面结果：
- SQL 核验摘要：assign_count=__，runner_count=__
- 证据文件：
- 结论：PASS / FAIL
- 缺陷编号（如失败）：
```

完成数据库执行后，同步更新 `system-test-report.md` 中对应行的“实际结果”和“状态”，不要只添加截图而不更新报告结论。
