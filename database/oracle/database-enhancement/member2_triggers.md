# 组员2：任务状态与关键数据审计触发器

## 1. 项目结论

项目的任务、支付、退款、审计不是彼此独立的演示表，而是一条完整业务链：

```text
tasks
  -> assign_records
  -> task_status_logs
  -> payments
  -> refunds
  -> audit_logs + 三张审计关联表
```

应用已经在真实业务事务中更新 `tasks.task_status`，并主动写入包含
`record_id`、前后状态和真实操作人的 `task_status_logs`。因此不能再创建一个
监听 `tasks` 并重复插入状态日志的触发器，否则会产生重复日志，而且数据库
会丢失网站当前登录用户的 `operator_user_id`。

本实现保留应用负责业务操作，触发器负责自动核验和留痕，不新增第 25 张表，
继续使用现有 `audit_logs`、`audit_status_log_checks`、
`audit_payment_checks` 和 `audit_refund_checks`。

## 2. 创建的触发器

| 触发器 | 监听对象 | 用途 |
| --- | --- | --- |
| `TRG_TASK_STATUS_AUDIT` | `task_status_logs` 插入 | 自动核验任务状态变化 |
| `TRG_PAYMENT_CHANGE_AUDIT` | `payments` 插入及关键列更新 | 自动核验支付关键数据变化 |
| `TRG_REFUND_CHANGE_AUDIT` | `refunds` 插入及关键列更新 | 自动核验退款关键数据变化 |

图片要求的是两类触发器，但“关键数据”分布在支付和退款两张表。Oracle 的行级
触发器一次只能绑定一张表，所以第二类在实现上合理拆成两个触发器，共三个数据库对象。

## 3. 任务状态审计规则

以下路径记为 `PASS`：

```text
WAITING -> ASSIGNED
ASSIGNED -> PICKED_UP
PICKED_UP -> DELIVERING
DELIVERING -> WAIT_CONFIRM
WAIT_CONFIRM -> WAIT_CONFIRM   （发布者确认收货的确认日志）
WAIT_CONFIRM -> FINISHED
FINISHED -> REFUNDING
REFUNDING -> FINISHED
ASSIGNED -> ASSIGNED           （重派后任务仍处于已接单状态）
```

这九种路径与共享库中截至 2026-09-11 的 406 条真实状态日志完全一致。
其他组合不会被触发器强行拦截，而是写入一条 `ABNORMAL` 审计，便于管理员发现
绕过应用层、脚本误操作或后续业务代码缺陷。

## 4. 支付关键数据审计规则

触发器监听：

```text
record_id, order_amount, pay_amount, pay_method, third_trade_no, pay_status
```

合理的初始状态为 `UNPAID`、`PAID` 或 `FAILED`。认可的状态变化为：

```text
UNPAID -> PAID
UNPAID -> FAILED
FAILED  -> UNPAID
FAILED  -> PAID
PAID    -> REFUNDED
```

已支付或已退款后又修改金额、支付方式、所属接派记录等关键数据，或者发生状态
倒退，会写 `ABNORMAL`。对值没有任何实际改变的空更新不会制造审计噪声。

## 5. 退款关键数据审计规则

触发器监听：

```text
payment_id, refund_amount, refund_reason, approved_amount, process_status
```

退款必须从 `APPLY` 创建，认可的状态变化为：

```text
APPLY    -> APPROVED
APPLY    -> REJECTED
APPROVED -> DONE
```

同时检查：待审核记录不应提前有核定金额；通过金额不能大于申请金额；驳回时核定
金额应为 0；已通过、已驳回或已完成记录不应再次修改关键内容。不符合规则时记录
`ABNORMAL`，但不替代 Service 层和约束层的业务校验。

## 6. 事务与失败策略

触发器没有 `COMMIT`、`ROLLBACK` 或自治事务。审计主表、审计关联表与业务变化
属于同一事务：业务回滚时审计同步回滚，审计写入发生技术故障时业务事务也会失败。
这是为了避免“业务已成功但审计记录永久缺失”的不一致状态。

触发器本身只分类和记录，不使用 `RAISE_APPLICATION_ERROR` 拦截被判定为异常的业务值。
是否拒绝业务仍由后端 Service、存储过程和数据库约束共同负责。

## 7. 执行顺序

正式 Schema 的第五阶段执行顺序应为：

```text
01_views.sql
02_triggers.sql
03_procedures.sql
04_functions.sql
05_test.sql
```

回滚触发器：

```text
06_rollback_triggers.sql
```

回滚脚本只删除三个触发器，不删除业务数据和已经形成的历史审计数据。

## 8. 个人 Schema 隔离测试结果

测试日期：2026-09-11。

测试位置：`APP2452098` 个人 Schema，仅使用 `M2_` 前缀隔离表。没有在
`APPUSER` 创建触发器，也没有修改团队正式业务数据。测试时临时授予 10 MB
表空间配额，完成后已经恢复为 0；没有新增 `CREATE TABLE` 系统权限。

测试结果：

| 检查项 | 结果 |
| --- | --- |
| 三个触发器状态 | `ENABLED` |
| 编译错误 | 0 |
| 合法任务变化 | 生成 `LOG/PASS` |
| 非法任务变化 | 生成 `LOG/ABNORMAL` |
| 业务回滚 | 业务行和审计行同时回滚 |
| 支付创建及合法付款 | 生成 `PAYMENT/PASS` |
| 已支付后修改金额 | 生成 `PAYMENT/ABNORMAL` |
| 支付状态倒退 | 生成 `PAYMENT/ABNORMAL` |
| 支付空更新 | 不新增审计 |
| 退款申请及审核通过 | 生成 `REFUND/PASS` |
| 退款状态倒退 | 生成 `REFUND/ABNORMAL` |

最终保留 9 条隔离审计记录。审计编号缺少 3 是预期现象：该编号在保存点之后
生成，随后业务与审计一起回滚；Oracle identity/sequence 回滚后不会回收编号。

个人 Schema 中保留了三个触发器和七张 `M2_` 测试表，便于在 DBeaver 中复查；
它们与指向 `APPUSER` 的原有同义词名称不冲突。

## 9. 当前部署状态

仓库脚本和个人 Schema 隔离验证已经完成；截至 2026-09-11，三个触发器尚未部署
到 `APPUSER` 正式 Schema。正式部署后，每次真实状态日志、支付创建/变化和退款
创建/变化都会额外产生对应审计记录，部署前应由数据库负责人确认该行为影响。
