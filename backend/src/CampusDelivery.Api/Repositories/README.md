# 持久层 Repositories

Repository 是持久层的一部分，负责具体 SQL 和数据库读写。

本层应该存放：

```text
UserRepository.cs
AddressRepository.cs
NodeRepository.cs
ServiceTypeRepository.cs
RunnerRepository.cs
TaskRepository.cs
AssignRepository.cs
TaskStatusLogRepository.cs
PaymentRepository.cs
RefundRepository.cs
ReviewRepository.cs
ComplaintRepository.cs
SettlementRepository.cs
AuditRepository.cs
ReportRepository.cs
```

本层负责：

- 编写 SQL。
- 调用 `OracleConnectionFactory` 或 `OracleDbHelper`。
- 将数据库查询结果转换为 Model / DTO。
- 执行新增、修改、删除、查询。

本层不应该：

- 决定页面跳转。
- 处理按钮显示隐藏。
- 编写复杂业务规则。
- 直接使用用户界面文本作为数据库逻辑。

调用方向：

```text
持久层 -> 数据库层
```
