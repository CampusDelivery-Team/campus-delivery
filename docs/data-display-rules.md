# 数据值与中文显示规则

本项目约定：数据库保存英文代码，MVC 页面显示中文名称。

## 为什么这样做

英文代码适合做约束、查询、接口传输和状态判断；中文名称适合展示给用户。

## 职责划分

| 层级 | 职责 |
| --- | --- |
| 数据库层 | 保存英文代码，并通过 CHECK 约束限制可选值 |
| Repository | 原样读写英文代码 |
| Service | 根据业务需要转换中文显示名 |
| ViewModel | 提供 `XxxDisplayName` 字段给页面 |
| Razor View | 显示中文字段，不直接写转换逻辑 |

## 当前 Node 示例

| 字段 | 英文代码 | 中文显示 |
| --- | --- | --- |
| `NodeType` | `GATE` | 校门 |
| `NodeType` | `STATION` | 驿站 |
| `NodeType` | `DISTRIBUTION` | 分发点 |
| `NodeStatus` | `NORMAL` | 正常 |
| `NodeStatus` | `CLOSED` | 关闭 |

表单示例：

```html
<option value="NORMAL">正常</option>
```

用户看到 `正常`，提交入库的是 `NORMAL`。
