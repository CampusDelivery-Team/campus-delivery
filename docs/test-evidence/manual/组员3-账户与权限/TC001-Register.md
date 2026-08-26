### TC001 正常注册流程与哈希校验

- 执行人：组员3
- 执行时间：2026-08-26
- 环境：本地 MVC 应用 + 远程共享 Oracle (APPUSER)
- 前置数据：无
- 测试账号：htan
- 页面结果：输入合法信息后注册成功，并自动跳转至带有“账号注册成功，请登录！”提示的登录页。
- SQL 核验摘要：成功写入 users 表，password_hash 字段显示为哈希密文，user_role 正确赋为 USER，account_status 为 NORMAL。
- 证据文件：
  - 前端表单截图：`screenshots/TC001-register-form.png`
  - 前端成功截图：`screenshots/TC001-register-success.png`
  - 数据库截图：`SQL/TC001-users-query.png`
- 结论：PASS