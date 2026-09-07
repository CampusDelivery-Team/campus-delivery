# 组员 9：业务视图说明

本文说明第五阶段数据库完善中组员 9 负责的视图对象。视图用于把多表关联和统计口径沉到数据库查询层，方便后台页面、报表展示和数据库答辩演示。

## 执行顺序

1. 个人调试时可使用自己的数据库账号，但需要已获得 `APPUSER` 基础表的查询权限；最终交付时由数据库负责人统一在 `APPUSER` 下执行。
2. 执行 `01_views.sql` 创建 4 个业务视图。
3. 执行 `05_test.sql` 检查视图状态、行数和样例结果。
4. 如需回滚，仅执行 `06_rollback_views.sql` 删除本次新增视图。

这些脚本只创建或删除视图，不会修改基础表数据。

本目录沿用第五阶段分工文档建议的脚本目录。组员 9 当前只维护视图相关内容：`01_views.sql`、`05_test.sql` 中的视图测试部分、`06_rollback_views.sql` 中的视图回滚部分和本文档。`02_functions.sql`、`03_procedures.sql`、`04_triggers.sql` 应由对应负责函数、存储过程和触发器的组员补充，避免多人同时改同一份脚本造成冲突。

`01_views.sql` 中的基础表均显式写为 `APPUSER.表名`。因此个人账号执行时，视图会创建在个人 schema 下，但数据来源仍是 `APPUSER` 的业务表；管理员最终用 `APPUSER` 执行时，视图会创建为正式的 `APPUSER.VW_*` 对象。

## 视图清单

| 视图 | 作用 | 关联的网站业务 |
| --- | --- | --- |
| `VW_TASK_OVERVIEW` | 汇总任务、发布者、地址、节点、接派、支付、退款、评价、投诉和状态日志 | 任务详情、任务管理、报表明细 |
| `VW_PAYMENT_REFUND_OVERVIEW` | 汇总支付记录、最新退款状态、结算状态和任务双方信息 | 支付状态、退款审核、支付报表 |
| `VW_RUNNER_PERFORMANCE` | 按跑腿员聚合完成量、支付金额、评价、投诉、结算和可结算候选金额 | 跑腿员绩效、管理员报表、结算候选统计 |
| `VW_SETTLEMENT_REPORT` | 按结算单汇总结算快照、关联支付项和金额复核结果 | 结算列表、结算详情、结算报表导出 |

## 与后端的关系

视图只负责读数据，不负责改变业务状态。现有 C# 服务层仍然负责：

- 任务发布、抢单、派单和重派；
- 配送状态流转和状态日志写入；
- 支付确认、退款审核和结算单生成；
- 权限判断、事务控制和并发保护。

当前后端已将部分只读查询改为使用这些视图：

- `/Payment/Status` 支付列表查询使用 `VW_PAYMENT_REFUND_OVERVIEW`；
- `/Report` 首页统计、跑腿员绩效、订单报表明细和支付报表明细使用 `VW_TASK_OVERVIEW`、`VW_PAYMENT_REFUND_OVERVIEW`、`VW_RUNNER_PERFORMANCE`、`VW_SETTLEMENT_REPORT`；
- `/Settlement` 管理员结算列表、结算详情、跑腿员本人结算列表和明细使用 `VW_SETTLEMENT_REPORT`、`VW_PAYMENT_REFUND_OVERVIEW`。

写入类 SQL 不建议改成视图实现。


## 口径说明

- `VW_PAYMENT_REFUND_OVERVIEW.PAYMENT_BUSINESS_TIME` 使用 `tasks.completed_at`，为空时回退到 `tasks.created_at`。原因是当前表结构没有独立的 `paid_at` 字段。
- `VW_RUNNER_PERFORMANCE` 中可结算候选金额的判断条件与后端结算候选逻辑保持一致：支付已成功、任务已完成、未进入结算、没有活动投诉、没有申请中/已通过/已完成退款。
- `VW_SETTLEMENT_REPORT.DATA_CHECK_RESULT` 是轻量复核字段，用于提示结算快照金额是否和关联支付项合计一致。
