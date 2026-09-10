# 1 系统需求

校园综合跑腿与代取服务管理系统面向校内外卖分发、快递代取和私人跑腿场景，将用户发布、跑腿员接派、配送履约、确认支付、售后评价、跑腿员结算以及运营审计串联为可追踪的业务闭环。系统当前版本以能够运行、能够验证和能够追责为主要目标，采用送达后付款口径，避免在真实接派服务产生前创建支付记录。

## 1.1 系统功能性需求

系统服务对象包括访客、普通用户、跑腿员和管理员。访客可浏览门户、服务类型并完成注册登录；普通用户维护地址、发布三类任务、确认收货和支付，并可评价、投诉或申请退款；跑腿员申请资格、在任务大厅接单、推进配送状态并查看结算；管理员维护基础资料、审核资格、派单重派、处理退款投诉、生成结算单、执行审计和导出报表。

| 角色 | 主要需求 | 访问边界 |
| 访客 | 浏览门户、查看服务说明、注册和登录 | 不得访问任务、地址及后台数据 |
| 普通用户 | 地址管理、三类任务发布、取消、收货付款、评价投诉退款 | 只能操作本人发布的任务和本人地址 |
| 跑腿员 | 资格申请、任务大厅抢单、配送状态流转、查看本人结算 | 必须通过审核，只能操作本人当前接派记录 |
| 管理员 | 配置、审核、派单、售后、结算、审计和报表 | 管理路由要求 ADMIN 角色并记录业务结果 |

主业务流程为：注册登录后维护地址，用户发布任务并进入 WAITING；跑腿员抢单或管理员派单后进入 ASSIGNED；跑腿员按 PICKED_UP、DELIVERING、WAIT_CONFIRM 顺序推进状态；发布者确认收货并完成付款后任务进入 FINISHED；随后系统围绕最终有效接派记录处理评价、投诉、退款、结算、审计和报表。

> USER -> WAITING -> ASSIGNED -> PICKED_UP -> DELIVERING
>      -> WAIT_CONFIRM -> PAID + FINISHED -> REVIEW / COMPLAINT
>      -> REFUND / SETTLEMENT -> AUDIT -> REPORT

## 1.2 系统非功能性需求

| 类别 | 设计要求 | 实现依据 |
| 安全性 | 密码不可明文存储，管理路由鉴权，写操作防 CSRF，对象归属校验 | PasswordHasher、Cookie 身份校验、Authorize、ValidateAntiForgeryToken |
| 一致性 | 跨表写入同一事务，竞争写操作加锁，状态只允许合法迁移 | Service 事务协调、Repository 参数化 SQL 与 FOR UPDATE |
| 可维护性 | 分层清晰、接口隔离、中文展示与英文状态码分离 | MVC 五层、依赖注入、DisplayNameService 与 ViewModel |
| 可用性 | 主要页面中文化，按角色给出真实入口和明确操作反馈 | Razor 页面、统一导航、ModelState 校验和友好错误页 |
| 可部署性 | 连接配置外置，支持本地与服务器环境，真实凭据不入库 | appsettings.Local.json 或环境变量覆盖 |
| 可验证性 | 核心规则可自动回归，真实 Oracle 链路可手工复核 | xUnit 测试替身、静态门禁、共享库测试证据 |

## 1.3 文档组织结构

第一章说明功能和质量需求；第二章给出技术选型、五层架构、模块边界和安全设计；第三章按照主要业务模块分别描述设计与实现；第四章说明 Oracle 数据库的实体关系、表结构分组及关键约束；第五章给出测试、运行和部署方式；第六章总结当前成果与后续改进方向；附录 A 汇总文档中的图表索引。

# 2 系统总体设计

## 2.1 技术选型与运行结构

系统后端与页面统一构建在 ASP.NET Core MVC 上，目标框架为 net9.0。表现层使用 Razor Views 和自定义 CSS，不再依赖独立 Vue 或 Vite 前端；数据访问通过 Oracle.ManagedDataAccess.Core 23.8.0 连接 Oracle 19c。应用在本地默认监听 5227 端口，公网环境由 HTTPS 443 的反向代理转发到内部应用。

| 组成 | 技术或版本 | 用途 |
| 应用框架 | .NET 9 与 ASP.NET Core MVC | 控制器、依赖注入、Cookie 认证、Razor 页面 |
| 数据访问 | Oracle.ManagedDataAccess.Core 23.8.0 | 参数化 SQL、事务、行锁和数据映射 |
| 数据库 | Oracle 19c 和 orclpdb1 | 24 张关系表、序列、约束、索引和迁移 |
| 测试 | xUnit 2.9.2 与 Microsoft.NET.Test.Sdk 17.12.0 | Service 业务规则、事务和并发回归 |
| 部署 | Windows PowerShell、HTTPS 反向代理 | 本地运行、SSH 数据库隧道和公网演示 |

[[IMAGE|docs/test-evidence/manual/2026-08-16/screenshots/ENV-01-home.png|图 2-1 系统门户与访客入口|6.3]]

## 2.2 MVC 五层架构

项目按照表现层、控制层、业务层、持久层和数据库层划分职责。Controller 只负责 HTTP 协调并调用 Service 接口；Service 负责权限、归属、状态机和事务；Repository 负责 SQL、参数绑定、行锁与映射；OracleConnectionFactory 是创建连接的统一入口。该调用方向由静态门禁持续检查。

| 层级 | 主要目录 | 核心职责 |
| 表现层 | Presentation/Views、ViewModels、wwwroot | 展示页面、收集输入、模型验证和中文显示 |
| 控制层 | Controllers | 接收请求、读取 Claims、调用业务服务并返回页面或重定向 |
| 业务层 | Services、Services/Interfaces | 权限、对象归属、状态机、事务和显示名称转换 |
| 持久层 | Repositories、Persistence/Oracle | 参数化 SQL、连接事务、FOR UPDATE 行锁和映射 |
| 数据库层 | database/oracle | 表、序列、约束、索引、基础数据和迁移脚本 |

> Razor View -> Controller -> IService -> Service
>            -> IRepository -> Repository -> OracleConnectionFactory -> Oracle

## 2.3 模块划分与依赖

系统以业务闭环划分为账户地址、基础资料与资格、任务发布、接派配送、支付退款、评价投诉、结算审计报表七组模块。各模块通过稳定的数据主键和服务接口协作，接派记录 assign_records 是履约核心：支付、评价、投诉和结算都围绕最终有效接派记录展开。

| 模块 | Controller 和 Service | 核心数据 |
| 账户与地址 | Auth、User、Account、Address | users、user_addresses |
| 基础资料与资格 | Node、ServiceType、ServiceNodeRule、Runner | nodes、service_types、service_node_rules、runners |
| 任务发布 | Task 和 TaskService | tasks 与三类任务明细 |
| 接派配送 | Task、Assign 和 AssignService | assign_records、task_status_logs |
| 支付退款 | Payment、Refund | payments、refunds |
| 评价投诉 | Review、Complaint | reviews、complaints |
| 结算审计报表 | Settlement、Audit、Report | settlements、audit_logs、reports 及关联表 |

## 2.4 权限与安全设计

系统使用 Cookie 认证保存用户编号、用户名和角色。每个已认证请求都会重新读取账号状态和角色；账号被封禁或注销后，旧 Cookie 在下一次请求即被拒绝。管理员页面使用角色授权，用户和跑腿员操作还要在 Service 中检查资源归属。所有修改状态的 POST Action 均添加防伪校验，异常统一进入安全错误页，避免向浏览器泄露 SQL 和调用栈。

- 密码由 ASP.NET Core PasswordHasher 生成带盐哈希，登录时使用 VerifyHashedPassword 校验。
- Repository 的 SQL 使用参数绑定，控制器和服务层不创建 OracleCommand。
- 抢单、地址编号、支付、退款、评价、结算等高风险写操作在事务内锁定业务行。
- 真实连接串、数据库密码和 SSH 私钥均通过本地文件或环境变量提供，不进入版本控制。

# 3 系统设计与实现

本章围绕已落地的七组业务模块说明交互流程、关键规则、实现分层及可验证结果。每组实现均遵守 Controller 调用 Service 接口、Service 组织业务规则、Repository 完成 Oracle 读写的统一约定。

## 3.1 账户权限与地址管理

### 3.1.1 功能设计

账户模块承担注册、登录、个人资料、账号封禁与注销。地址模块为任务发布提供属于当前用户的收货地址，并保证同一用户最多只有一个默认地址。服务层在新增地址前锁定用户行后分配 address_no，默认地址切换与删除后的默认补位在同一事务内完成。

| 动作序列 | 设计说明 |
| 注册 | 校验用户名、手机号和密码，检查唯一性，生成密码哈希，创建 NORMAL 用户 |
| 登录 | 查询账号，拒绝 BLOCKED 或 CANCELLED，验证哈希并签发 Cookie |
| 维护地址 | 只读取本人地址，新增时分配复合主键，编辑和删除前校验归属 |
| 设置默认 | 事务内锁定用户，先校验目标存在，再清除旧默认并设置新默认 |
| 管理账号 | 管理员封禁、解封、注销和恢复，认证事件实时使旧会话失效 |

### 3.1.2 关键实现

Program.cs 注册 IUserService、IAddressService 和密码哈希服务，并在 Cookie 的 OnValidatePrincipal 事件中按用户编号重新读取认证状态。AddressService 负责归属和事务逻辑，AddressRepository 使用复合键 user_id、address_no 访问数据。前端 ModelState 处理必填项和格式错误，业务错误回显在原表单。

- 用户名和手机号均执行唯一性检查，密码字段只向数据库写入哈希。
- 跨用户提交地址编号时返回拒绝结果，不泄露其他用户的地址内容。
- 数据库函数索引与服务事务共同保证单默认地址约束。
- 自动化和共享库测试覆盖正常注册、重复注册、错误密码、封禁旧会话、地址并发与跨用户操作。

## 3.2 基础资料与跑腿员资格

### 3.2.1 功能设计

节点、服务类型和服务节点规则共同限制任务可发布范围。管理员通过状态关闭保留历史引用，不直接删除已进入业务链路的数据。普通用户可以提交跑腿员申请；管理员审核通过后同步更新 runners.audit_status 和 users.user_role，使其获得任务大厅与配送工作台权限。

| 对象 | 状态或约束 | 设计目的 |
| 节点 | NORMAL 或 CLOSED，使用中禁止删除 | 保留历史任务中的交接节点 |
| 服务类型 | ENABLED 或 DISABLED，基础价格不得为负 | 统一服务口径和最低价格 |
| 适用规则 | service_type_id 与 node_id 复合主键 | 防止在不适用节点发布服务 |
| 跑腿员审核 | PENDING、APPROVED、REJECTED | 由管理员决定资格并同步角色 |
| 跑腿员工作 | FREE、BUSY、OFFLINE | 约束是否允许接单以及配送占用 |

### 3.2.2 关键实现

NodeService、ServiceTypeService 与 ServiceNodeRuleService 分别封装状态切换、引用保护和绑定规则；RunnerService 负责申请、审核、重新提交及角色同步。页面只提交英文状态代码，中文名称由显示服务或 ViewModel 提供，从而避免在多个 Razor 页面重复判断。

## 3.3 三类任务发布

### 3.3.1 功能设计

任务采用主表加专属明细的垂直拆分。公共字段写入 tasks，外卖、快递和私人跑腿的特有字段分别写入 food_delivery_details、express_pickup_details 和 private_task_details。一次发布只能产生一种明细；地址必须属于发布者，节点必须开放，服务类型必须启用，服务与节点必须存在绑定，任务价格不得低于基础价。

| 任务类型 | 专属字段 | 保存表 |
| 外卖分发 | 商家名称、平台订单号、取餐备注 | food_delivery_details |
| 快递代取 | 快递公司、运单号、取件码、备注 | express_pickup_details |
| 私人跑腿 | 物品类别、取送地点、期望完成时间、描述 | private_task_details |

### 3.3.2 关键实现

TaskController 使用具体的创建 ViewModel 接收表单，TaskService 统一规范化字符串、校验业务关联和价格，再由 TaskRepository 在同一事务中插入主表及对应明细。发布成功后状态直接为 WAITING，不提前创建接派记录、支付记录或配送日志。

[[IMAGE|docs/test-evidence/manual/组员8-评价、投诉与信誉/screenshots/01-1.png|图 3-1 外卖分发任务发布成功页面|6.2]]

## 3.4 接单派单与配送状态

### 3.4.1 功能设计

待接单任务可以由已审核且处于可接单状态的跑腿员抢单，也可以由管理员派单。接派成功后系统新增 assign_records，将任务更新为 ASSIGNED，并写入接派后的状态日志。重派不删除旧记录，而是新增 operation_type 为 REASSIGN 的记录，后续业务按 assigned_at、record_id 倒序取最终有效记录。

| 当前状态 | 允许操作 | 目标状态与副作用 |
| WAITING | 跑腿员抢单或管理员派单 | ASSIGNED，新增接派并写日志 |
| ASSIGNED | 确认取货 | PICKED_UP，写状态日志 |
| PICKED_UP | 开始配送 | DELIVERING，写状态日志 |
| DELIVERING | 确认送达 | WAIT_CONFIRM，写状态日志 |
| WAIT_CONFIRM | 发布者确认并支付 | FINISHED，生成或更新支付 |
| WAITING | 发布者取消 | CANCELLED，未接单时不写配送日志 |

### 3.4.2 并发与实现

AssignService 在事务内先锁定任务行与跑腿员行，重新检查任务仍为 WAITING、资格已通过且工作状态允许，再写入接派记录、任务状态与日志。两个跑腿员并发请求同一任务时，后获得锁的请求会读到已变化状态并回滚，因此数据库中只形成一条有效抢单结果。

[[IMAGE|docs/test-evidence/manual/组员6-接单派单与状态流转/组员6-测试结果记录.assets/TC-ASSIGN-01-任务大厅待接单列表.png|图 3-2 跑腿员任务大厅与接单入口|6.25]]

## 3.5 支付与退款

### 3.5.1 功能设计

系统采用送达后付款。任务进入 WAIT_CONFIRM 后，只有发布者可以确认收货；选择现金、微信或支付宝完成支付后，payments.pay_status 更新为 PAID，任务才进入 FINISHED。若选择稍后付款，任务保持待确认或待付款语义，不允许提前评价或结算。退款必须基于已有支付记录，由用户申请、管理员审核，并与任务、支付及争议状态联动。

| 业务动作 | 前置条件 | 结果 |
| 确认支付 | 最终接派存在，任务 WAIT_CONFIRM，当前用户为发布者 | 支付 PAID、任务 FINISHED，重复请求幂等 |
| 申请退款 | 支付 PAID，本人任务，未进入禁止退款的已结算边界 | 新增 APPLY 退款并阻断普通结算 |
| 同意退款 | 管理员，退款 APPLY，金额合法 | 退款 APPROVED 或 DONE，支付可转 REFUNDED |
| 拒绝退款 | 管理员，退款 APPLY | 退款 REJECTED，支付维持 PAID |

### 3.5.2 关键实现

PaymentService 和 RefundService 负责归属、状态、金额与重复提交校验；Repository 在事务内锁定支付或退款记录。线上付款可保存第三方流水号，现金付款允许为空。异常状态、重复退款和不存在的业务编号都转换为明确业务结果，避免直接暴露 Oracle 异常。

[[IMAGE|docs/test-evidence/manual/2026-08-16/screenshots/FLOW-17-refund.png|图 3-3 管理员退款处理列表|6.3]]

## 3.6 评价投诉与信誉

### 3.6.1 功能设计

评价和投诉面向一次真实接派服务，而不是只绑定任务主单。评价要求任务 FINISHED、支付 PAID 且不存在活动退款，同一任务最多评价一次。评分 1 至 5 对应信誉变化 -2、-1、0、+1、+2，最终信誉分限制在 0 至 100。投诉由发布者提交，管理员处理；投诉成立时在同一业务操作中完成状态更新和信誉扣减。

| 规则 | 约束 | 一致性措施 |
| 评价资格 | 本人任务、已完成、已付款、无活动退款 | 服务层在事务内重新读取最终接派与支付 |
| 评价唯一 | 一项任务一次评价 | 服务检查与数据库唯一约束双重保护 |
| 信誉变动 | 评分映射，结果限制 0 至 100 | 保存实际 credit_delta，新增编辑删除均可回算 |
| 投诉处理 | 本人提交、管理员处理、不得重复结案 | 成立时一次性扣分，重复请求不重复扣减 |

### 3.6.2 关键实现

ReviewService 和 ComplaintService 不接收客户端指定的 runner_id，而是根据任务的最终有效接派记录确定服务对象。列表页面通过 ViewModel 显示中文状态和匿名信息；管理员可查看全部评价与待处理投诉，普通用户只能查看本人记录。

## 3.7 结算审计与报表

### 3.7.1 功能设计

结算候选只包含属于目标跑腿员、已付款、已完成、未退款、无活动投诉且未被其他结算单引用的支付记录。系统按订单金额汇总并计算 10% 平台费，主表和结算支付明细同事务写入。审计支持状态日志、支付和退款三类对象；报表按月生成订单、支付和投诉指标，可关联审计依据并导出 UTF-8 CSV。

| 子模块 | 关键状态 | 实现要点 |
| 结算 | WAITING、DONE、BLOCKED | 支付只结算一次，校验跑腿员归属，DONE 为终态 |
| 审计 | PASS、ABNORMAL | 主审计表和不同对象明细同事务写入，目标编号去重 |
| 报表 | GENERATED、EXPORTED | 按 yyyy-MM 读取真实业务，保存记录，导出后更新状态 |

### 3.7.2 关键实现

SettlementService 通过候选查询和事务锁定防止重复纳入；AuditService 按审计对象选择对应明细表；ReportService 查询周期数据、计算统计指标并持久化报表与审计关联。删除报表只删除报表及其关联，不删除审计历史。

[[IMAGE|docs/test-evidence/manual/2026-08-16/screenshots/FLOW-20-report.png|图 3-4 管理员统计面板与报表生成|6.3]]

# 4 数据库设计

## 4.1 数据模型原则

数据库使用 Oracle 19c，共包含 24 张关系表。模型以第三范式为基础：用户地址采用弱实体复合键；服务类型与节点通过关系表表达多对多适用范围；任务公共字段与三类专属字段垂直拆分；支付、退款、评价、投诉、结算、审计与报表分别保存自身事实，并通过外键关联。

[[IMAGE|.codex-doc-work/dbdoc-unpacked/word/media/image1.png|图 4-1 系统总体 E-R 图|6.35]]

## 4.2 关系结构

users、tasks、assign_records 和 payments 构成主业务骨架。用户通过复合外键选择自己的地址；任务通过 service_type_id 与 node_id 的复合外键约束适用规则；任务明细以 task_id 依赖任务主单；支付、评价和投诉绑定接派记录；结算支付明细保证同一 payment_id 只能进入一个结算；审计和报表通过专用关联表连接。

[[IMAGE|.codex-doc-work/dbdoc-unpacked/word/media/image10.png|图 4-2 数据库关系图|6.15]]

## 4.3 数据表分组

| 分组 | 数据表 | 用途 |
| 账户基础 | users、user_addresses、runners | 账号、地址、跑腿资格和信誉 |
| 服务配置 | nodes、service_types、service_node_rules | 节点、价格规则和适用范围 |
| 任务履约 | tasks、三类 details、assign_records、task_status_logs | 任务发布、专属字段、接派和状态日志 |
| 支付售后 | payments、refunds、reviews、complaints | 收款、退款、评价和投诉 |
| 运营管理 | settlements、settlement_payment_items、audit_logs、三类 audit checks、reports、report_audit_items | 结算、审计和统计报表 |

## 4.4 关键约束与索引

- users.username 与 phone 唯一，password_hash 非空，角色和账号状态使用 CHECK 约束限制英文代码。
- user_addresses 以 user_id、address_no 为复合主键，并通过函数唯一索引保证每个用户至多一个默认地址。
- tasks 对发布者地址和服务节点规则使用复合外键，避免跨用户地址和无效服务节点组合。
- 三类明细分别依赖 tasks，一次业务发布由服务层保证只写一种明细。
- reviews 对任务或接派关系实施唯一性与完整性约束，信誉分与任务价格在迁移中增加边界保护。
- settlement_payment_items 对 payment_id 保持唯一，确保同一支付不可重复结算。

## 4.5 事务与锁策略

应用在需要跨表一致性或存在并发竞争的操作中显式开启事务。抢单锁定任务和跑腿员；地址新增锁定用户；支付与退款锁定接派或支付；评价锁定业务对象并同步信誉；结算锁定结算单和候选支付。当前仓储层可静态检出 15 处 FOR UPDATE 语句，形成了从服务规则到数据库并发控制的双层保障。

# 5 测试运行与部署

## 5.1 测试策略与结果

项目使用小型高价值测试工程验证状态机、权限、事务、幂等和并发，不以简单 CRUD 用例数量作为目标。Service 测试通过仓储接口替身隔离 Oracle；静态门禁检查数据库规模、防伪属性、分层边界、行锁和配置外置；共享 Oracle 的手工端到端测试用于证明真实页面、事务和最终数据。

| 验证项 | 当前结果 | 说明 |
| Release 自动化测试 | 58/58 通过 | 覆盖任务、接单、支付退款、评价、地址、结算和报表等核心服务 |
| 数据库表 | 24 张 | 满足课程设计规模并与数据库字典一致 |
| POST 防伪 | 47/47 | 所有修改状态的 MVC POST Action 均校验令牌 |
| 分层边界 | 通过 | Controller 只用 Service，Service 不包含 Oracle 命令 |
| 行锁 | 15 处 | Repository 中存在真实 FOR UPDATE 竞争控制 |
| 真实数据库验收 | 通过 | 主链路、权限、并发、售后、结算审计和报表已留存证据 |

[[IMAGE|docs/test-evidence/manual/组员1-总集成与架构/screenshots/TC-INT-05-自动化门禁52项通过.png|图 5-1 阶段性自动化门禁执行记录|6.15]]

## 5.2 本地运行

开发机安装 .NET SDK 9 后，可在仓库根目录使用统一脚本启动，也可以直接运行应用项目。开发环境从 appsettings.Local.json 或环境变量读取 OracleDb 连接串。共享数据库通过 SSH 隧道映射到 127.0.0.1:15210/orclpdb1，真实账号和密钥由负责人单独提供。

> .\scripts\run-backend.ps1
> dotnet run --project backend\src\CampusDelivery.Api\CampusDelivery.Api.csproj
> powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-tests.ps1

[[IMAGE|docs/test-evidence/manual/组员1-总集成与架构/screenshots/TC-INT-04-数据库连接正常.png|图 5-2 Oracle 数据库连接状态页面|6.25]]

## 5.3 部署结构

公网访问入口为 https://47.116.60.57/。外部请求通过 HTTPS 443 到达反向代理，再转发到 ASP.NET Core 内部端口；Oracle 不直接开放公网端口。部署时需要记录分支和提交号，配置连接串环境变量，并分别检查首页、登录重定向、数据库状态和关键业务路由。

| 环节 | 地址或配置 | 验证方式 |
| 公网入口 | https://47.116.60.57/ | 首页返回 200，受保护页面正常重定向登录 |
| 本地应用 | http://localhost:5227 | 运行脚本后访问首页与 Database/Status |
| Oracle 隧道 | 127.0.0.1:15210/orclpdb1 | Test-NetConnection 与数据库状态页 |
| 配置 | ConnectionStrings:OracleDb | 本地文件或环境变量覆盖，仓库无真实凭据 |

## 5.4 已知边界

当前验收重点是核心业务闭环、权限、并发和数据一致性，尚未建设持续集成工作流、独立可重建测试库、浏览器兼容矩阵和压力容量测试。自动距离计价、完成凭证上传、超时自动签收和第三方取件码核验需要地图、文件存储、定时任务或外部服务，保留为后续扩展，不作为本轮已实现能力。

# 6 总结

本系统已经完成从账户与配置、三类任务发布、接派配送、送达后付款，到退款评价投诉、跑腿员结算、审计与报表的完整实现。五层架构明确了页面、控制、业务、持久化和数据库的职责；Oracle 关系模型以真实接派记录为业务核心；事务、行锁、状态机、对象归属与数据库约束共同保护数据一致性。

测试结果表明当前版本能够在 .NET 9 与 Oracle 19c 环境中稳定构建和运行，58 项自动化测试、47 个 POST 防伪检查、24 表结构检查、15 处行锁检查和分层门禁均通过。结合共享数据库端到端证据，系统已经具备课程设计答辩与后续迭代所需的可运行性、可解释性和可验证性。

后续工作可在不破坏现有主流程的前提下，优先完成任务类型与服务类型联动、敏感明细的角色化展示、基础数据漂移核验以及持续集成；需要外部能力的自动计价、文件凭证和定时签收应在明确接口、异常处理和数据模型后再逐步接入。

# 附录 A 图表索引

## 图索引

- 图 2-1 系统门户与访客入口
- 图 3-1 外卖分发任务发布成功页面
- 图 3-2 跑腿员任务大厅与接单入口
- 图 3-3 管理员退款处理列表
- 图 3-4 管理员统计面板与报表生成
- 图 4-1 系统总体 E-R 图
- 图 4-2 数据库关系图
- 图 5-1 阶段性自动化门禁执行记录
- 图 5-2 Oracle 数据库连接状态页面

## 表索引

- 角色需求与访问边界
- 非功能性需求
- 技术选型与运行结构
- MVC 五层职责
- 模块与核心数据
- 各业务模块动作序列与约束
- 数据表分组
- 测试结果
- 部署验证
