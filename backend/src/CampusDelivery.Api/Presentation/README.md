# 表现层 Presentation

表现层负责页面展示、表单输入、静态资源和展示用数据模型。

本层应该存放：

- `Views/`：Razor 页面，例如 `Views/Task/Create.cshtml`、`Views/Auth/Login.cshtml`。
- `ViewModels/`：页面表单和展示模型，例如 `LoginViewModel`、`TaskCreateViewModel`。
- `wwwroot/`：页面 CSS、图片、前端脚本等静态资源。

本层不应该：

- 直接访问数据库。
- 编写复杂业务判断。
- 写 SQL。
- 直接修改任务状态、支付状态、审核状态等核心业务状态。

调用方向：

```text
表现层 -> 控制层
```
