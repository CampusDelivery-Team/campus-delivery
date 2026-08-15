using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CampusDelivery.Api.Models;
using Microsoft.AspNetCore.Identity;
using Oracle.ManagedDataAccess.Client;

return await PasswordMigrationApplication.RunAsync(args);

internal static class PasswordMigrationApplication
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__OracleDb";
    private static readonly byte[] BackupEntropy =
        Encoding.UTF8.GetBytes("CampusDelivery.PasswordMigration.v1");

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            string? verifyBackupPath = GetOption(args, "--verify-backup");
            if (verifyBackupPath is not null)
            {
                return await VerifyBackupAsync(verifyBackupPath);
            }

            string connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
                ?? throw new InvalidOperationException(
                    $"缺少环境变量 {ConnectionStringEnvironmentVariable}，迁移工具不会从仓库文件读取数据库凭据。");

            if (args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase))
            {
                return await DryRunAsync(connectionString);
            }

            string? verifyMigratedPath = GetOption(args, "--verify-migrated");
            if (verifyMigratedPath is not null)
            {
                return await VerifyMigratedAsync(connectionString, verifyMigratedPath);
            }

            string? restorePath = GetOption(args, "--restore");
            if (restorePath is not null)
            {
                return await RestoreAsync(connectionString, restorePath);
            }

            if (args.Contains("--execute", StringComparer.OrdinalIgnoreCase))
            {
                string backupPath = GetRequiredOption(args, "--backup");
                int expectedLegacyCount = int.Parse(GetRequiredOption(args, "--expected-legacy-count"));
                return await ExecuteAsync(connectionString, backupPath, expectedLegacyCount);
            }

            Console.Error.WriteLine(
                "用法：--dry-run | --verify-backup <备份路径> | --verify-migrated <备份路径> | --execute --backup <绝对路径> --expected-legacy-count <数量> | --restore <备份路径>");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"MIGRATION_FAILED|{exception.GetType().Name}|{exception.Message}");
            return 1;
        }
    }

    private static async Task<int> DryRunAsync(string connectionString)
    {
        await using OracleConnection connection = new(connectionString);
        await connection.OpenAsync();
        List<PasswordAccount> accounts = await LoadAccountsAsync(connection, null, forUpdate: false);
        MigrationInventory inventory = BuildInventory(accounts);
        PrintInventory(inventory);
        return inventory.UnsupportedCount == 0 ? 0 : 3;
    }

    private static async Task<int> ExecuteAsync(
        string connectionString,
        string backupPath,
        int expectedLegacyCount)
    {
        if (!Path.IsPathFullyQualified(backupPath))
        {
            throw new InvalidOperationException("加密备份路径必须是绝对路径。");
        }

        if (File.Exists(backupPath))
        {
            throw new InvalidOperationException("备份文件已经存在，拒绝覆盖。");
        }

        await using OracleConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using OracleTransaction transaction = connection.BeginTransaction();

        try
        {
            List<PasswordAccount> accounts = await LoadAccountsAsync(connection, transaction, forUpdate: true);
            MigrationInventory inventory = BuildInventory(accounts);
            PrintInventory(inventory);

            if (inventory.LegacyAccounts.Count != expectedLegacyCount)
            {
                throw new InvalidOperationException(
                    $"旧格式账号数量从预期的 {expectedLegacyCount} 变为 {inventory.LegacyAccounts.Count}，已停止迁移。");
            }

            if (inventory.UnsupportedCount > 0)
            {
                throw new InvalidOperationException(
                    $"存在 {inventory.UnsupportedCount} 条无法安全识别的数据，已停止迁移。");
            }

            await WriteEncryptedBackupAsync(backupPath, inventory.LegacyAccounts);

            PasswordHasher<User> passwordHasher = new();
            int migratedCount = 0;
            foreach (PasswordAccount account in inventory.LegacyAccounts)
            {
                User user = new()
                {
                    UserId = account.UserId,
                    Username = account.Username,
                    PasswordHash = account.StoredPassword
                };
                string newHash = passwordHasher.HashPassword(user, account.StoredPassword);
                PasswordVerificationResult verification =
                    passwordHasher.VerifyHashedPassword(user, newHash, account.StoredPassword);
                if (verification == PasswordVerificationResult.Failed)
                {
                    throw new InvalidOperationException(
                        $"用户编号 {account.UserId} 的新哈希内存校验失败。");
                }

                await using OracleCommand updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.BindByName = true;
                updateCommand.CommandText = """
                    UPDATE APPUSER.users
                    SET password_hash = :newPasswordHash
                    WHERE user_id = :userId
                      AND password_hash = :oldPasswordHash
                    """;
                updateCommand.Parameters.Add(new OracleParameter("newPasswordHash", newHash));
                updateCommand.Parameters.Add(new OracleParameter("userId", account.UserId));
                updateCommand.Parameters.Add(new OracleParameter("oldPasswordHash", account.StoredPassword));

                if (await updateCommand.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        $"用户编号 {account.UserId} 在迁移期间发生变化，已停止迁移。");
                }

                migratedCount++;
            }

            List<PasswordAccount> postMigrationAccounts =
                await LoadAccountsAsync(connection, transaction, forUpdate: false);
            MigrationInventory postMigrationInventory = BuildInventory(postMigrationAccounts);
            if (postMigrationInventory.LegacyAccounts.Count != 0
                || postMigrationInventory.UnsupportedCount != 0)
            {
                throw new InvalidOperationException("事务内校验发现仍有旧格式密码，已停止迁移。");
            }

            await transaction.CommitAsync();
            Console.WriteLine($"MIGRATION_COMMITTED|{migratedCount}");
            Console.WriteLine($"ENCRYPTED_BACKUP|{Path.GetFullPath(backupPath)}");
            return 0;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> RestoreAsync(string connectionString, string backupPath)
    {
        List<PasswordAccount> backupAccounts = await ReadEncryptedBackupAsync(backupPath);
        if (backupAccounts.Count == 0)
        {
            throw new InvalidOperationException("备份中没有可恢复记录。");
        }

        await using OracleConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using OracleTransaction transaction = connection.BeginTransaction();

        try
        {
            int restoredCount = 0;
            foreach (PasswordAccount account in backupAccounts)
            {
                await using OracleCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.BindByName = true;
                command.CommandText = """
                    UPDATE APPUSER.users
                    SET password_hash = :passwordHash
                    WHERE user_id = :userId
                      AND username = :username
                    """;
                command.Parameters.Add(new OracleParameter("passwordHash", account.StoredPassword));
                command.Parameters.Add(new OracleParameter("userId", account.UserId));
                command.Parameters.Add(new OracleParameter("username", account.Username));
                if (await command.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        $"用户编号 {account.UserId} 无法恢复，已回滚全部恢复操作。");
                }

                restoredCount++;
            }

            await transaction.CommitAsync();
            Console.WriteLine($"RESTORE_COMMITTED|{restoredCount}");
            return 0;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> VerifyBackupAsync(string backupPath)
    {
        List<PasswordAccount> backupAccounts = await ReadEncryptedBackupAsync(backupPath);
        if (backupAccounts.Count == 0)
        {
            throw new InvalidOperationException("备份中没有记录。");
        }

        if (backupAccounts.Any(account => !IsSupportedLegacyValue(account.StoredPassword)))
        {
            throw new InvalidOperationException("备份包含无法安全识别的密码数据。");
        }

        if (backupAccounts.Select(account => account.UserId).Distinct().Count() != backupAccounts.Count
            || backupAccounts.Select(account => account.Username).Distinct(StringComparer.Ordinal).Count()
                != backupAccounts.Count)
        {
            throw new InvalidOperationException("备份中的用户标识存在重复。");
        }

        Console.WriteLine($"BACKUP_VERIFIED|{backupAccounts.Count}");
        return 0;
    }

    private static async Task<int> VerifyMigratedAsync(string connectionString, string backupPath)
    {
        List<PasswordAccount> backupAccounts = await ReadEncryptedBackupAsync(backupPath);
        if (backupAccounts.Count == 0)
        {
            throw new InvalidOperationException("备份中没有可校验记录。");
        }

        await using OracleConnection connection = new(connectionString);
        await connection.OpenAsync();
        List<PasswordAccount> currentAccounts =
            await LoadAccountsAsync(connection, null, forUpdate: false);
        Dictionary<int, PasswordAccount> currentById = currentAccounts.ToDictionary(account => account.UserId);
        PasswordHasher<User> passwordHasher = new();

        foreach (PasswordAccount backupAccount in backupAccounts)
        {
            if (!currentById.TryGetValue(backupAccount.UserId, out PasswordAccount? currentAccount)
                || !string.Equals(
                    currentAccount.Username,
                    backupAccount.Username,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"用户编号 {backupAccount.UserId} 与备份不一致。");
            }

            User user = new()
            {
                UserId = currentAccount.UserId,
                Username = currentAccount.Username,
                PasswordHash = currentAccount.StoredPassword
            };
            PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(
                user,
                currentAccount.StoredPassword,
                backupAccount.StoredPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new InvalidOperationException(
                    $"用户编号 {backupAccount.UserId} 的迁移后密码校验失败。");
            }
        }

        Console.WriteLine($"MIGRATED_PASSWORDS_VERIFIED|{backupAccounts.Count}");
        return 0;
    }

    private static async Task<List<PasswordAccount>> LoadAccountsAsync(
        OracleConnection connection,
        OracleTransaction? transaction,
        bool forUpdate)
    {
        var accounts = new List<PasswordAccount>();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT user_id, username, password_hash
            FROM APPUSER.users
            ORDER BY user_id
            """ + (forUpdate ? " FOR UPDATE OF password_hash" : string.Empty);

        await using OracleDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            accounts.Add(new PasswordAccount(
                Convert.ToInt32(reader["user_id"]),
                Convert.ToString(reader["username"]) ?? string.Empty,
                Convert.ToString(reader["password_hash"]) ?? string.Empty));
        }

        return accounts;
    }

    private static MigrationInventory BuildInventory(List<PasswordAccount> accounts)
    {
        List<PasswordAccount> identityAccounts = accounts
            .Where(account => IsIdentityHash(account.StoredPassword))
            .ToList();
        List<PasswordAccount> legacyAccounts = accounts
            .Where(account => !IsIdentityHash(account.StoredPassword)
                && IsSupportedLegacyValue(account.StoredPassword))
            .ToList();
        int unsupportedCount = accounts.Count - identityAccounts.Count - legacyAccounts.Count;
        return new MigrationInventory(accounts.Count, identityAccounts.Count, legacyAccounts, unsupportedCount);
    }

    private static bool IsIdentityHash(string value)
    {
        try
        {
            byte[] payload = Convert.FromBase64String(value);
            return payload.Length >= 13 && payload[0] == 0x01;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsSupportedLegacyValue(string value) =>
        value.Length is >= 6 and <= 128
        && value.All(character => !char.IsControl(character));

    private static async Task WriteEncryptedBackupAsync(
        string backupPath,
        List<PasswordAccount> accounts)
    {
        string? directory = Path.GetDirectoryName(backupPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("备份目录无效。");
        }

        Directory.CreateDirectory(directory);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(new PasswordBackup(
            DateTimeOffset.UtcNow,
            Environment.MachineName,
            accounts));
        byte[] encrypted = ProtectedData.Protect(json, BackupEntropy, DataProtectionScope.CurrentUser);
        CryptographicOperations.ZeroMemory(json);
        await File.WriteAllBytesAsync(backupPath, encrypted);
    }

    private static async Task<List<PasswordAccount>> ReadEncryptedBackupAsync(string backupPath)
    {
        byte[] encrypted = await File.ReadAllBytesAsync(backupPath);
        byte[] json = ProtectedData.Unprotect(encrypted, BackupEntropy, DataProtectionScope.CurrentUser);
        try
        {
            PasswordBackup? backup = JsonSerializer.Deserialize<PasswordBackup>(json);
            return backup?.Accounts ?? [];
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
        }
    }

    private static void PrintInventory(MigrationInventory inventory)
    {
        Console.WriteLine($"TOTAL|{inventory.TotalCount}");
        Console.WriteLine($"IDENTITY|{inventory.IdentityCount}");
        Console.WriteLine($"LEGACY_CANDIDATE|{inventory.LegacyAccounts.Count}");
        Console.WriteLine($"UNSUPPORTED|{inventory.UnsupportedCount}");
    }

    private static string GetRequiredOption(string[] args, string name) =>
        GetOption(args, name)
        ?? throw new InvalidOperationException($"缺少必需参数 {name}。");

    private static string? GetOption(string[] args, string name)
    {
        int index = Array.FindIndex(
            args,
            value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private sealed record PasswordAccount(int UserId, string Username, string StoredPassword);

    private sealed record PasswordBackup(
        DateTimeOffset CreatedAtUtc,
        string MachineName,
        List<PasswordAccount> Accounts);

    private sealed record MigrationInventory(
        int TotalCount,
        int IdentityCount,
        List<PasswordAccount> LegacyAccounts,
        int UnsupportedCount);
}
