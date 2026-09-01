# TC-SETTLE-09：删除报表不动审计

## 1. test-plan 原始要求

| 项目 | 内容 |
| --- | --- |
| 测试点 | 删除报表不动审计 |
| 前置条件 | 已生成报表 |
| 测试步骤 | 删除报表 |
| 预期结果 | 仅删报表及关联，`audit_logs` 保留 |
| 证据 | SQL + 结果 |

## 2. 测试目标解释与最终口径

本用例原本要验证“报表可以删除，但审计日志作为原始证据不能被删除”。这是合理的数据库设计要求：报表是二次汇总结果，审计日志是业务核查证据，删除报表不应破坏审计历史。

但当前系统实现中，`ReportController` 只有以下功能：

- `/Report`：报表首页。
- `/Report/Generate`：生成报表。
- `/Report/Details/{id}`：查看报表详情。
- `/Report/Export/{id}`：导出报表。

当前没有报表删除按钮、删除路由或仓储删除方法。因此本用例不能按原始 test-plan 完整执行。最终口径建议记录为：

- 当前实现未提供报表删除功能。
- 该用例按测试计划属于“阻塞 / 需确认”。
- 如果后续补充删除功能，再验证删除 `reports` 和 `report_audit_items` 时保留 `audit_logs`。

## 3. 前置条件与测试数据

- 已生成至少一张报表。
- 该报表最好有关联审计依据，即 `report_audit_items` 中存在对应 `report_id`。

## 4. 页面操作步骤

1. 管理员登录，进入 `/Report`。
2. 打开一张已生成报表详情 `/Report/Details/{report_id}`。
3. 检查页面是否存在删除按钮。
4. 检查当前系统是否存在删除报表入口。
5. 如果没有删除入口，则记录本用例为“阻塞 / 需确认”，并说明原因是当前实现未提供该功能。

## 5. SQL 验证

查询目标报表：

```sql
SELECT report_id, report_type, stat_period, generated_at, report_status
FROM APPUSER.reports
WHERE report_id = :report_id;
```

查询报表与审计日志关联：

```sql
SELECT report_id, audit_id
FROM APPUSER.report_audit_items
WHERE report_id = :report_id
ORDER BY audit_id;
```

查询关联审计日志是否存在：

```sql
SELECT audit_id, audit_object, audit_result, audited_at, exception_note
FROM APPUSER.audit_logs
WHERE audit_id IN (
    SELECT audit_id
    FROM APPUSER.report_audit_items
    WHERE report_id = :report_id
)
ORDER BY audit_id;
```

如果后续实现删除功能，删除后应重新执行：

```sql
SELECT report_id
FROM APPUSER.reports
WHERE report_id = :report_id;
```

```sql
SELECT report_id, audit_id
FROM APPUSER.report_audit_items
WHERE report_id = :report_id;
```

```sql
SELECT audit_id, audit_object, audit_result, audited_at
FROM APPUSER.audit_logs
WHERE audit_id IN (:audit_id_1, :audit_id_2);
```

预期：删除功能实现后，前两条删除后查询无结果，`audit_logs` 仍能查到原始审计记录。当前版本因为没有删除入口，应记录为阻塞或需求需确认。

## 6. 证据截图位置

### 页面截图

<!-- TODO: 放置报表详情页无删除入口截图 -->

<!-- TODO: 如后续实现删除功能，放置删除操作截图 -->

### SQL 截图

<!-- TODO: 放置删除前 reports 查询截图 -->

<!-- TODO: 放置删除前 report_audit_items 与 audit_logs 查询截图 -->

<!-- TODO: 如后续实现删除功能，放置删除后 SQL 对比截图 -->

## 7. 实际结果与结论

实际结果：待填写。当前需先确认是否要求本阶段补充报表删除功能。

结论：建议填写：阻塞 / 需确认。
