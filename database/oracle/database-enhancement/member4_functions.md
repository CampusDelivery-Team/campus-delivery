# 组员 4：数据库函数说明

本文说明第五阶段数据库完善中组员 4 负责的三个 Oracle 函数。函数用于提供可复用的价格计算、接单资格判断和信誉等级展示口径，不替代 C# Service 的身份授权、事务、行锁与业务写入。

## 文件与执行顺序

1. 使用隔离测试库，并确认当前连接用户为 `APPUSER`。
2. 执行 `04_business_functions_and_credit_rules.sql` 创建或替换三个函数，并收紧信誉分约束。
3. 执行 `05_test.sql` 中的组员4测试，检查函数、边界值、异常输入和信誉分约束。
4. 如需删除函数，执行 `06_business_functions_and_credit_rules.sql`；信誉分约束和已经归一化的数据不会回滚。

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

当前网页发布任务仍由发布者填写最终总价，后端只强制总价不得低于所选服务类型基础价；它不会自动调用该函数，也不会自动推导附加费。

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

`BUSY` 可以继续接单是 2026-08-25 多单承接功能确定的当前规则。函数只做瞬时只读判断；真正抢单仍须由 `AssignService` 在事务中锁定任务和跑腿员，防止并发重复接单。

示例：

```sql
SELECT fn_runner_can_accept_task(1, 100) AS can_accept FROM dual;
```

### `FN_GET_CREDIT_LEVEL`

```sql
fn_get_credit_level(
    p_credit_score IN runners.credit_score%TYPE
) RETURN VARCHAR2
```

第五阶段新增的只读展示分级：

| 分数 | 返回代码 | 中文展示建议 |
| --- | --- | --- |
| `>= 90` | `EXCELLENT` | 优秀 |
| `80-89` | `GOOD` | 良好 |
| `70-79` | `NORMAL` | 正常 |
| `60-69` | `WATCH` | 需关注 |
| `0-59` | `RISK` | 风险 |

信誉分业务范围为0至100，评价按星级产生 `-2` 到 `+2` 的理论变动，实际生效值同时受上下限截断。这组等级阈值是本次数据库完善新增的展示口径，不用于自动封禁、禁止接单或阻断结算；如项目负责人以后调整阈值，应同步修改函数、测试和本文。

负数、大于100或空分数抛出 `-20044`。

示例：

```sql
SELECT runner_id,
       credit_score,
       fn_get_credit_level(credit_score) AS credit_level
  FROM runners;
```

## 与其他成员的交付关系

- 组员 3 的存储过程可以调用价格或资格函数，但写入前仍需锁行并重新验证状态。
- 组员 9 的视图可以调用信誉等级函数作为只读展示字段。
- 组员 10 应在隔离库中依次验证创建、重复创建、测试、回滚、再次创建，并保存 `USER_OBJECTS`、`USER_ERRORS` 和结果集截图。
- 组员 2 最终确认对象命名、执行账号、脚本总顺序及共享库部署窗口。

## 已知边界

- 价格函数不解析自然语言计价规则，不调用地图或距离服务。
- 价格函数使用数据库当前基础价；如管理员调价，后续计算会立即使用新值。
- 资格函数返回 `1` 不保证随后抢单一定成功，最终结果取决于写入事务取得行锁后的状态。
- 信誉等级是展示分类，不是信用认证或风险预测结论。
