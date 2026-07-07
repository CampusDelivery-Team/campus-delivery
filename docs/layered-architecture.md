# MVC 五层架构说明

项目统一采用 ASP.NET Core MVC 五层架构。

## 五层划分

| 层级 | 目录 | 职责 |
| --- | --- | --- |
| 表现层 | `Presentation/Views`、`Presentation/ViewModels`、`Presentation/wwwroot` | Razor 页面、表单模型、页面样式和脚本 |
| 控制层 | `Controllers` | 接收请求、绑定表单、调用业务层、返回 View 或 Redirect |
| 业务层 | `Services` | 业务规则、状态判断、流程控制 |
| 持久层 | `Repositories`、`Persistence` | SQL、Oracle 连接、数据读写 |
| 数据库层 | `database/oracle` | 建表脚本、初始化脚本、数据库对象 |

## 调用方向

```text
View -> Controller -> Service -> Repository -> Persistence -> Oracle
```

不允许：

```text
View 直接访问 Repository
Controller 直接写复杂 SQL
Service 返回 Razor View
Repository 处理页面跳转
```

## 已落地示例

`Node` 模块已经完成五层闭环：

```text
Presentation/Views/Node/*
Presentation/ViewModels/Node*
Controllers/NodeController.cs
Services/NodeService.cs
Repositories/NodeRepository.cs
database/oracle/campus_runner_oracle_schema.sql 中的 nodes 表
```

后续模块按同样结构扩展。

## 数据值显示规则

数据库层存英文代码，表现层显示中文名称。

示例：

| 表字段 | 数据库存储 | 页面显示 |
| --- | --- | --- |
| `nodes.node_type` | `GATE` | 校门 |
| `nodes.node_type` | `STATION` | 驿站 |
| `nodes.node_type` | `DISTRIBUTION` | 分发点 |
| `nodes.node_status` | `NORMAL` | 正常 |
| `nodes.node_status` | `CLOSED` | 关闭 |

职责边界：

```text
Repository：读写英文代码
Service/ViewModel：把英文代码转换成中文显示名
View：只展示已经准备好的中文文本
```

不要把中文显示名直接写入数据库枚举字段。
