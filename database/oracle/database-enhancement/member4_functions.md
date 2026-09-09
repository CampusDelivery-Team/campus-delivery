# 组员 4：数据库函数说明

本文说明第五阶段数据库完善中组员 4 负责的三个 Oracle 函数，以及与接单函数配套的一个原子接单过程。函数提供可复用的价格计算、接单资格判断和服务节点适用判断；`SP_ACCEPT_TASK_ATOMIC` 才负责锁行并完成接单写入。

## 文件与执行顺序

1. 使用隔离测试库，并确认当前连接用户为 `APPUSER`。
2. 执行 `03_procedures.sql` 创建或替换原子接单过程及其他成员过程。
3. 执行 `04_functions.sql` 创建或替换三个函数、清理旧版 `FN_GET_CREDIT_LEVEL`，并收紧信誉分约束。
4. 执行 `05_test.sql`，检查过程与函数状态、计价和资格边界、重复接单防护及信誉分约束。
5. 如需删除对象，分别执行 `06_rollback_procedures.sql` 和 `06_rollback_functions.sql`；信誉分约束和已经归一化的数据不会回滚。

脚本中的函数定义不会修改业务数据；但同一文件中的信誉分迁移会把历史超分截断为100并替换检查约束。第一次执行应先使用隔离库；共享库部署前必须备份超分记录、暂停评价和投诉写入，并由负责人使用 `APPUSER` 统一执行。

## 函数接口

### `FN_CALCULATE_TASK_PRICE`

```sql
fn_calculate_task_price(
    p_service_type_id IN service_types.service_type_id%TYPE,
    p_extra_amount    IN NUMBER DEFAULT 0
) RETURN NUMBER
```

返回启用服务类型的基础价格与显式附加费之和，并四舍五入到两位小数。基础价格始终读取 `service_types.base_price`，不硬编码在函数中；基础数据基线为外卖分发3元、快递代取4元、私人跑腿5元。附加费可以表达用户接受的距离、加急或复杂度加价，但必须由调用方显式给出且不得小于零。

当前表只有自然语言形式的 `distance_rule` 和 `urgent_rule`，没有距离、每公里单价或结构化加急费字段。按照 `docs/设计调整说明.md`，本轮不实现自动距离计价，也不从中文规则文本中提取金额。

异常：

- `-20041`：服务类型编号为空；
- `-20042`：附加费为空或小于零；
- `-20043`：启用的服务类型不存在。
- `-20045`：计算结果超过 `tasks.task_price NUMBER(10,2)` 可保存的上限。

示例：

```sql
SELECT fn_calculate_task_price(1, 2.50) AS task_price FROM dual;
```

当前网页由发布者填写非负附加费并实时预览“基础费 + 附加费”。后端不信任页面预览值，而是在创建任务的数据库事务中调用本函数，以数据库当前基础费重新计算并写入最终总价。系统仍不自动推导距离、重量、加急或复杂度费用。

### `FN_RUNNER_CAN_ACCEPT_TASK`

```sql
fn_runner_can_accept_task(
    p_runner_id IN runners.runner_id%TYPE,
    p_task_id   IN tasks.task_id%TYPE
) RETURN NUMBER
```

满足以下条件返回 `1`，否则返回 `0`：

- 用户角色为 `RUNNER`，账号状态为 `NORMAL`；
- 跑腿员审核状态为 `APPROVED`；
- 工作状态为 `FREE` 或 `BUSY`；
- 任务状态为 `WAITING`；
- 跑腿员不是任务发布者。

`BUSY` 可以继续接单是 2026-08-25 多单承接功能确定的当前规则。函数只做瞬时只读判断；真正抢单由后端在事务中调用 `SP_ACCEPT_TASK_ATOMIC`，过程取得任务行锁后再次检查状态，防止并发重复接单。

示例：

```sql
SELECT fn_runner_can_accept_task(1, 100) AS can_accept FROM dual;
```

### `FN_SERVICE_NODE_ALLOWED`

```sql
fn_service_node_allowed(
    p_service_type_id IN service_types.service_type_id%TYPE,
    p_node_id         IN nodes.node_id%TYPE
) RETURN NUMBER
```

服务类型处于 `ENABLED`、节点处于 `NORMAL`，且 `service_node_rules` 中存在对应绑定时返回 `1`，否则返回 `0`。空值和不存在的编号同样返回 `0`。任务发布后端使用该函数作为服务类型与交接节点的统一校验入口。

示例：

```sql
SELECT fn_service_node_allowed(1, 3) AS is_allowed FROM dual;
```

## 与其他成员的交付关系

- 组员 3 的存储过程可以调用价格或资格函数，但写入前仍需锁行并重新验证状态。
- 原子接单过程 `SP_ACCEPT_TASK_ATOMIC` 负责最终锁行和写入；资格函数用于快速失败判断，不能单独保证并发安全。
- 组员 10 应在隔离库中依次验证创建、重复创建、测试、回滚、再次创建，并保存 `USER_OBJECTS`、`USER_ERRORS` 和结果集截图。
- 组员 2 最终确认对象命名、执行账号、脚本总顺序及共享库部署窗口。

## 原子接单过程

`SP_ACCEPT_TASK_ATOMIC` 接收任务、跑腿员、操作者和 `SELF/ADMIN` 操作类型。在同一个调用中完成以下操作：

1. 对任务行执行 `SELECT ... FOR UPDATE`，等待其他接单事务结束；
2. 取得锁后重新确认任务仍是 `WAITING`；
3. 锁定并重新检查跑腿员、账号和操作者资格；
4. 将任务改为 `ASSIGNED`、跑腿员改为 `BUSY`，并写入接派记录与状态日志；
5. 通过输出参数返回稳定结果码。

过程故意不执行 `COMMIT` 或 `ROLLBACK`，事务最终由 ASP.NET Core 后端统一提交或回滚。两个会话抢同一任务时，后到者会在任务行锁处等待；先到者提交后，后到者读到的状态已不是 `WAITING`，因此返回 `TASK_NOT_WAITING`，不会生成第二条接派记录。

## 已知边界

- 价格函数不解析自然语言计价规则，不调用地图或距离服务。
- 价格函数使用数据库当前基础价；如管理员调价，后续计算会立即使用新值。
- 资格函数返回 `1` 不保证随后抢单一定成功，最终结果取决于写入事务取得行锁后的状态。
- 服务节点函数返回的是调用瞬间的配置状态，任务写入仍必须处于后端事务中。
