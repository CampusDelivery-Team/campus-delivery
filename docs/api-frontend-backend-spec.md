# 校园跑腿系统接口与前后端分离规范文档

## 1. 文档信息

| 项目 | 内容 |
| --- | --- |
| 项目名称 | 校园中转分发与跑腿服务管理系统 |
| 技术模式 | 前后端分离 |
| 前端 | Vue 3 + Vite |
| 后端 | ASP.NET Core Web API + C# |
| 数据库 | Oracle |
| 接口风格 | RESTful API + JSON |
| 负责人 | 组员1：总集成、架构与数据库负责人 |

## 2. 组员1职责范围

组员1负责项目基础架构和统一规范，主要交付内容包括：

1. 创建和维护后端 Web API 项目骨架。
2. 配置 Oracle 数据库连接。
3. 编写公共数据库访问工具。
4. 建立 Controller、Service、Repository、DTO 等目录规范。
5. 整理并维护 24 张表建表 SQL。
6. 编写基础数据和测试数据。
7. 制定统一接口返回格式。
8. 配置跨域 CORS。
9. 设计统一异常处理方案。
10. 设计统一登录鉴权方案。
11. 负责代码合并和版本整合。
12. 组织前后端联调。

## 3. 前后端分离原则

1. 前端只负责页面、交互、表单校验和接口调用。
2. 后端只负责 API、业务逻辑、权限校验和数据库访问。
3. 前端不得直接连接数据库。
4. 前端不得编写后端业务 SQL。
5. 后端不得依赖具体页面样式。
6. 所有业务接口统一返回 JSON。
7. 前端和后端必须以接口文档为协作依据。
8. 登录后由后端返回用户身份与角色信息，前端根据角色显示菜单。
9. 前端可先用 Mock 数据开发，后端可先用 Postman 或 Apifox 测试。
10. 联调前必须确认接口地址、请求参数、返回字段、状态码和中文枚举值。

## 4. 后端分层规范

后端采用以下分层：

```text
Controller -> Service -> Repository -> Oracle
```

| 层级 | 职责 |
| --- | --- |
| Controller | 接收 HTTP 请求，进行基础参数绑定，返回 JSON |
| Service | 处理业务逻辑、权限判断和状态流转 |
| Repository | 编写 SQL，访问 Oracle 数据库 |
| DTO | 定义请求参数和返回数据结构 |
| Helper/Data | 数据库连接、公共工具和基础能力 |

建议后端目录结构：

```text
backend/src/CampusDelivery.Api/
├── Controllers/
├── Services/
├── Repositories/
├── Models/
├── Dtos/
├── Data/
├── Helpers/
├── Program.cs
└── appsettings.json
```

## 5. 统一接口返回格式

所有业务接口建议统一返回以下结构：

```json
{
  "success": true,
  "message": "操作成功",
  "data": {}
}
```

失败返回：

```json
{
  "success": false,
  "message": "任务状态不允许接单",
  "data": null
}
```

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| success | boolean | 请求是否成功 |
| message | string | 提示信息 |
| data | object/null | 返回数据，失败时可为 null |

## 6. HTTP 状态码规范

| 状态码 | 使用场景 |
| --- | --- |
| 200 | 查询、修改、删除成功 |
| 201 | 新增成功 |
| 400 | 请求参数错误 |
| 401 | 未登录或登录失效 |
| 403 | 无权限访问 |
| 404 | 资源不存在 |
| 409 | 业务状态冲突，例如重复抢单 |
| 500 | 服务器内部错误 |

## 7. 登录鉴权规范

1. 登录接口由后端验证账号和密码。
2. 登录成功后返回用户基本信息、角色和令牌。
3. 前端保存令牌，并在后续请求中通过请求头传递。
4. 后端根据令牌识别当前用户身份。
5. 管理员接口必须校验管理员角色。
6. 跑腿员接口必须校验跑腿员身份和审核状态。

请求头示例：

```text
Authorization: Bearer <token>
```

## 8. 前端接口调用规范

1. 前端接口文件统一放在 `frontend/src/api/`。
2. 接口路径统一使用 `/api/...`。
3. 开发环境可通过 Vite 代理转发到后端。
4. 生产环境建议通过 Nginx 将 `/api` 反向代理到后端。
5. 前端页面不得写死后端服务器 IP。
6. 前端必须处理加载中、成功、失败三种状态。
7. 失败提示优先展示后端返回的 `message`。

前端调用示例：

```ts
const result = await getApi<UserInfo>('/api/users/me')

if (result.ok) {
  user.value = result.data
} else {
  errorMessage.value = result.message
}
```

## 9. 数据库连接规范

数据库连接统一由后端读取配置，不允许在业务代码中写死连接串。

当前开发配置位置：

```text
backend/src/CampusDelivery.Api/appsettings.json
```

本地开发连接串示例：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=APPUSER;Password=App123456;Data Source=localhost:1521/XEPDB1;"
  }
}
```

上线或迁移阿里云时，只修改连接配置，不修改业务代码。

## 10. 基础检查接口

| 接口名称 | 方法 | 地址 | 说明 | 负责人 |
| --- | --- | --- | --- | --- |
| 健康检查 | GET | `/api/health` | 检查后端服务是否运行 | 组员1 |
| 数据库连通检查 | GET | `/api/DbTest/ping` | 检查 Oracle 是否可连接 | 组员1 |

## 11. 业务接口总表

### 11.1 账户、权限、地址、基础资料接口

负责人：组员5

| 方法 | 地址 | 说明 |
| --- | --- | --- |
| POST | `/api/auth/register` | 用户注册 |
| POST | `/api/auth/login` | 用户登录 |
| GET | `/api/users/me` | 当前用户信息 |
| PUT | `/api/users/me` | 修改个人信息 |
| GET | `/api/addresses` | 地址列表 |
| POST | `/api/addresses` | 新增地址 |
| PUT | `/api/addresses/{id}` | 修改地址 |
| DELETE | `/api/addresses/{id}` | 删除地址 |
| PUT | `/api/addresses/{id}/default` | 设置默认地址 |
| GET | `/api/admin/nodes` | 节点列表 |
| POST | `/api/admin/nodes` | 新增节点 |
| PUT | `/api/admin/nodes/{id}` | 修改节点 |
| GET | `/api/admin/service-types` | 服务类型列表 |
| POST | `/api/admin/service-types` | 新增服务类型 |
| GET | `/api/admin/service-node-rules` | 服务节点规则列表 |
| POST | `/api/admin/service-node-rules` | 新增服务节点规则 |

### 11.2 跑腿员接口

负责人：组员6

| 方法 | 地址 | 说明 |
| --- | --- | --- |
| POST | `/api/runners/apply` | 申请成为跑腿员 |
| GET | `/api/runners/me` | 查看自己的跑腿员信息 |
| PUT | `/api/runners/me/status` | 修改工作状态 |
| GET | `/api/admin/runners` | 管理员查看跑腿员列表 |
| GET | `/api/admin/runners/pending` | 待审核跑腿员列表 |
| PUT | `/api/admin/runners/{id}/approve` | 审核通过 |
| PUT | `/api/admin/runners/{id}/reject` | 审核拒绝 |

### 11.3 任务发布接口

负责人：组员7

| 方法 | 地址 | 说明 |
| --- | --- | --- |
| POST | `/api/tasks/food` | 发布外卖分发任务 |
| POST | `/api/tasks/express` | 发布快递代取任务 |
| POST | `/api/tasks/private` | 发布私人任务 |
| GET | `/api/tasks/my` | 我的任务 |
| GET | `/api/tasks/{id}` | 任务详情 |
| PUT | `/api/tasks/{id}/cancel` | 取消任务 |
| GET | `/api/admin/tasks` | 管理员查看全部任务 |

### 11.4 接单派单与状态流转接口

负责人：组员8

| 方法 | 地址 | 说明 |
| --- | --- | --- |
| GET | `/api/runner/tasks/hall` | 任务大厅 |
| POST | `/api/runner/tasks/{id}/accept` | 跑腿员抢单 |
| GET | `/api/runner/assignments` | 我的接单 |
| GET | `/api/runner/assignments/{id}` | 接单详情 |
| PUT | `/api/runner/assignments/{id}/status` | 更新任务状态 |
| PUT | `/api/tasks/{id}/confirm` | 用户确认签收 |
| GET | `/api/tasks/{id}/status-logs` | 状态历史 |
| POST | `/api/admin/tasks/{id}/assign` | 管理员派单 |
| POST | `/api/admin/tasks/{id}/reassign` | 管理员重派 |

任务状态流转：

```text
已支付 -> 待接单 -> 已接单 -> 已取件 -> 配送中 -> 待签收 -> 已完成
```

### 11.5 支付、退款、评价、投诉、结算、报表接口

负责人：组员9

| 方法 | 地址 | 说明 |
| --- | --- | --- |
| POST | `/api/payments` | 模拟支付 |
| GET | `/api/payments/my` | 我的支付记录 |
| POST | `/api/refunds` | 申请退款 |
| GET | `/api/admin/refunds` | 退款审核列表 |
| PUT | `/api/admin/refunds/{id}/approve` | 退款通过 |
| PUT | `/api/admin/refunds/{id}/reject` | 退款拒绝 |
| POST | `/api/reviews` | 用户评价 |
| GET | `/api/reviews/task/{taskId}` | 任务评价 |
| POST | `/api/complaints` | 用户投诉 |
| GET | `/api/admin/complaints` | 投诉列表 |
| PUT | `/api/admin/complaints/{id}/handle` | 处理投诉 |
| POST | `/api/admin/settlements/generate` | 生成结算 |
| GET | `/api/admin/settlements` | 结算列表 |
| GET | `/api/admin/audit-logs` | 审计日志 |
| GET | `/api/admin/reports/summary` | 统计报表 |

## 12. 接口文档模板

每个接口必须按以下格式补充详细文档：

| 项目 | 内容 |
| --- | --- |
| 接口名称 | 用户登录 |
| 请求方式 | POST |
| 请求地址 | `/api/auth/login` |
| 负责人 | 组员5 |
| 联调状态 | 未开始/开发中/已自测/已联调 |

请求参数：

```json
{
  "username": "zhangsan",
  "password": "123456"
}
```

成功返回：

```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "userId": 1,
    "username": "zhangsan",
    "role": "普通用户"
  }
}
```

失败返回：

```json
{
  "success": false,
  "message": "用户名或密码错误",
  "data": null
}
```

## 13. 中文枚举规范

所有业务枚举值统一使用中文，不使用英文缩写。

| 类型 | 中文值 |
| --- | --- |
| 用户角色 | 普通用户、跑腿员、管理员 |
| 账号状态 | 正常、禁用 |
| 是否标志 | 是、否 |
| 节点状态 | 正常、关闭 |
| 服务类型状态 | 启用、禁用 |
| 跑腿员审核状态 | 待审核、已通过、已拒绝 |
| 跑腿员工作状态 | 空闲、忙碌、离线 |
| 任务状态 | 已创建、已支付、待接单、已接单、已取件、配送中、待签收、已完成、已取消、退款中 |
| 接派类型 | 自主接单、管理员派单、重新派单 |
| 支付方式 | 微信、支付宝、现金 |
| 支付状态 | 未支付、已支付、支付失败、已退款 |
| 退款状态 | 待审核、已通过、已拒绝 |
| 投诉状态 | 待处理、已成立、已驳回 |

禁止使用：

```text
USER, RUNNER, ADMIN, NORMAL, DISABLED, CREATED, PAID, WAITING, Y, N
```

## 14. 联调流程

1. 后端先确认接口地址、请求参数和返回格式。
2. 前端根据接口文档编写 API 调用文件。
3. 后端使用 Postman 或 Apifox 完成自测。
4. 前端切换真实接口进行联调。
5. 联调重点检查地址、参数名、返回字段、中文枚举、登录状态和数据库变化。
6. 测试负责人记录 Bug、截图和验收结果。

## 15. 最小演示闭环

项目必须优先跑通以下主流程：

```text
管理员初始化节点、服务类型、规则
普通用户注册登录
普通用户新增地址
普通用户发布任务
普通用户模拟支付
跑腿员申请并通过审核
跑腿员在任务大厅抢单
跑腿员更新状态
普通用户确认签收
普通用户评价
管理员查看报表
```

如果时间不足，最低版本保留：

```text
登录 -> 发布任务 -> 支付 -> 抢单 -> 更新状态 -> 签收 -> 数据库验证
```

## 16. 验收标准

### 16.1 后端接口验收

1. API 可以通过 Postman 或 Apifox 调通。
2. 请求参数校验正确。
3. 业务状态判断正确。
4. 数据库增删改查正确。
5. 返回 JSON 格式统一。
6. 失败时返回友好错误信息。
7. 中文枚举值正确。
8. 提供接口说明和测试截图。

### 16.2 前端调用验收

1. 页面能正常打开。
2. 表单字段完整。
3. 能调用真实后端 API。
4. 加载、成功、失败状态有提示。
5. 角色菜单显示正确。
6. 中文状态显示正确。
7. 不直接访问数据库。
8. 至少提供自己模块的演示截图。

### 16.3 组员1集成验收

1. 后端项目骨架清晰。
2. 数据库连接配置可切换本地和云端。
3. CORS 配置可支持前端开发地址。
4. 接口返回格式统一。
5. 数据库脚本可用于重新初始化环境。
6. 前后端联调流程明确。
7. 最小演示闭环可以跑通。
