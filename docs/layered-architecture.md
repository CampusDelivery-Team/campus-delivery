# MVC 五层架构说明

项目采用 ASP.NET Core MVC 五层架构。

## 五层划分

| 层级 | 目录 | 职责 |
| --- | --- | --- |
| 表现层 | `Presentation/Views`、`Presentation/ViewModels`、`Presentation/wwwroot` | Razor 页面、表单模型、展示模型、CSS 等静态资源 |
| 控制层 | `Controllers` | 接收请求、绑定参数、调用业务层、返回 View 或 Redirect |
| 业务层 | `Services`、`Services/Interfaces` | 业务接口、业务规则、状态判断、显示名称转换、流程与事务边界控制 |
| 持久层 | `Repositories`、`Repositories/Interfaces`、`Persistence` | 仓储接口、参数化 SQL、Oracle 连接与数据库读写 |
| 数据库层 | `database/oracle` | Oracle 建表脚本和数据库对象 |

调用方向固定为：

```text
View -> Controller -> IService -> Service -> IRepository -> Repository -> OracleConnectionFactory -> Oracle
```

Controller 只注入 Service 接口，Service 只注入 Repository 接口。Oracle 类型和 SQL 只能出现在 Repository/Persistence；不允许 View 直接访问 Repository、Controller 直接写 SQL、Service 返回 Razor View，或 Repository 处理页面跳转和中文展示文案。

## 当前落地模块

| 模块 | 数据表 | 已实现能力 |
| --- | --- | --- |
| `Node` | `nodes` | 管理员同页新增、编辑、关闭和恢复 |
| `ServiceType` | `service_types` | 管理员同页新增、编辑、启用和停用 |
| `ServiceNodeRule` | `service_node_rules` | 管理员绑定服务类型和节点、受限解除 |
| `Runner` | `runners`、`users` | 用户资格申请/重新申请，管理员审核与工作状态维护 |

首页 `/` 会根据访客、普通用户、跑腿员和管理员身份展示真实入口。任务发布、订单、任务大厅、接单、配送、评价、投诉、结算和报表仍是后续模块，当前没有 Controller 路由或可点击入口。

## 数据值显示规则

数据库存英文代码，页面显示中文名称。

| 表字段 | 数据库存储 | 页面显示 |
| --- | --- | --- |
| `nodes.node_type` | `GATE` | 校门 |
| `nodes.node_type` | `STATION` | 驿站 |
| `nodes.node_type` | `DISTRIBUTION` | 分发点 |
| `nodes.node_status` | `NORMAL` | 正常 |
| `nodes.node_status` | `CLOSED` | 关闭 |
| `service_types.type_status` | `ENABLED` | 启用 |
| `service_types.type_status` | `DISABLED` | 停用 |

Repository 读写英文代码；Service/ViewModel 转换或承载中文显示名；View 展示中文文本。
