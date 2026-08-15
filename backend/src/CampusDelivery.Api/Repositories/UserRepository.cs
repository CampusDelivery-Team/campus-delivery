using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using CampusDelivery.Api.Models;
using Microsoft.Extensions.Configuration;

namespace CampusDelivery.Api.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        // 通过依赖注入获取 appsettings.Local.json 里的连接字符串
        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleDb")
                ?? throw new InvalidOperationException("Connection string 'OracleDb' is missing.");
        }

        // 1. 根据账号查找用户（用于登录校验，以及注册时检查账号是否已存在）
        public User? GetUserByUsername(string username)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                // ⚠️ 极其重要：表名前必须加 APPUSER.
                string sql = @"SELECT user_id, username, phone, password_hash, user_role, account_status 
                               FROM APPUSER.users 
                               WHERE username = :username";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    // 防止 SQL 注入的安全写法
                    cmd.Parameters.Add(new OracleParameter("username", username));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserId = Convert.ToInt32(reader["user_id"]),
                                Username = GetString(reader, "username"),
                                Phone = GetString(reader, "phone"),
                                PasswordHash = GetString(reader, "password_hash"),
                                UserRole = GetString(reader, "user_role"),
                                AccountStatus = GetString(reader, "account_status")
                            };
                        }
                    }
                }
            }
            return null; // 如果没查到，返回 null
        }

        // 2. 插入新用户（用于注册功能）
        public bool InsertUser(User user)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
            
                string sql = @"INSERT INTO APPUSER.users 
                               (username, phone, password_hash, user_role, account_status) 
                               VALUES 
                               (:username, :phone, :password_hash, :user_role, :account_status)";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("username", user.Username));
                    cmd.Parameters.Add(new OracleParameter("phone", user.Phone));
                    cmd.Parameters.Add(new OracleParameter("password_hash", user.PasswordHash));
                    cmd.Parameters.Add(new OracleParameter("user_role", user.UserRole));
                    cmd.Parameters.Add(new OracleParameter("account_status", user.AccountStatus));

                    // 执行插入，如果受影响的行数大于 0，说明插入成功
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool UpdatePasswordHash(int userId, string passwordHash)
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                connection.Open();

                const string sql = @"UPDATE APPUSER.users
                                     SET password_hash = :passwordHash
                                     WHERE user_id = :userId";
                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("passwordHash", passwordHash));
                    command.Parameters.Add(new OracleParameter("userId", userId));
                    return command.ExecuteNonQuery() == 1;
                }
            }
        }

        // 3. 更新用户手机号
        public bool UpdateUserPhone(int userId, string newPhone)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
   
                string sql = @"UPDATE APPUSER.users SET phone = :phone WHERE user_id = :user_id";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("phone", newPhone));
                    cmd.Parameters.Add(new OracleParameter("user_id", userId));

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public UserAddress? GetPrimaryAddress(int userId)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();

                const string sql = @"SELECT contact_name, contact_phone, campus, building_room, is_default
                                     FROM APPUSER.user_addresses
                                     WHERE user_id = :user_id
                                     ORDER BY CASE is_default WHEN 'Y' THEN 0 ELSE 1 END, address_no
                                     FETCH FIRST 1 ROWS ONLY";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("user_id", userId));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        return new UserAddress
                        {
                            ContactName = GetString(reader, "contact_name"),
                            ContactPhone = GetString(reader, "contact_phone"),
                            Campus = GetString(reader, "campus"),
                            BuildingRoom = GetString(reader, "building_room"),
                            IsDefault = GetString(reader, "is_default", "N")
                        };
                    }
                }
            }
        }

        public List<ManagedAccount> GetManagedAccounts()
        {
            var accounts = new List<ManagedAccount>();
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                const string sql = @"SELECT u.user_id, u.username, u.phone, u.user_role, u.account_status,
                                            r.runner_id, r.real_name, r.audit_status, r.work_status
                                     FROM APPUSER.users u
                                     LEFT JOIN APPUSER.runners r ON r.user_id = u.user_id
                                     WHERE u.user_role IN ('USER', 'RUNNER')
                                     ORDER BY u.user_id";
                using (OracleCommand command = new OracleCommand(sql, connection))
                using (OracleDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        accounts.Add(new ManagedAccount
                        {
                            UserId = Convert.ToInt32(reader["user_id"]),
                            Username = GetString(reader, "username"),
                            Phone = GetString(reader, "phone"),
                            UserRole = GetString(reader, "user_role", "USER"),
                            AccountStatus = GetString(reader, "account_status", "NORMAL"),
                            RunnerId = reader["runner_id"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(reader["runner_id"]),
                            RealName = reader["real_name"] == DBNull.Value
                                ? null
                                : GetString(reader, "real_name"),
                            RunnerAuditStatus = reader["audit_status"] == DBNull.Value
                                ? null
                                : GetString(reader, "audit_status"),
                            RunnerWorkStatus = reader["work_status"] == DBNull.Value
                                ? null
                                : GetString(reader, "work_status")
                        });
                    }
                }
            }

            return accounts;
        }

        public bool UpdateAccountStatus(int userId, string nextStatus, params string[] allowedCurrentStatuses)
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                using (OracleTransaction transaction = connection.BeginTransaction())
                {
                    var statusParameters = string.Join(", ", allowedCurrentStatuses.Select((_, index) => $":status{index}"));
                    using (OracleCommand accountCommand = new OracleCommand($@"UPDATE APPUSER.users
                        SET account_status = :nextStatus
                        WHERE user_id = :userId
                          AND user_role IN ('USER', 'RUNNER')
                          AND account_status IN ({statusParameters})", connection))
                    {
                        accountCommand.Transaction = transaction;
                        accountCommand.Parameters.Add(new OracleParameter("nextStatus", nextStatus));
                        accountCommand.Parameters.Add(new OracleParameter("userId", userId));
                        for (var index = 0; index < allowedCurrentStatuses.Length; index++)
                        {
                            accountCommand.Parameters.Add(new OracleParameter($"status{index}", allowedCurrentStatuses[index]));
                        }

                        if (accountCommand.ExecuteNonQuery() == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    if (nextStatus is "BLOCKED" or "CANCELLED")
                    {
                        using OracleCommand runnerCommand = new OracleCommand(
                            "UPDATE APPUSER.runners SET work_status = 'OFFLINE' WHERE user_id = :userId",
                            connection);
                        runnerCommand.Transaction = transaction;
                        runnerCommand.Parameters.Add(new OracleParameter("userId", userId));
                        runnerCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
            }
        }

        public bool RevokeRunnerQualification(int userId)
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                using (OracleTransaction transaction = connection.BeginTransaction())
                {
                    using (OracleCommand userCommand = new OracleCommand(@"UPDATE APPUSER.users
                        SET user_role = 'USER'
                        WHERE user_id = :userId
                          AND user_role = 'RUNNER'
                          AND account_status <> 'CANCELLED'", connection))
                    {
                        userCommand.Transaction = transaction;
                        userCommand.Parameters.Add(new OracleParameter("userId", userId));
                        if (userCommand.ExecuteNonQuery() == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    using (OracleCommand runnerCommand = new OracleCommand(@"UPDATE APPUSER.runners
                        SET audit_status = 'REJECTED',
                            work_status = 'OFFLINE'
                        WHERE user_id = :userId
                          AND audit_status = 'APPROVED'", connection))
                    {
                        runnerCommand.Transaction = transaction;
                        runnerCommand.Parameters.Add(new OracleParameter("userId", userId));
                        if (runnerCommand.ExecuteNonQuery() == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    transaction.Commit();
                    return true;
                }
            }
        }

        private static string GetString(OracleDataReader reader, string columnName, string fallback = "")
        {
            return reader[columnName] == DBNull.Value
                ? fallback
                : reader[columnName].ToString() ?? fallback;
        }
    }

}
