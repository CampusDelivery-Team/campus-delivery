### TC010-TC011 收货地址新增与默认地址切换

- 执行人：组员3
- 执行时间：2026-08-26
- 环境：本地 MVC 应用 + 远程共享 Oracle (APPUSER)
- 前置数据：以普通用户 (USER) 身份登录（账号：htan）
- 页面结果：成功新增两条不同的收货地址；设置其中一条为默认地址时，系统提示成功，且列表中仅有一条地址高亮“默认地址”标签。
- SQL 核验摘要：查询 user_addresses 表确认成功写入两条记录，它们归属于同一个 user_id，且系统为其分配了互不冲突的 address_no。
- 证据文件：
  - 前端地址列表截图：`screenshots/TC010-address-list.png`
  - 数据库查询截图：`SQL/TC010-address-query.png`
- 结论：PASS