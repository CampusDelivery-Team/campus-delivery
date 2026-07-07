# 命名规范

## Controller

```text
XxxController.cs
```

## Service

```text
XxxService.cs
```

## Repository

```text
XxxRepository.cs
```

## Model

```text
Xxx.cs
```

## ViewModel

```text
XxxCreateViewModel.cs
XxxEditViewModel.cs
XxxIndexViewModel.cs
```

## Razor View

```text
Presentation/Views/Xxx/Index.cshtml
Presentation/Views/Xxx/Create.cshtml
Presentation/Views/Xxx/Edit.cshtml
```

## 数据库脚本

```text
database/oracle/campus_runner_oracle_schema.sql
```

如果后续新增初始化数据脚本，命名为：

```text
database/oracle/002_init_base_data.sql
database/oracle/003_init_test_data.sql
```

当前仓库尚未提供这两个文件。

## 显示名称字段

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
