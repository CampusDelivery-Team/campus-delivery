# 截图证据索引

> 历史说明：本索引记录 2026-08-16 当时的截图取得情况，保留 `PENDING-ENV` 不代表当前业务仍未验收。2026-08-22 的共享 Oracle 写入型验收结果统一见 `../2026-08-22-shared-e2e.md` 和 `../../../system-test-report.md`。

## 状态说明

- `PASS`：截图已真实取得，画面足以支持验收结论。
- `PASS-READONLY`：页面截图与只读 SQL 相互印证，证明已有业务结果可被当前版本正确读取；未在本次执行中新增或修改数据。
- `PARTIAL`：已取得真实页面或 SQL 证据，但数据为空或尚未执行写操作，不能证明完整业务闭环。
- `PENDING-ENV`：需要新版可用环境、测试账号或 Oracle 数据，当前未执行。
- `FAIL`：已执行但结果不符合预期，必须关联 Bug。

## A. 基础环境

| 编号 | 文件 | 验收点 | 当前状态 | 说明 |
| --- | --- | --- | --- | --- |
| ENV-01 | `screenshots/ENV-01-home.png` | 新版首页和真实功能入口 | PASS | 本地提交 `2917db9`，公开页面 |
| ENV-02 | `screenshots/ENV-02-login.png` | 登录表单与友好提示 | PASS | 未填写密码时截图 |
| ENV-03 | `screenshots/ENV-03-register.png` | 注册表单与字段校验入口 | PASS | 未提交注册数据 |
| ENV-04 | `screenshots/ENV-04-database-status.png` | Oracle 连接状态 | PASS | 页面显示“已连接”、用户数量 28；`sql/ENV-04-database-status.txt` 复核 APPUSER 用户数为 28 |
| ENV-05 | `screenshots/ENV-05-tests-pass.png` | 39 项测试和静态门禁通过 | PASS | 同步保留 `logs/ENV-05-tests-pass.txt` 和可复核 HTML 渲染源 |
| ENV-06 | `screenshots/ENV-06-admin-home.png` | 管理员登录态和角色工作台 | PASS | 当前管理员 Cookie 会话，可见运营、审核、结算审计入口 |

## B. 核心业务流程

| 编号 | 文件 | 角色 | 核心验收点 | SQL 证据 | 当前状态 |
| --- | --- | --- | --- | --- | --- |
| FLOW-01 | `screenshots/FLOW-01-user-login.png` | U1 | 登录成功并进入用户工作台 | `sql/FLOW-01-user-login.txt` | PENDING-ENV |
| FLOW-02 | `screenshots/FLOW-02-address-default.png` | U1 | 新增两条地址并切换默认地址 | `sql/FLOW-02-address-default.txt` | PENDING-ENV |
| FLOW-03 | `screenshots/FLOW-03-runner-apply.png` | U1 | 跑腿员资格申请提交成功 | `sql/FLOW-03-runner-apply.txt` | PENDING-ENV |
| FLOW-04 | `screenshots/FLOW-04-runner-audit.png` | A1 | 管理员审核申请并同步角色 | `sql/FLOW-04-runner-audit.txt` | PENDING-ENV |
| FLOW-05A | `screenshots/FLOW-05A-node.png` | A1 | 节点资料和状态可查看、存在维护入口 | `sql/FLOW-05-config.txt` | PASS-READONLY |
| FLOW-05B | `screenshots/FLOW-05B-service-type.png` | A1 | 三类服务及价格规则可查看、存在维护入口 | `sql/FLOW-05-config.txt` | PASS-READONLY |
| FLOW-05C | `screenshots/FLOW-05C-service-node-rule.png` | A1 | 服务节点绑定可查看、存在维护入口 | `sql/FLOW-05-config.txt` | PASS-READONLY |
| FLOW-06 | `screenshots/FLOW-06-task-food.png` | U1 | 发布外卖任务并保存专属字段 | `sql/FLOW-06-task-food.txt` | PENDING-ENV |
| FLOW-07 | `screenshots/FLOW-07-task-express.png` | U1 | 发布快递任务并保存专属字段 | `sql/FLOW-07-task-express.txt` | PENDING-ENV |
| FLOW-08 | `screenshots/FLOW-08-task-private.png` | U1 | 发布私人跑腿并保存专属字段 | `sql/FLOW-08-task-private.txt` | PENDING-ENV |
| FLOW-09 | `screenshots/FLOW-09-task-cancel.png` | U1 | 待接单任务取消为 CANCELLED | `sql/FLOW-09-task-cancel.txt` | PENDING-ENV |
| FLOW-10 | `screenshots/FLOW-10-grab-success.png` | R1 | 抢单成功，任务与跑腿员状态联动 | `sql/FLOW-10-grab-success.txt` | PENDING-ENV |
| FLOW-11A | `screenshots/FLOW-11A-concurrent-winner.png` | R1 | 并发抢单仅一人成功 | `sql/FLOW-11-concurrent.txt` | PENDING-ENV |
| FLOW-11B | `screenshots/FLOW-11B-concurrent-loser.png` | R2 | 失败会话收到友好提示 | `sql/FLOW-11-concurrent.txt` | PENDING-ENV |
| FLOW-12 | `screenshots/FLOW-12-admin-reassign.png` | A1 | 派单和重派正确释放/占用跑腿员 | `sql/FLOW-12-admin-reassign.txt` | PENDING-ENV |
| FLOW-13 | `screenshots/FLOW-13-delivery-status.png` | R1/R2 | 状态按取货、配送、待确认顺序流转 | `sql/FLOW-13-delivery-status.txt` | PENDING-ENV |
| FLOW-14 | `screenshots/FLOW-14-receipt-payment.png` | U1 | 确认收货并支付，任务 FINISHED | `sql/FLOW-14-receipt-payment.txt` | PENDING-ENV |
| FLOW-15 | `screenshots/FLOW-15-review.png` | U1 | 已支付完成任务只能评价一次 | `sql/FLOW-15-review.txt` | PENDING-ENV |
| FLOW-16 | `screenshots/FLOW-16-complaint.png` | A1 | 投诉管理页面可访问 | `sql/FLOW-16-complaint.txt` | PARTIAL（当前投诉数为 0，未证明提交与处理） |
| FLOW-17 | `screenshots/FLOW-17-refund.png` | A1 | 已有退款通过/驳回结果可查看 | `sql/FLOW-17-refund.txt` | PASS-READONLY（3 条退款记录，未在本次新增） |
| FLOW-18A | `screenshots/FLOW-18-settlement.png` | A1 | 结算列表、候选汇总与平台费率可查看 | `sql/FLOW-18-settlement.txt` | PASS-READONLY |
| FLOW-18B | `screenshots/FLOW-18B-settlement-details.png` | A1 | 结算单 #1 金额构成与 3 条支付明细一致 | `sql/FLOW-18-settlement.txt` | PASS-READONLY |
| FLOW-18C | `screenshots/FLOW-18C-settlement-candidates.png` | A1 | 候选页说明仅纳入已支付、已完成、无争议且未结算记录；当前无候选 | `sql/FLOW-18-settlement.txt` | PASS-READONLY |
| FLOW-19 | `screenshots/FLOW-19-audit.png` | A1 | 审计入口与待审对象统计可查看 | `sql/FLOW-19-audit.txt` | PARTIAL（当前审计记录为 0） |
| FLOW-19B | `screenshots/FLOW-19B-audit-payment-candidates.png` | A1 | 支付审计表单可读取 5 条候选记录 | `sql/FLOW-19-audit.txt` | PARTIAL（未提交审计；截图保留 BUG-04 修复前状态，待新版页面回归） |
| FLOW-20 | `screenshots/FLOW-20-report.png` | A1 | 报表实时指标、节点业务量与绩效可查看 | `sql/FLOW-20-report.txt` | PARTIAL（当前生成记录为 0，未验证导出） |

## C. 异常与安全

| 编号 | 文件 | 验收点 | 当前状态 |
| --- | --- | --- | --- |
| NEG-01 | `screenshots/NEG-01-unauthorized.png` | 未登录访问 `/Report` 被重定向登录页并保留 ReturnUrl | PASS |
| NEG-02 | `screenshots/NEG-02-invalid-transition.png` | 非法任务状态跳转被拒绝 | PENDING-ENV |
| NEG-03 | `screenshots/NEG-03-duplicate-payment.png` | 重复支付被拒绝且不产生第二条记录 | PENDING-ENV |
| NEG-04 | `screenshots/NEG-04-duplicate-review.png` | 重复评价被友好拒绝 | PENDING-ENV |
| NEG-05 | `screenshots/NEG-05-invalid-input.png` | 空字段、非法枚举或不存在 ID 提示友好 | PENDING-ENV |

## D. 缺陷截图

| 编号 | 文件 | 实际结果 | 关联 Bug |
| --- | --- | --- | --- |
| BUG-03 | `screenshots/BUG-20260816-03-review-all-error.png` | 管理员访问 `/Review/All` 进入统一错误页 | `BUG-20260816-03` |
| BUG-04 | `screenshots/FLOW-19B-audit-payment-candidates.png` | 修复前审计表单标题直接显示 Razor 文本 `@Model.AuditObjectDisplayName` | `BUG-20260816-04`（已修复/待页面回归） |
