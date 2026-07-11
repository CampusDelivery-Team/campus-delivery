# Models

本目录存放与数据库字段语义对应的数据/领域模型。

```text
Node.cs             # 节点
ServiceType.cs      # 服务类型
ServiceNodeRule.cs  # 服务类型与节点绑定
Runner.cs           # 跑腿员资格资料
User.cs             # 系统用户
UserAddress.cs      # 用户收货地址
```

模型中的枚举和状态保持数据库英文代码。页面表单模型和中文展示字段放在 `Presentation/ViewModels`，不要让 Model 承担 Razor 页面职责。
