### TC006 封禁或注销账号登录拦截

- 执行人：组员3
- 执行时间：2026-08-26
- 环境：本地 MVC 应用 + 远程共享 Oracle (APPUSER)
- 前置数据：在管理员后台将某一测试账号状态设置为“封控”(BLOCKED)。
- 页面结果：使用被封控的账号尝试登录系统，系统拒绝签发通行证，返回登录页并显示“您的账号已被封控”的友好提示。
- SQL 核验摘要：查询 users 表确认目标账号的 account_status 字段已正确更新为 BLOCKED，底层状态机与前端拦截逻辑完全一致。
- 证据文件：
  - 前端拦截截图：`screenshots/TC006-login-blocked.png`
  - 数据库查询截图：`SQL/TC006-account-status-query.png`
- 结论：PASS