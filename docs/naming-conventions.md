# 命名规范

## 后端类

| 类型 | 命名 |
| --- | --- |
| Controller | `XxxController.cs` |
| Service | `XxxService.cs` |
| Repository | `XxxRepository.cs` |
| Model | `Xxx.cs` |
| 创建表单 ViewModel | `XxxCreateViewModel.cs` |
| 编辑表单 ViewModel | `XxxEditViewModel.cs` |
| 列表页 ViewModel | `XxxIndexViewModel.cs` |
| 列表项 ViewModel | `XxxListItemViewModel.cs` |

## Razor 页面

```text
Presentation/Views/Xxx/Index.cshtml
Presentation/Views/Xxx/Create.cshtml
Presentation/Views/Xxx/Edit.cshtml
```

共享布局：

```text
Presentation/Views/Shared/_Layout.cshtml
```

首页：

```text
Presentation/Views/Home/Index.cshtml
```

## 静态资源

```text
Presentation/wwwroot/css/site.css
```

## 数据库脚本

当前建表脚本：

```text
database/oracle/campus_runner_oracle_schema.sql
```

当前已提供基础数据脚本：

```text
database/oracle/002_init_base_data.sql
```

后续如需完整业务演示数据，命名为：

```text
database/oracle/003_init_test_data.sql
```

## 中文显示字段

数据库英文代码对应的中文显示字段，建议命名为：

```text
XxxDisplayName
```

例如：

```text
NodeType
NodeTypeDisplayName
NodeStatus
NodeStatusDisplayName
```
