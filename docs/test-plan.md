# 第五阶段测试用例与执行规范

## 一、分工与流程

```text
组员10：编写测试步骤、预期结果和记录模板 -> 分发给组员1-9

各个模块负责人：按照用例测试自己的模块，填写实际结果，并提交截图或 SQL 证据

组员10：抽查重要结果，并亲自执行完整业务流程和跨模块测试（见 TC-E2E）

发现 Bug：组员10 登记到 docs/bug-list.md，分配给对应模块负责人

各个模块负责人：修复 Bug 并通知组员10
组员10：重新测试（回归）；通过后将 Bug 标记为「已关闭」
```

## 二、测试环境与账号

| 项目 | 内容 |
| --- | --- |
| 本地地址 | `http://localhost:5227/` |
| 公网演示地址 | `https://47.116.60.57/` |
| 数据库 | 共享 Oracle 19c（SSH 隧道 `127.0.0.1:15210/orclpdb1`，Service Name `orclpdb1`） |
| 测试账号 | 自行准备 |
| 浏览器 | Edge / Chrome 最新版 |

## 三、证据提交规范（组员1-9 必读）

1. **实际结果**：每条用例如实填写「实际结果」「结果（通过/失败/阻塞）」两列；不得把未执行的用例标为通过。
2. **页面截图**：完整浏览器窗口（含地址栏 URL），能看清操作结果或报错信息。
3. **SQL 证据**：用例表中标注「需 SQL」的，附执行 SQL 文本 + 查询结果截图；SQL 语句见各模块用例表下方。
4. **命名规则**：`用例编号-简述.png` / `用例编号-查询.sql`，例如 `TC-ACC-03-封禁后登录被拒.png`。
5. **提交位置**：放入 `docs/test-evidence/manual/<负责人>-<模块>/` 目录。

## 四、记录模板（组员1-9 复制填写）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据文件 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （照抄本文件） | | | | | （填写） | 通过/失败/阻塞 | （截图/SQL 文件名） |

发现问题时，除填「失败」外，另附一句问题描述用于登记 Bug。

---

## 五、分模块测试用例（包括但不限于以下内容）

### 组员1：总集成与架构（TC-INT）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-INT-01 | 最终版本部署可用 | 已完成部署 | 打开公网地址 `https://47.116.60.57/` | 首页正常打开，HTTPS 无证书错误，样式完整 | | | 截图 |
| TC-INT-02 | 首页导航按角色展示入口 | 准备访客/用户/跑腿员/管理员账号 | 分别以四种身份访问首页和导航 | 访客只见登录注册；用户见任务、地址等；跑腿员见任务大厅；管理员见管理入口 | | | 截图×4 |
| TC-INT-03 | 全站页面无编译/配置/样式冲突 | 已合并全部模块 | 按 README 模块表逐个打开全部页面 | 每个页面正常渲染，无 500、无样式错乱、无死链 | | | 截图 |
| TC-INT-04 | 数据库连接检测页 | 应用已启动 | 打开 `/Database/Status` | 显示 Oracle 连接正常 | | | 截图 |
| TC-INT-05 | 自动化门禁通过 | 本机装好 .NET SDK 9 | 执行 `scripts\run-tests.ps1` | 52 项自动化测试全部通过（2026-09-01 复核），静态检查（24 表、47/47 POST 防伪令牌、14 处行锁、五层边界）通过 | | | 控制台截图 |

### 组员2：数据库与基础数据（TC-DB）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-DB-01 | 24 张业务表齐全 | 已连接共享库 | 执行 SQL① 统计当前用户业务表 | 返回 24 张表，与 `docs/database_dictionary.md` 一致 | | | SQL+结果 |
| TC-DB-02 | 迁移脚本全部生效 | 003-006 已执行 | 检查账号生命周期、评价完整性、密码哈希、业务完整性加固相关列与约束存在 | 各迁移对象存在且无失效 | | | SQL+结果 |
| TC-DB-03 | 密码不明文存储 | 库中有注册账号 | 执行 SQL② 查看 `users` 密码列 | 全部为 Identity 哈希值，无明文 | | | SQL+结果 |
| TC-DB-04 | 基础数据完整 | 初始化数据已导入 | 查询 `nodes`、`service_types`、`service_node_rules` | 有可用节点、启用服务类型及对应规则 | | | SQL+结果 |
| TC-DB-05 | 默认地址唯一约束 | 已有用户地址数据 | 执行 SQL③ 检查每用户默认地址数量 | 同一用户最多 1 条默认地址，无违反 | | | SQL+结果 |
| TC-DB-06 | 测试账号可用 | 已建测试账号 | 用各角色账号登录系统 | 普通用户/跑腿员/管理员均可正常登录 | | | 截图 |

参考 SQL：
```sql
-- ① 业务表清单
SELECT table_name FROM user_tables ORDER BY table_name;
-- ② 密码哈希检查
SELECT user_id, account, password_hash, account_status FROM users FETCH FIRST 10 ROWS ONLY;
-- ③ 默认地址唯一性
SELECT user_id, COUNT(*) FROM user_addresses WHERE is_default = 1 GROUP BY user_id HAVING COUNT(*) > 1;
```

### 组员3：账户、权限与地址（TC-ACC）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-ACC-01 | 正常注册 | 无 | `/Auth/Register` 填写合法账号、手机号、密码提交 | 注册成功并跳转登录；`users` 新增记录，状态 NORMAL，密码为哈希 | | | 截图+SQL |
| TC-ACC-02 | 重复账号/手机号注册 | 已有账号 | 用已存在账号或手机号再次注册 | 拒绝并提示，不插入重复记录 | | | 截图 |
| TC-ACC-03 | 封禁账号禁止登录 | 管理员将某账号置为 BLOCKED | 该账号尝试登录；再用其旧登录态访问业务页 | 登录被拒；已登录会话访问业务接口也被拦截 | | | 截图+SQL |
| TC-ACC-04 | 注销账号处理 | 管理员注销某账号（CANCELLED） | 该账号登录；管理员再恢复 | 注销后不能登录；恢复后 NORMAL 可登录 | | | 截图+SQL |
| TC-ACC-05 | 角色路由授权 | 三种角色账号 | 普通用户访问 `/Account`、`/Task/AdminConsole` 等管理员页；跑腿员访问 `/Settlement/My` | 越权访问被拒绝或跳转，不泄露数据 | | | 截图 |
| TC-ACC-06 | 地址增删改与默认地址 | 已登录用户 | `/Address` 新增 2 条地址 → 设第 2 条为默认 → 删除默认地址 | 新增成功；默认切换后仅 1 条默认；删除默认后自动补位，事务内完成 | | | 截图+SQL |
| TC-ACC-07 | 地址不可跨用户使用 | 用户 A、B 各有地址 | 发布任务时用户 A 尝试使用 B 的地址 ID | 校验拒绝，提示地址无效 | | | 截图 |

参考 SQL：
```sql
SELECT user_id, account, role, account_status FROM users WHERE account = '测试账号';
SELECT address_id, user_id, address_no, is_default FROM user_addresses WHERE user_id = :uid ORDER BY address_no;
```

### 组员4：基础资料与配送员（TC-BASE）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-BASE-01 | 节点新增/修改/关闭/恢复 | 管理员登录 | `/Node` 新增节点 → 修改 → 关闭 → 恢复 | 各操作生效；关闭后节点状态 CLOSED，发布任务时不可选 | | | 截图+SQL |
| TC-BASE-02 | 使用中节点删除保护 | 节点被历史任务引用 | 尝试删除被引用的节点 | 拒绝删除，提示先关闭；历史数据不受影响 | | | 截图 |
| TC-BASE-03 | 服务类型维护 | 管理员登录 | `/ServiceType` 新增服务类型（含价格规则）→ 停用 | 新增成功；停用后 DISABLED，发布任务时不可选 | | | 截图+SQL |
| TC-BASE-04 | 服务节点适用规则 | 已有服务类型和节点 | `/ServiceNodeRule` 绑定/解绑服务类型与节点 | 绑定后该组合可发布；解绑后发布时校验拒绝 | | | 截图+SQL |
| TC-BASE-05 | 跑腿员申请 | 普通用户登录 | `/Runner/Apply` 提交申请 | 申请成功，`runners` 新增记录，审核状态 PENDING | | | 截图+SQL |
| TC-BASE-06 | 管理员审核通过 | 有 PENDING 申请 | `/Runner/Pending` 审核通过 | 状态变 APPROVED，用户角色同步为 RUNNER，工作状态 FREE | | | 截图+SQL |
| TC-BASE-07 | 驳回后重新申请 | 有被驳回申请 | 管理员驳回 → 用户重新提交 → 再审通过 | 驳回后 REJECTED；允许重新申请并可通过 | | | 截图+SQL |
| TC-BASE-08 | 准备可接单账号 | 审核已通过 | 确认一名跑腿员 APPROVED 且 FREE | 该账号可进入任务大厅抢单，供 TC-ASSIGN 使用 | | | 截图+SQL |

### 组员5：任务发布（TC-TASK）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-TASK-01 | 发布外卖分发任务 | 用户已登录并有地址 | `/Task/Create` 选外卖类型、地址、节点，填明细提交 | 发布成功；`tasks` 状态 WAITING；`food_delivery_details` 有且仅有一条对应明细 | | | 截图+SQL |
| TC-TASK-02 | 发布快递代取任务 | 同上 | 选快递代取提交 | 成功；仅写 `express_pickup_details` | | | 截图+SQL |
| TC-TASK-03 | 发布私人跑腿任务 | 同上 | 选私人跑腿提交 | 成功；仅写 `private_task_details` | | | 截图+SQL |
| TC-TASK-04 | 停用类型/关闭节点不可选 | 管理员停用某类型、关闭某节点 | 打开发布页查看下拉项 | 停用类型和关闭节点不出现在可选项 | | | 截图 |
| TC-TASK-05 | 类型-节点不匹配校验 | 准备不在规则表中的组合 | 用脚本/改包提交不匹配组合 | 服务端校验拒绝，不落库 | | | 截图 |
| TC-TASK-06 | 发布不产生副作用 | 发布一条任务 | 执行 SQL 检查该任务 | 无 `assign_records`、无 `payments`、无 `task_status_logs`，且只有一种明细 | | | SQL+结果 |
| TC-TASK-07 | 我的任务列表与详情 | 已发布多条任务 | `/Task`、`/Task/MyTasks` 查看列表和详情 | 列表完整、详情字段正确，状态显示中文 | | | 截图 |
| TC-TASK-08 | 待接单任务取消 | 一条 WAITING 任务 | 在详情页取消 | 状态变 CANCELLED；不写状态日志；任务大厅不再显示 | | | 截图+SQL |

参考 SQL：
```sql
SELECT task_id, task_type, task_status, price FROM tasks WHERE task_id = :tid;
SELECT 'food' AS kind, COUNT(*) FROM food_delivery_details WHERE task_id = :tid
UNION ALL SELECT 'express', COUNT(*) FROM express_pickup_details WHERE task_id = :tid
UNION ALL SELECT 'private', COUNT(*) FROM private_task_details WHERE task_id = :tid;
SELECT COUNT(*) FROM assign_records WHERE task_id = :tid;   -- 应为 0
SELECT COUNT(*) FROM payments p JOIN assign_records a ON p.record_id = a.record_id WHERE a.task_id = :tid;  -- 应为 0
```

### 组员6：接单派单与状态流转（TC-ASSIGN）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-ASSIGN-01 | 任务大厅只显示待接单 | 有 WAITING 和 ASSIGNED 任务 | 跑腿员打开 `/Task/Hall` | 仅显示 WAITING 任务，已接单任务消失 | | | 截图 |
| TC-ASSIGN-02 | 跑腿员抢单 | APPROVED+FREE 跑腿员，一条 WAITING 任务 | 大厅点击抢单 | 任务变 ASSIGNED；新增 `operation_type=SELF` 接派记录；跑腿员变 BUSY；写入状态日志 | | | 截图+SQL |
| TC-ASSIGN-03 | 多单接单与资格限制 | 一名 APPROVED+BUSY 跑腿员、一名未审核或审核未通过的申请用户，两条 WAITING 任务 | BUSY 跑腿员继续抢一单；未取得跑腿员资格的用户尝试进入任务大厅 | BUSY 跑腿员可继续接单且进行中任务数增加；未取得资格的用户被拒绝访问，任务状态不变 | | | 截图+SQL |
| TC-ASSIGN-04 | 并发抢单只成功一人 | 两名 APPROVED 且非 OFFLINE 的跑腿员，一条 WAITING 任务 | 两人使用独立会话同时提交抢单 | 仅一人成功；任务只有一条有效接派记录，无脏数据 | | | 截图+SQL |
| TC-ASSIGN-05 | 管理员派单 | 无人接单的 WAITING 任务 | `/Task/AdminConsole` 派给指定的 APPROVED 且非 OFFLINE 跑腿员 | 任务变 ASSIGNED；记录 `operation_type=ADMIN`；跑腿员变为或保持 BUSY | | | 截图+SQL |
| TC-ASSIGN-06 | 管理员重派与工作状态联动 | 准备两条由同一旧跑腿员承接的活动任务和另一名 APPROVED 且非 OFFLINE 跑腿员 | 依次把两条任务重派给新跑腿员 | 旧记录保留并新增 `REASSIGN` 记录；旧跑腿员尚有其他活动任务时保持 BUSY，最后一条活动任务转出后变为 FREE；新跑腿员变为或保持 BUSY；日志完整 | | | 截图+SQL |
| TC-ASSIGN-07 | 禁止接自己的任务 | 用户自己发布的 WAITING 任务（该用户同时为跑腿员） | 尝试接/派给自己 | 系统拒绝 | | | 截图 |
| TC-ASSIGN-08 | 配送状态顺序流转 | 已接单任务 | 依次操作取货 → 配送中 → 送达 | 状态依次 PICKED_UP → DELIVERING → WAIT_CONFIRM；每次变更写 `task_status_logs` | | | 截图+SQL |
| TC-ASSIGN-09 | 非法状态跳转被拒 | 已接单任务 | 尝试跳过取货直接送达等非法跳转 | 拒绝并提示，状态和日志不变 | | | 截图+SQL |

参考 SQL：
```sql
SELECT record_id, task_id, runner_id, operation_type, assigned_at, reassign_reason
FROM assign_records
WHERE task_id = :tid
ORDER BY assigned_at DESC, record_id DESC;

SELECT l.log_id, l.record_id, l.status_before, l.status_after, l.operator_user_id, l.operated_at
FROM task_status_logs l
JOIN assign_records ar ON ar.record_id = l.record_id
WHERE ar.task_id = :tid
ORDER BY l.operated_at, l.log_id;

WITH latest_assign AS (
    SELECT ar.*,
           ROW_NUMBER() OVER (
               PARTITION BY ar.task_id
               ORDER BY ar.assigned_at DESC, ar.record_id DESC
           ) AS rn
    FROM assign_records ar
)
SELECT r.runner_id, r.audit_status, r.work_status, COUNT(t.task_id) AS active_task_count
FROM runners r
LEFT JOIN latest_assign la ON la.runner_id = r.runner_id AND la.rn = 1
LEFT JOIN tasks t ON t.task_id = la.task_id
                 AND t.task_status IN ('ASSIGNED', 'PICKED_UP', 'DELIVERING', 'WAIT_CONFIRM')
WHERE r.runner_id = :runner
GROUP BY r.runner_id, r.audit_status, r.work_status;
```

### 组员7：收货后支付与退款（TC-PAY）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-PAY-01 | 未接单任务无支付 | 一条 WAITING 任务 | SQL 检查并访问支付页 | 无 `payments` 记录；无法对未接单任务支付 | | | SQL+截图 |
| TC-PAY-02 | 送达后确认收货并付款 | 任务处于 WAIT_CONFIRM | `/Task/Receipt` 确认收货 → `/Payment/Status` 选现金支付确认 | `payments` 创建/更新为 PAID；任务变 FINISHED；现金 `third_trade_no` 可为空 | | | 截图+SQL |
| TC-PAY-03 | 线上补付记录流水号 | WAIT_CONFIRM 任务 | 选微信/支付宝补付并确认 | 支付 PAID 且记录第三方交易流水号；任务 FINISHED | | | 截图+SQL |
| TC-PAY-04 | 重复确认幂等 | 已 FINISHED 任务 | 再次提交确认收货/付款 | 不重复扣款、不重复生成支付记录，状态不变 | | | 截图+SQL |
| TC-PAY-05 | 支付状态查询 | 已有支付记录 | `/Payment/Status` 查看 | 状态、金额、方式显示正确 | | | 截图 |
| TC-PAY-06 | 未付款任务不能退款 | 未支付的已接单任务 | 尝试提交退款 | 拒绝，只能取消或投诉 | | | 截图 |
| TC-PAY-07 | 退款申请 | 已 PAID 任务 | `/Refund/Create` 提交退款申请 | `refunds` 新增 APPLY 记录，任务进入 REFUNDING | | | 截图+SQL |
| TC-PAY-08 | 管理员审核退款 | 有 APPLY 退款 | `/Refund/AdminIndex` 审核同意 → 执行退款 | 状态 APPLY → APPROVED → DONE；支付变 REFUNDED | | | 截图+SQL |
| TC-PAY-09 | 管理员拒绝退款 | 有 APPLY 退款 | 审核拒绝 | 状态 REJECTED；支付保持 PAID，任务可继续正常流程 | | | 截图+SQL |

参考 SQL：
```sql
SELECT payment_id, record_id, amount, pay_method, pay_status, third_trade_no FROM payments WHERE record_id = :rid;
SELECT refund_id, payment_id, process_status, apply_reason FROM refunds WHERE payment_id = :pid;
```

### 组员8：评价、投诉与信誉（TC-REVIEW）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-REVIEW-01 | 完成后评价 | 任务 FINISHED 且支付 PAID | 发布者提交 5 分评价 | `reviews` 新增记录，绑定最终接派记录；跑腿员信誉 +2 | | | 截图+SQL |
| TC-REVIEW-02 | 分数-信誉映射 | 多条 FINISHED 任务 | 分别提交 4/3/2/1 分评价 | 信誉变化依次 +1/0/-1/-2，与评价写入同一事务 | | | SQL+结果 |
| TC-REVIEW-03 | 信誉下限保护 | 信誉接近 0 的跑腿员 | 提交 1 分差评 | 信誉扣减后不低于 0 | | | SQL+结果 |
| TC-REVIEW-04 | 每任务最多评价一次 | 已评价任务 | 再次提交评价 | 拒绝 | | | 截图 |
| TC-REVIEW-05 | 非完成/退款中任务不能评价 | 进行中任务；APPLY/APPROVED 退款任务 | 尝试评价 | 均拒绝 | | | 截图 |
| TC-REVIEW-06 | 他人任务不能评价 | 用户 B 登录 | B 对 A 的任务提交评价 | 拒绝（身份取自登录态） | | | 截图 |
| TC-REVIEW-07 | 投诉提交 | 已接单任务 | 发布者提交投诉 | `complaints` 新增 SUBMITTED 记录，绑定真实接派记录 | | | 截图+SQL |
| TC-REVIEW-08 | 管理员处理投诉 | 有 SUBMITTED 投诉 | `/Complaint/Index` 受理并填写处理结果 | 状态 SUBMITTED → PROCESSING → DONE，处理结果保存 | | | 截图+SQL |
| TC-REVIEW-09 | 管理员评价查询 | 已有多条评价 | `/Review/All` 查询 | 可按条件查看全部评价，数据正确 | | | 截图 |

参考 SQL：
```sql
SELECT review_id, task_id, record_id, score, content, created_at FROM reviews WHERE task_id = :tid;
SELECT runner_id, credit_score FROM runners WHERE runner_id = :runner;
SELECT complaint_id, record_id, process_status, result FROM complaints WHERE record_id = :rid;
```

### 组员9：结算、审计与报表（TC-SETTLE）

| 用例编号 | 测试点 | 前置条件 | 测试步骤 | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-SETTLE-01 | 生成结算单 | 跑腿员有 PAID 且无争议的支付记录 | `/Settlement` 为跑腿员生成结算单 | `settlements` WAITING；明细 `settlement_payment_items` 仅含该跑腿员、已支付、已完成、无活动退款的记录 | | | 截图+SQL |
| TC-SETTLE-02 | 重复结算限制 | 已结算过的支付记录 | 再次把同一 payment 纳入结算 | 拒绝；同一 `payment_id` 只结算一次 | | | 截图+SQL |
| TC-SETTLE-03 | 跑腿员归属校验 | 支付记录属于跑腿员 A | 尝试给跑腿员 B 结算该支付 | 校验拒绝 | | | 截图 |
| TC-SETTLE-04 | 退款/投诉中支付不结算 | 有 REFUNDING 或投诉中的支付 | 生成结算单 | 该类支付不进入结算明细 | | | SQL+结果 |
| TC-SETTLE-05 | 结算状态机 | 一条 WAITING 结算单 | 执行完成 → 尝试再次变更；再建一条置为 BLOCKED → 恢复 | 仅允许 WAITING→DONE、WAITING→BLOCKED、BLOCKED→WAITING；DONE 为终态不可改 | | | 截图+SQL |
| TC-SETTLE-06 | 三类审计 | 已有日志/支付/退款记录 | `/Audit` 分别审计 LOG、PAYMENT、REFUND | `audit_logs` 与对应明细表同事务写入，结果 PASS/ABNORMAL 正确 | | | 截图+SQL |
| TC-SETTLE-07 | 统计面板 | 有业务数据 | `/Report` 查看面板 | 指标与 SQL 手算一致 | | | 截图 |
| TC-SETTLE-08 | 报表生成与导出 | 有历史业务数据 | 生成 ORDER/PAYMENT/COMPLAINT 三类报表并导出 | 按 yyyy-MM 归集；导出 UTF-8 CSV；状态 GENERATED→EXPORTED；页面注明支付/投诉时间口径 | | | 截图+导出文件 |
| TC-SETTLE-09 | 删除报表不动审计 | 已生成报表 | 删除报表 | 仅删报表及关联，`audit_logs` 保留 | | | SQL+结果 |
| TC-SETTLE-10 | 跑腿员查看本人结算 | 跑腿员登录 | `/Settlement/My` | 只见本人结算单，数据正确 | | | 截图 |

参考 SQL：
```sql
SELECT settlement_id, runner_id, total_amount, settlement_status FROM settlements WHERE runner_id = :runner;
SELECT spi.payment_id FROM settlement_payment_items spi WHERE spi.settlement_id = :sid;
SELECT audit_id, audit_object, audit_result, created_at FROM audit_logs ORDER BY audit_id DESC FETCH FIRST 20 ROWS ONLY;
SELECT report_id, report_type, period, report_status FROM reports ORDER BY report_id DESC;
```

---

## 六、组员10 执行：完整业务流程与跨模块测试（TC-E2E）

| 用例编号 | 测试点 | 测试步骤（跨模块链路） | 预期结果 | 实际结果 | 结果 | 证据 |
| --- | --- | --- | --- | --- | --- | --- |
| TC-E2E-01 | 完整主流程一次走通 | 新用户注册 → 加地址 → 发外卖任务 → 跑腿员抢单 → 取货/配送/送达 → 用户确认收货并现金支付 → 用户评价 5 分 → 管理员生成结算 | 全链路状态依次流转至 FINISHED；支付 PAID；信誉 +2；结算明细包含该笔支付；各环节 SQL 数据链路完整（tasks→assign_records→payments→reviews→settlements） | | | 每环节截图+SQL |
| TC-E2E-02 | 派单+重派异常流 | 发快递任务无人抢 → 管理员派单 → 重派给另一跑腿员 → 送达 → 线上补付 | 旧接派保留、REASSIGN 生效；最终支付/评价绑定最新接派记录 | | | 截图+SQL |
| TC-E2E-03 | 退款售后流 | 完成一单私人跑腿并支付 → 用户申请退款 → 管理员审核同意 → 该支付不进入结算 | 退款 DONE、支付 REFUNDED；结算明细排除该笔 | | | 截图+SQL |
| TC-E2E-04 | 投诉流 | 完成一单 → 用户投诉 → 管理员处理完毕 → 生成投诉报表 | 投诉 DONE；COMPLAINT 报表含该记录 | | | 截图+SQL |
| TC-E2E-05 | 权限横向抽查 | 普通用户/跑腿员互访对方专属页面，访问管理员页 | 全部越权被拒，无数据泄露 | | | 截图 |
| TC-E2E-06 | 数据保留抽查 | 对已进入链路的任务/节点/用户尝试删除类操作 | 系统只能状态关闭（CANCELLED/CLOSED/BLOCKED），历史关系完整 | | | SQL+截图 |
| TC-E2E-07 | 报表口径复核 | 用 SQL 手算某月订单/支付/投诉指标，与 `/Report` 面板对比 | 数字一致；页面注明时间口径 | | | 截图+SQL |

## 七、通过标准

- 模块用例：全部执行，无未填实际结果的用例；失败用例均已登记 Bug。
- Bug 闭环：所有「严重」「主要」级 Bug 修复并通过回归后标记「已关闭」；「次要」级 Bug 经全组确认可遗留的在答辩材料中说明。
- 抽查：每模块至少抽查 2 条关键用例复核证据；TC-E2E 全部通过。
