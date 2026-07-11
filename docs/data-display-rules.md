# 数据值与中文显示规则

本项目约定：数据库保存英文代码，MVC 页面显示中文名称。

## 原则

英文代码适合数据库约束、查询、接口传输和状态判断；中文名称适合展示给用户。

## 职责划分

| 层级 | 职责 |
| --- | --- |
| 数据库层 | 保存英文代码，并通过 CHECK 约束限制可选值 |
| Repository | 原样读写英文代码 |
| Service | 根据业务需要转换中文显示名 |
| ViewModel | 提供 `XxxDisplayName` 字段给页面 |
| Razor View | 显示中文字段，不直接写转换逻辑 |

## 当前基础资料与跑腿员示例

| 字段 | 英文代码 | 中文显示 |
| --- | --- | --- |
| `NodeType` | `GATE` | 校门 |
| `NodeType` | `STATION` | 驿站 |
| `NodeType` | `DISTRIBUTION` | 分发点 |
| `NodeStatus` | `NORMAL` | 正常 |
| `NodeStatus` | `CLOSED` | 关闭 |
| `ServiceTypeStatus` | `ENABLED` | 启用 |
| `ServiceTypeStatus` | `DISABLED` | 停用 |
| `RunnerAuditStatus` | `PENDING` | 待审核 |
| `RunnerAuditStatus` | `APPROVED` | 已通过 |
| `RunnerAuditStatus` | `REJECTED` | 已拒绝 |
| `RunnerWorkStatus` | `FREE` | 可接单 |
| `RunnerWorkStatus` | `BUSY` | 配送中 |
| `RunnerWorkStatus` | `OFFLINE` | 离线 |

表单示例：

```html
<option value="NORMAL">正常</option>
```

用户看到 `正常`，提交入库的是 `NORMAL`。
