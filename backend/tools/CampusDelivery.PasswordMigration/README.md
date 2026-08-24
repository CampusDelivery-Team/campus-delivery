# 密码统一迁移工具

该工具只用于把 `APPUSER.users.password_hash` 中的历史明文转换为当前项目使用的 ASP.NET Core Identity 带盐哈希。

- `--dry-run`：只统计，不锁表、不写数据库。
- `--execute`：锁定用户记录，核对预期数量，生成 DPAPI 加密备份后在一个事务中迁移。
- `--verify-backup`：使用当前 Windows 用户解密并校验备份，不连接或修改数据库。
- `--verify-migrated`：只读校验数据库中的新哈希仍能接受备份里的每个原密码，不输出账号或密码。
- `--restore`：使用同一 Windows 用户解密备份，并在一个事务中恢复迁移前的密码字段。

连接串只能通过环境变量 `ConnectionStrings__OracleDb` 提供，工具不会读取或打印仓库中的本地配置。

```powershell
dotnet run --project backend/tools/CampusDelivery.PasswordMigration -- --dry-run

dotnet run --project backend/tools/CampusDelivery.PasswordMigration -- `
  --execute `
  --backup "C:\absolute\path\users-passwords.dpapi" `
  --expected-legacy-count 25

dotnet run --project backend/tools/CampusDelivery.PasswordMigration -- `
  --verify-backup "C:\absolute\path\users-passwords.dpapi"

dotnet run --project backend/tools/CampusDelivery.PasswordMigration -- `
  --verify-migrated "C:\absolute\path\users-passwords.dpapi"

dotnet run --project backend/tools/CampusDelivery.PasswordMigration -- `
  --restore "C:\absolute\path\users-passwords.dpapi"
```

正式执行前应暂停账号写入，并让所有组员升级到使用 `PasswordHasher<User>` 的当前代码。
