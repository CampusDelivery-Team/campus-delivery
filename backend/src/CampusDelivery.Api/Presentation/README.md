# 表现层

表现层包含 Razor View、ViewModel 和静态资源。

```text
Views/       # Razor 页面
ViewModels/  # 页面表单和展示模型
wwwroot/     # CSS 等静态资源
```

首页 `/` 是角色感知的真实项目门户，使用蓝粉色校园跑腿视觉。它只展示当前已有 Controller 的入口；未接入的任务、订单和配送能力必须显示为不可用状态，不能使用伪链接。
