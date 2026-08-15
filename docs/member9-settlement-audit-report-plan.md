# 组员9结算审计报表模块开发计划

> 历史计划归档说明（2026-08-15）：结算、审计、报表及投诉页面现已落地。本文用于保留实施思路，不再作为当前完成度依据；当前能力与缺口见 `system-test-report.md`。

## 1. 模块定位

组员9负责管理员端的后置管理模块，范围包括跑腿员结算、审计日志和统计报表。

本模块不参与普通用户发布任务、跑腿员抢单、配送状态流转和用户支付的前半段流程，而是在这些业务数据已经产生之后，负责把结果汇总、校验、留痕和展示出来。

完整业务链路中的位置如下：

```text
任务发布
-> 跑腿员接单或管理员派单
-> 配送状态流转
-> 用户确认收货
-> 用户完成收货后支付
-> 评价或投诉
-> 组员9模块：结算、审计、统计报表
```

## 2. 当前项目现状

当前仓库已经不是从零项目，已有 ASP.NET Core MVC 五层结构：

```text
Controller -> Service -> Repository -> OracleConnectionFactory -> Oracle
```

已经存在的前置模块包括：

| 模块 | 当前情况 | 对组员9的影响 |
| --- | --- | --- |
| 任务模块 | 已有任务发布、任务大厅、我的任务、状态流转页面 | 报表和结算需要读取 `tasks` |
| 接派模块 | 已有抢单、派单、重派、状态日志 | 结算、评价、投诉、审计都依赖 `assign_records` |
| 支付模块 | 已有收货后支付逻辑，支付后任务进入 `FINISHED` | 结算以 `payments.pay_status = 'PAID'` 为核心入口 |
| 退款模块 | 已有退款相关代码 | 结算时应排除退款中或已退款支付 |
| 评价模块 | 已绑定 `record_id`，可影响跑腿员信誉分 | 报表可统计评价数量、评分情况 |
| 投诉模块 | 有 Controller / Service / Repository，但当前缺少 `Views/Complaint` 页面目录 | 结算依赖投诉状态，需与组员8确认处理口径 |

## 3. 负责数据表

组员9直接负责以下表：

| 表名 | 用途 |
| --- | --- |
| `settlements` | 跑腿员结算主表 |
| `settlement_payment_items` | 结算单和支付记录的对应明细 |
| `audit_logs` | 审计主表 |
| `audit_status_log_checks` | 审计与任务状态日志的关联 |
| `audit_payment_checks` | 审计与支付记录的关联 |
| `audit_refund_checks` | 审计与退款记录的关联 |
| `reports` | 报表生成记录 |
| `report_audit_items` | 报表与审计记录的关联 |

同时会读取以下前置表：

| 表名 | 用途 |
| --- | --- |
| `payments` | 判断可结算支付、统计支付金额 |
| `refunds` | 判断退款异常、统计退款情况 |
| `complaints` | 判断投诉未处理任务是否应暂缓结算 |
| `tasks` | 判断任务完成状态、统计任务数量 |
| `assign_records` | 追溯支付对应的跑腿员和任务 |
| `task_status_logs` | 状态流转审计 |
| `runners` | 结算对象和跑腿员绩效 |
| `nodes` | 节点业务量统计 |
| `service_types` | 服务类型统计 |

## 4. 数据库口径

开发以当前云端 Oracle 数据库实际表结构为准。截图确认本模块核心表字段与仓库 schema 的组员9部分基本一致：

```text
settlements:
  settlement_id, runner_id, order_total, platform_fee, net_income, settlement_status

settlement_payment_items:
  settlement_id, payment_id

audit_logs:
  audit_id, audit_object, audit_result, audited_at, exception_note

audit_status_log_checks:
  audit_id, log_id

audit_payment_checks:
  audit_id, payment_id

audit_refund_checks:
  audit_id, refund_id

reports:
  report_id, report_type, stat_period, generated_at, report_status

report_audit_items:
  report_id, audit_id
```

注意：分工 PDF 中提到中文枚举，但当前项目代码、README、业务规则文档和云端字段截图均使用英文枚举值。后续开发应保持当前项目口径：

```text
数据库保存英文枚举
页面显示中文名称
```

例如：

```text
WAITING -> 待接单
PAID -> 已支付
SUBMITTED -> 待处理
WAITING -> 待结算
DONE -> 已完成
BLOCKED -> 已阻断
```

## 5. 最终要做成什么样

### 5.1 管理员结算管理

最终页面入口：

```text
/Settlement
/Settlement/Candidates
/Settlement/Details/{id}
```

最终效果：

- 管理员可以查看结算单列表。
- 管理员可以查看可结算支付记录。
- 管理员可以按跑腿员生成结算单。
- 管理员可以查看结算明细，即一张结算单包含哪些支付记录。
- 系统阻止同一笔支付重复进入结算。
- 有未处理投诉、退款中或已退款的支付不进入普通结算。

### 5.2 管理员审计日志

最终页面入口：

```text
/Audit
/Audit/Payments
/Audit/Refunds
/Audit/StatusLogs
/Audit/Create
```

最终效果：

- 管理员可以查看审计日志。
- 管理员可以对支付记录做审计。
- 管理员可以对退款记录做审计。
- 管理员可以对任务状态日志做审计。
- 审计结果支持 `PASS` 和 `ABNORMAL`。
- 异常审计可以填写异常说明。
- 审计主表和审计关联表必须在同一事务中写入。

### 5.3 管理员统计报表

最终页面入口：

```text
/Report
/Report/Order
/Report/Payment
/Report/Complaint
/Report/RunnerPerformance
```

最终效果：

- 管理员可以查看订单数量统计。
- 管理员可以查看支付金额统计。
- 管理员可以查看退款率和投诉率。
- 管理员可以查看节点业务量。
- 管理员可以查看跑腿员绩效。
- 管理员可以生成报表记录，写入 `reports` 表。

报表页面优先使用表格展示，不强制做复杂图表。原因是当前 `reports` 表只保存报表类型、统计周期、生成时间和状态，没有保存具体统计结果的字段。实际统计结果应通过查询实时展示，`reports` 表用于记录生成动作。

## 6. 平台抽成规则

课程设计演示采用固定平台抽成比例：

```text
平台抽成比例 = 10%
平台抽成 = 可结算支付金额合计 * 10%
跑腿员净收入 = 可结算支付金额合计 - 平台抽成
```

示例：

```text
某跑腿员本次有 3 笔可结算支付：
10 元 + 12 元 + 8 元 = 30 元

平台抽成：
30 * 10% = 3 元

跑腿员净收入：
30 - 3 = 27 元
```

代码中建议定义为常量：

```csharp
private const decimal PlatformFeeRate = 0.10m;
```

答辩解释口径：

```text
当前系统为课程设计演示版本，平台服务费采用固定 10% 抽成规则，便于结算逻辑清晰、数据可验证。后续真实系统可将抽成比例配置化。
```

## 7. 结算业务规则

可结算支付必须同时满足：

1. 支付状态为 `PAID`。
2. 对应任务状态为 `FINISHED`。
3. 对应 `payment_id` 不存在于 `settlement_payment_items`。
4. 对应接派记录没有未处理投诉。
5. 对应支付没有退款中或已退款记录。
6. 支付对应的跑腿员必须和结算单 `runner_id` 一致。

建议未处理投诉口径：

```text
complaints.process_status IN ('SUBMITTED', 'PROCESSING')
```

即：

- `SUBMITTED`：待处理，不结算。
- `PROCESSING`：处理中，不结算。
- `DONE`：已处理，可以进入结算候选，但后续是否阻断需要结合组员8是否支持“投诉成立/驳回”。

当前 `complaints` 表只有 `process_status` 和 `process_result`，没有明确“成立/驳回”字段。因此第一版结算逻辑先按“未处理投诉不结算，已处理投诉允许结算”实现。

如果组员8后续在 `process_result` 中固定写入“成立/驳回”，组员9可以再扩展为：

```text
投诉成立 -> settlement_status = BLOCKED 或不进入普通结算
投诉驳回 -> 可以结算
```

## 8. 审计功能说明

审计是管理员在平台里的后台功能，不是数据库管理员功能。

也就是说，审计页面应由平台中的 `ADMIN` 角色使用。管理员登录系统后，在网页菜单中进入审计页面，查看或登记审计结果。

审计功能面向三类对象：

| 审计对象 | `audit_object` | 关联表 |
| --- | --- | --- |
| 状态日志 | `LOG` | `audit_status_log_checks` |
| 支付记录 | `PAYMENT` | `audit_payment_checks` |
| 退款记录 | `REFUND` | `audit_refund_checks` |

审计结果：

| `audit_result` | 页面显示 |
| --- | --- |
| `PASS` | 通过 |
| `ABNORMAL` | 异常 |

写入规则：

```text
插入 audit_logs
-> 根据 audit_object 插入对应 audit_*_checks 表
-> 同一个事务提交
```

## 9. 管理员平台账号与数据库账号的区别

本模块开发和测试会遇到两类账号：

| 账号类型 | 用途 | 示例 |
| --- | --- | --- |
| 平台管理员账号 | 登录网页后台，访问 `/Settlement`、`/Audit`、`/Report` | 由组内提供的管理员账号 |
| Oracle 数据库账号 | 后端连接数据库，执行 SQL 查询和写入 | `APPREAD` 或写权限账号 |

二者不是一回事。

平台管理员账号用于浏览器登录系统：

```text
http://localhost:5227/Auth/Login
```

Oracle 数据库账号写在连接串中：

```text
User Id=<数据库用户名>;Password=<数据库密码>;Data Source=localhost:15210/orclpdb1;
```

计划文档不记录真实密码。开发者本地通过环境变量或 `appsettings.Local.json` 配置数据库连接。

## 10. 与组员8的协作点

组员8负责评价投诉与信誉模块。当前代码情况：

- 评价模块已有基础实现，绑定 `record_id`，并会根据 `credit_delta` 更新跑腿员信誉分。
- 投诉模块有 Controller、Service、Repository。
- 投诉页面已落地在 `Presentation/Views/Complaint`；结算候选仍按投诉状态过滤。
- 投诉处理状态目前为 `SUBMITTED`、`PROCESSING`、`DONE`，没有明确“成立/驳回”字段。
- 投诉判定成立时，`ComplaintService` 会在处理事务内扣减跑腿员 10 点信誉分。

对组员9的影响：

1. 结算模块需要读取 `complaints.process_status`。
2. 第一版可以只判断未处理投诉：

```text
SUBMITTED / PROCESSING -> 暂缓结算
DONE -> 允许结算
```

3. 如果组员8需要支持投诉成立后阻断结算，需要双方约定：

```text
process_result 中是否固定包含“成立”或“驳回”
或者后续是否增加更明确的状态口径
```

4. 组员9不应修改组员8的投诉主流程，避免冲突。组员9只在自己的结算查询中读取投诉状态。

建议与组员8沟通的问题：

```text
1. 投诉页面是否已经计划补齐？
2. 投诉处理完成后，如何区分投诉成立和投诉驳回？
3. 投诉成立是否一定扣减跑腿员信誉分？
4. 投诉成立的订单是否应该永远不结算，还是进入 BLOCKED 结算单？
```

## 11. 与组员7的协作点

组员7负责支付和退款。组员9结算依赖支付与退款数据。

当前计划采用以下口径：

```text
payments.pay_status = 'PAID' -> 可能进入结算
payments.pay_status = 'REFUNDED' -> 不进入结算
refunds.process_status IN ('APPLY', 'APPROVED', 'DONE') -> 不进入普通结算
refunds.process_status = 'REJECTED' -> 可结算
```

建议与组员7沟通的问题：

```text
1. 退款表真实状态值是否为 APPLY / APPROVED / REJECTED / DONE？
2. 退款申请批准后，支付状态是否会立即变为 REFUNDED？
3. 支付记录是否一定绑定真实 assign_records.record_id？
4. 支付成功后任务是否一定进入 FINISHED？
```

## 12. 开发文件计划

### 12.1 Models

```text
Models/Settlement.cs
Models/SettlementCandidate.cs
Models/SettlementPaymentItem.cs
Models/AuditLog.cs
Models/ReportRecord.cs
```

### 12.2 Repositories

```text
Repositories/SettlementRepository.cs
Repositories/AuditRepository.cs
Repositories/ReportRepository.cs
```

### 12.3 Services

```text
Services/SettlementService.cs
Services/AuditService.cs
Services/ReportService.cs
```

### 12.4 Controllers

```text
Controllers/SettlementController.cs
Controllers/AuditController.cs
Controllers/ReportController.cs
```

### 12.5 ViewModels

```text
Presentation/ViewModels/SettlementIndexViewModel.cs
Presentation/ViewModels/SettlementCandidateViewModel.cs
Presentation/ViewModels/SettlementDetailsViewModel.cs
Presentation/ViewModels/AuditIndexViewModel.cs
Presentation/ViewModels/AuditCreateViewModel.cs
Presentation/ViewModels/ReportDashboardViewModel.cs
```

### 12.6 Views

```text
Presentation/Views/Settlement/Index.cshtml
Presentation/Views/Settlement/Candidates.cshtml
Presentation/Views/Settlement/Details.cshtml
Presentation/Views/Audit/Index.cshtml
Presentation/Views/Audit/Create.cshtml
Presentation/Views/Report/Index.cshtml
```

### 12.7 Shared changes

```text
Program.cs
Presentation/Views/Shared/_Layout.cshtml
Services/DisplayNameService.cs
```

## 13. 开发阶段安排

### 阶段一：结算查询页面

目标：

- 管理员能打开结算管理页面。
- 能看到结算单列表。
- 能看到可结算支付记录。

验收：

```text
/Settlement 可打开
/Settlement/Candidates 可打开
能列出 PAID 且未结算的 payment
dotnet build 通过
```

### 阶段二：生成结算单

目标：

- 管理员可以按跑腿员生成结算单。
- 系统自动计算订单总额、平台抽成、净收入。
- 同一 payment_id 不能重复结算。

验收：

```text
settlements 新增记录
settlement_payment_items 新增明细
重复生成会被阻止
金额计算正确
事务失败时不产生半条数据
```

### 阶段三：审计日志

目标：

- 管理员可以查看审计记录。
- 管理员可以对支付、退款、状态日志登记审计结果。

验收：

```text
audit_logs 新增记录
对应 audit_*_checks 新增关联
PASS / ABNORMAL 显示中文
异常说明可保存
```

### 阶段四：统计报表

目标：

- 管理员可以查看核心统计。
- 管理员可以生成报表记录。

验收：

```text
任务数、完成数、支付金额、退款数、投诉数可展示
节点业务量可展示
跑腿员绩效可展示
reports 可写入生成记录
```

### 阶段五：整合与答辩准备

目标：

- 管理员导航入口完整。
- 页面中文展示清晰。
- 可演示从支付到结算、审计、报表的闭环。

验收：

```text
管理员登录后可进入结算、审计、报表
dotnet build 通过
能用 SQL Developer / DBeaver 验证数据库变化
准备每个页面截图
能讲清楚本模块负责表和业务规则
```

## 14. 风险与处理

| 风险 | 影响 | 处理方式 |
| --- | --- | --- |
| 投诉状态与结算联动 | 结算需排除待处理投诉 | 当前按 `complaints` 表状态查询，并由系统测试验证阻断 |
| 投诉没有成立/驳回字段 | 无法精确判断投诉成立后是否结算 | 第一版仅阻断未处理投诉；后续与组员8约定 `process_result` 口径 |
| 支付/退款测试数据不足 | 可结算候选为空 | 先完成空状态页面；用写权限账号准备少量测试数据 |
| 报表表没有统计明细字段 | 无法把统计结果完整落库 | 页面实时统计，`reports` 表只记录生成动作 |
| 云数据库字段和仓库文档不一致 | SQL 运行失败 | 以云数据库 `all_tab_columns` 查询结果为准 |

## 15. 最小可答辩版本

如果时间紧，组员9至少完成：

1. 管理员查看可结算支付记录。
2. 管理员生成跑腿员结算单。
3. 系统阻止重复结算。
4. 管理员查看结算明细。
5. 管理员查看基础统计报表。
6. 管理员查看审计日志列表。

最低演示路径：

```text
管理员登录
-> 进入结算管理
-> 查看已支付未结算记录
-> 生成结算单
-> 查看结算详情
-> 进入统计报表
-> 查看支付金额和跑腿员绩效
-> 使用 DBeaver 验证 settlements 和 settlement_payment_items 数据变化
```

## 16. 答辩讲解口径

组员9答辩时重点讲：

1. 本模块属于订单完成后的后台管理模块。
2. 结算以已支付记录为基础，而不是直接按任务结算。
3. `settlement_payment_items.payment_id` 唯一约束保证一笔支付不能重复结算。
4. 未处理投诉和退款异常会阻断普通结算，避免财务风险。
5. 审计日志用于记录管理员对支付、退款和状态日志的核验结果。
6. 报表页面实时统计业务数据，`reports` 表记录报表生成动作。
7. 当前演示版采用固定 10% 平台抽成，后续可配置化。

