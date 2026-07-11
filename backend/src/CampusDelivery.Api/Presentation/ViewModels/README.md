# ViewModels

本目录存放页面输入和展示模型，不直接承担数据库实体职责。

```text
LoginViewModel.cs / RegisterViewModel.cs / UserViewModel.cs
DatabaseStatusViewModel.cs
NodeCreateViewModel.cs / NodeEditViewModel.cs / NodeIndexViewModel.cs / NodeListItemViewModel.cs
ServiceTypeCreateViewModel.cs / ServiceTypeEditViewModel.cs / ServiceTypeIndexViewModel.cs / ServiceTypeListItemViewModel.cs
ServiceNodeRuleCreateViewModel.cs / ServiceNodeRuleIndexViewModel.cs
ServiceNodeRuleListItemViewModel.cs / ServiceNodeRuleOptionViewModel.cs
RunnerApplicationFormViewModel.cs / RunnerApplicationPageViewModel.cs
RunnerIndexViewModel.cs / RunnerListItemViewModel.cs
```

数据库英文代码对应的中文文本应放在展示型字段中，例如 `NodeStatusDisplayName`、`TypeStatusDisplayName`、`AuditStatusDisplayName` 和 `WorkStatusDisplayName`。
