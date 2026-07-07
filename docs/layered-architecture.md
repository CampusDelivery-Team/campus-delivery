# MVC 五层架构说明

项目采用 ASP.NET Core MVC 五层架构。

## 五层划分

| 层级 | 目录 | 职责 |
| --- | --- | --- |
| 表现层 | `Presentation/Views`、`Presentation/ViewModels`、`Presentation/wwwroot` | Razor 页面、表单模型、展示模型、CSS 等静态资源 |
| 控制层 | `Controllers` | 接收请求、绑定参数、调用业务层、返回 View 或 Redirect |
| 业务层 | `Services` | 业务规则、状态判断、显示名称转换、流程控制 |
| 持久层 | `Repositories`、`Persistence` | SQL、Oracle 连接、数据库读写 |
| 数据库层 | `database/oracle` | Oracle 建表脚本和数据库对象 |

调用方向固定为：

```text
View -> Controller -> Service -> Repository -> OracleConnectionFactory -> Oracle
```

不允许：

```text
View 直接访问 Repository
Controller 直接写复杂 SQL
Service 返回 Razor View
Repository 处理页面跳转或中文展示文案
```

## 当前落地模块

`Node` 模块已经完成五层闭环：

```text
Presentation/Views/Node/*
Presentation/ViewModels/Node*
Controllers/NodeController.cs
Services/NodeService.cs
Repositories/NodeRepository.cs
database/oracle/campus_runner_oracle_schema.sql 中的 nodes 表
```

首页 `/` 是系统门户页面，用于展示真实项目入口、服务类型、角色入口、流程和系统检测入口。

## 数据值显示规则

数据库存英文代码，页面显示中文名称。

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
Service/ViewModel：转换或承载中文显示名
View：展示中文文本
```
