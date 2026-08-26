# TC-BASE SQL 查询结果

数据库时间：`2026-08-25 22:35:31.783 +08:00`

## 节点与历史引用

| NODE_ID | NODE_TYPE | NODE_NAME | LOCATION | OPEN_TIME | NODE_STATUS | TASK_COUNT |
| ---: | --- | --- | --- | --- | --- | ---: |
| 101 | STATION | B40825142756测试节点-已修改 | 嘉定校区测试区域A | 08:00-22:00 | NORMAL | 1 |

删除操作后仍返回该行，且页面提示“该节点已有任务记录，不能删除；可先关闭节点”。

## 服务类型与绑定

| SERVICE_TYPE_ID | SERVICE_NAME | BASE_PRICE | DISTANCE_RULE | URGENT_RULE | TYPE_STATUS |
| ---: | --- | ---: | --- | --- | --- |
| 101 | B40825142756测试服务 | 9.99 | 每公里加收1元 | 加急加收2元 | ENABLED |

最终 `service_node_rules` 返回 `(101, 101)`。解除绑定期间，标题为 `B40825142756未绑定组合` 的任务数量为 `0`；说明服务端拒绝后没有写入任务。验证结束后已重新绑定。

## 历史任务

| TASK_ID | TASK_TITLE | NODE_ID | SERVICE_TYPE_ID | TASK_STATUS |
| ---: | --- | ---: | ---: | --- |
| 255 | B40825142756历史引用任务 | 101 | 101 | WAITING |

## 跑腿员最终状态

| USER_ID | USERNAME | USER_ROLE | ACCOUNT_STATUS | RUNNER_ID | REAL_NAME | AUDIT_STATUS | WORK_STATUS | CREDIT_SCORE |
| ---: | --- | --- | --- | ---: | --- | --- | --- | ---: |
| 482 | b4runA8076859 | RUNNER | NORMAL | 381 | 甲测试员 | APPROVED | FREE | 100 |
| 483 | b4runB8076859 | RUNNER | NORMAL | 382 | 乙测试员重申 | APPROVED | FREE | 100 |

中间状态由对应页面截图和执行时即时查询共同确认：runner `#381` 申请后为 `PENDING / OFFLINE`；runner `#382` 依次为 `PENDING / OFFLINE`、`REJECTED / OFFLINE`、重新提交后的 `PENDING / OFFLINE`，最后为 `APPROVED / FREE`。
