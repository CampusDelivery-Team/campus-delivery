# MVC 模块开发规范

新增模块时按五层创建文件。

以 `Node` 为例：

```text
表现层：
Presentation/ViewModels/NodeCreateViewModel.cs
Presentation/ViewModels/NodeEditViewModel.cs
Presentation/ViewModels/NodeIndexViewModel.cs
Presentation/ViewModels/NodeListItemViewModel.cs
Presentation/Views/Node/Index.cshtml
Presentation/Views/Node/Create.cshtml
Presentation/Views/Node/Edit.cshtml

控制层：
Controllers/NodeController.cs

业务层：
Services/NodeService.cs

持久层：
Repositories/NodeRepository.cs

数据库层：
database/oracle/campus_runner_oracle_schema.sql
```

Controller 只负责请求和响应；Service 负责业务规则和显示转换；Repository 负责 SQL 和数据库读写。

## 页面入口

当前首页 `/` 是项目主页面，不是技术说明页。后续模块入口应从首页的服务类型、角色入口或管理员入口继续接入。

## 英文入库，中文显示

数据库中的枚举和状态字段统一保存英文代码。

页面需要中文时，不改数据库值，应在 Service 或展示型 ViewModel 中提供中文显示字段。

示例：

```text
Repository 读取 nodes.node_type = GATE
Service 转换为 NodeTypeDisplayName = 校门
View 显示 校门
```

表单下拉框的 `value` 使用英文，用户看到中文：

```html
<option value="NORMAL">正常</option>
```

提交后入库的是 `NORMAL`，页面显示的是 `正常`。
