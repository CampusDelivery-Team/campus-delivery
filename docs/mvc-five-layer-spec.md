# MVC 模块开发规范

新增模块按五层组织，并固定遵循下列调用方向：

```text
View -> Controller -> Service -> Repository -> OracleConnectionFactory -> Oracle
```

Controller 只负责请求、参数绑定、调用 Service 和返回 View/Redirect；Service 负责业务规则、流程控制和中文显示转换；Repository 负责参数化 SQL 与 Oracle 读写。

## 当前 Node 样板

```text
表现层：
Presentation/ViewModels/NodeCreateViewModel.cs
Presentation/ViewModels/NodeEditViewModel.cs
Presentation/ViewModels/NodeIndexViewModel.cs
Presentation/ViewModels/NodeListItemViewModel.cs
Presentation/Views/Node/Index.cshtml

控制层：
Controllers/NodeController.cs

业务层：
Services/NodeService.cs

持久层：
Repositories/NodeRepository.cs

数据库层：
database/oracle/campus_runner_oracle_schema.sql 中的 nodes 表
```

`Node` 当前采用同页新增和同页编辑：`Index.cshtml` 按需展开输入行，因此不存在独立的 `Create.cshtml`、`Edit.cshtml` 页面。Controller 仍以 `Create`、`Edit` POST Action 接收表单；Action 名称不等于必须存在同名 Razor 页面。

## 页面入口

首页 `/` 是面向用户的系统门户，不是技术说明页。首页和导航只能连接已经存在的 Controller 路由；任务发布、订单、接单、配送等后续模块尚未实现时，应显示为不可用状态，不能用 `#` 锚点或跳回首页的链接临时替代。

## 英文入库，中文显示

数据库枚举和状态字段统一保存英文代码。Repository 原样读写英文值；Service 或展示型 ViewModel 准备中文显示字段；Razor View 只展示中文字段。

```text
Repository 读取 nodes.node_type = GATE
Service 转换为 NodeTypeDisplayName = 校门
View 显示 校门
```

表单下拉框的 `value` 使用英文、用户看到中文：

```html
<option value="NORMAL">正常</option>
```

提交后入库的是 `NORMAL`，页面显示的是“正常”。
