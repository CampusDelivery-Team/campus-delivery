# 截图验收 Bug 记录

| Bug ID | 日期 | 环境 | 模块 | 问题 | 严重级别 | 状态 | 证据/备注 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| BUG-20260816-01 | 2026-08-16 | 云端 | 部署 | 云端仍为旧版，`/Auth/Login`、`/Task/Create`、`/Review/MyReviews` 等新版路由返回 404 | 阻断 | 待部署 | 云端不能用于新版答辩演示 |
| BUG-20260816-02 | 2026-08-16 | 本地 + Oracle 隧道 | 数据库连接 | 曾出现 Oracle 通信失败 | 阻断 | 已恢复/待观察 | 2026-08-16 11:43 复核：隧道监听正常、`--dry-run` 成功、数据库状态页显示已连接；原截图实际为管理员首页，已更名为 `ENV-06-admin-home.png`，不能作为错误证据 |
| BUG-20260816-03 | 2026-08-16 | 本地 `2917db9` + Oracle | 评价管理 | 管理员访问 `/Review/All` 进入统一错误页，无法查看评价列表 | 高 | 已定位/待迁移 | `screenshots/BUG-20260816-03-review-all-error.png`、`sql/BUG-03-review-schema.txt`；APPUSER `reviews` 只有旧版 7 列，缺少代码所需 `task_id`，仍使用 `FK_REVIEWS_RECORD`/`UK_REVIEWS_RECORD` |
| BUG-20260816-04 | 2026-08-16 | 本地 `2917db9` + Oracle | 审计界面 | `/Audit/Create?auditObject=PAYMENT` 标题显示“登记@Model.AuditObjectDisplayName审计”，Razor 表达式未被正确解析 | 中 | 已修复/待页面回归 | `screenshots/FLOW-19B-audit-payment-candidates.png` 保留修复前证据；现已使用显式表达式边界 `@(Model.AuditObjectDisplayName)` 修复，待新版部署后补充回归截图 |
| BUG-20260816-05 | 2026-08-16 | APPUSER 共享 Oracle | 数据库迁移 | 新版完整性迁移未落库：评价 task_id/复合外键、单默认地址索引、服务名大小写唯一索引均不存在 | 阻断 | 待数据库负责人执行 | `sql/ENV-07-migration-status.txt`；在迁移前不应继续评价、地址并发等最终验收，也不能把 `PENDING-DB` 改为 PASS |
