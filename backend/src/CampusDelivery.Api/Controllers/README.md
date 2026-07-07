# 控制层

Controller 负责接收 MVC 请求、绑定 ViewModel、调用 Service，并返回 View、Redirect 或少量 JSON。

不要在 Controller 中直接写复杂 SQL 或直接访问 Oracle。
