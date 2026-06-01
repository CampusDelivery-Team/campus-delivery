# 命名规范

## 仓库和目录

- GitHub 仓库名建议使用 `campus-delivery`。
- 不建议使用 `delivery-backend` 或 `backend`，因为当前仓库已经同时包含后端、前端、数据库脚本和文档。
- 根目录只放跨端公共文件，例如 `README.md`、`.editorconfig`、`.gitignore`、`database/`、`docs/`、`scripts/`。

## 后端

- Solution：`CampusDelivery.sln`
- API 项目：`CampusDelivery.Api`
- 命名空间：`CampusDelivery.Api`
- Controller：使用 `PascalCase`，并以 `Controller` 结尾，例如 `HealthController`。
- DTO：使用 `PascalCase`，并以用途结尾，例如 `HealthResponse`。
- 服务类：使用明确职责名，例如 `OracleConnectionFactory`。

## 前端

- npm package：`campus-delivery-web`
- Vue 组件：`PascalCase`，例如 `TaskList.vue`。
- TypeScript 文件：业务模块使用 `camelCase` 或功能名，例如 `http.ts`、`taskApi.ts`。
- API 请求统一放在 `frontend/src/api/`。

## 数据库

- SQL 文件使用编号前缀，保证执行顺序清晰：
  - `001_schema.sql`
  - `002_seed.sql`
- 表名沿用当前 Oracle 脚本的 `snake_case` 复数形式，例如 `users`、`task_status_logs`。
- 约束名使用类型前缀，例如 `pk_`、`fk_`、`uk_`、`ck_`。
