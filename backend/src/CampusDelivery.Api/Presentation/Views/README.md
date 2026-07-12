# Razor Views

本目录存放 MVC Razor 页面。

```text
Home/Index.cshtml                 # 角色感知的首页门户
Home/AccessDenied.cshtml           # 无权限页
Home/Error.cshtml                  # 错误页
Auth/Login.cshtml / Register.cshtml
User/Profile.cshtml / Edit.cshtml
Node/Index.cshtml                  # 同页新增、编辑、关闭/恢复
ServiceType/Index.cshtml           # 同页新增、编辑、启用/停用
ServiceNodeRule/Index.cshtml       # 同页绑定与受限解除
Runner/Apply.cshtml                # 跑腿员资格申请
Runner/Index.cshtml / Pending.cshtml
Database/Status.cshtml
Shared/_Layout.cshtml              # 共享布局和管理导航
```

`Node` 和 `ServiceType` 没有独立 `Create.cshtml`、`Edit.cshtml`。页面显示中文名称；数据库英文代码由 Service/ViewModel 预先转换，View 只负责展示和收集输入。
