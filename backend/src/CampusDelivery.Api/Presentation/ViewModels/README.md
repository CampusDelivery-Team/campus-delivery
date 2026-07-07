# ViewModels

本目录存放页面输入和展示模型。

当前 ViewModel：

```text
DatabaseStatusViewModel.cs
NodeCreateViewModel.cs
NodeEditViewModel.cs
NodeIndexViewModel.cs
NodeListItemViewModel.cs
```

ViewModel 只服务于页面，不直接承担数据库实体职责。

数据库英文代码对应的中文显示文本，应放在展示型 ViewModel 字段中，例如：

```text
NodeStatus
NodeStatusDisplayName
```
