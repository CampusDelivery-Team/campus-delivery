using System;
using System.Data;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CampusDelivery.Api.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly OracleConnectionFactory _connectionFactory;

        // 通过依赖注入获取 appsettings.Local.json 里的连接字符串
        public UserRepository(OracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public User? GetUserById(int userId)
        {
            using OracleConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = """
                SELECT user_id, username, phone, password_hash, user_role, account_status
                  FROM APPUSER.users
                 WHERE user_id = :userId
                """;
            command.Parameters.Add(new OracleParameter("userId", userId));
            using OracleDataReader reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

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

        // 1. 根据账号查找用户（用于登录校验，以及注册时检查账号是否已存在）
        public User? GetUserByUsername(string username)
        {
            using (OracleConnection conn = _connectionFactory.CreateConnection())
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

        public User? GetUserByPhone(string phone)
        {
            using OracleConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            const string sql = @"SELECT user_id, username, phone, password_hash, user_role, account_status
                                 FROM APPUSER.users
                                 WHERE phone = :phone";
            using OracleCommand command = new OracleCommand(sql, connection)
            {
                BindByName = true
            };
            command.Parameters.Add(new OracleParameter("phone", phone));

            using OracleDataReader reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

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

        public string? GetRunnerRealName(int userId)
        {
            using OracleConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            const string sql = @"SELECT real_name
                                 FROM APPUSER.runners
                                 WHERE user_id = :userId";
            using OracleCommand command = new OracleCommand(sql, connection)
            {
                BindByName = true
            };
            command.Parameters.Add(new OracleParameter("userId", userId));

            object? result = command.ExecuteScalar();
            return result is null or DBNull ? null : Convert.ToString(result);
        }

        // 2. 插入新用户（用于注册功能）
        public UserInsertWriteResult InsertUser(User user)
        {
            try
            {
                using OracleConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                const string sql = @"INSERT INTO APPUSER.users
                                     (username, phone, password_hash, user_role, account_status)
                                     VALUES
                                     (:username, :phone, :password_hash, :user_role, :account_status)";
                using OracleCommand command = new OracleCommand(sql, connection)
                {
                    BindByName = true
                };
                command.Parameters.Add(new OracleParameter("username", user.Username));
                command.Parameters.Add(new OracleParameter("phone", user.Phone));
                command.Parameters.Add(new OracleParameter("password_hash", user.PasswordHash));
                command.Parameters.Add(new OracleParameter("user_role", user.UserRole));
                command.Parameters.Add(new OracleParameter("account_status", user.AccountStatus));

                return command.ExecuteNonQuery() == 1
                    ? UserInsertWriteResult.Success
                    : UserInsertWriteResult.Failed;
            }
            catch (OracleException exception) when (exception.Number == 1)
            {
                if (exception.Message.Contains("UK_USERS_USERNAME", StringComparison.OrdinalIgnoreCase))
                {
                    return UserInsertWriteResult.DuplicateUsername;
                }

                if (exception.Message.Contains("UK_USERS_PHONE", StringComparison.OrdinalIgnoreCase))
                {
                    return UserInsertWriteResult.DuplicatePhone;
                }

                return UserInsertWriteResult.Failed;
            }
        }

        public bool UpdatePasswordHash(int userId, string passwordHash)
        {
            using (OracleConnection connection = _connectionFactory.CreateConnection())
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
        public UserPhoneUpdateWriteResult UpdateUserPhone(int userId, string newPhone)
        {
            try
            {
                using OracleConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                const string sql = @"UPDATE APPUSER.users SET phone = :phone WHERE user_id = :user_id";
                using OracleCommand command = new OracleCommand(sql, connection)
                {
                    BindByName = true
                };
                command.Parameters.Add(new OracleParameter("phone", newPhone));
                command.Parameters.Add(new OracleParameter("user_id", userId));

                return command.ExecuteNonQuery() == 1
                    ? UserPhoneUpdateWriteResult.Success
                    : UserPhoneUpdateWriteResult.NotFound;
            }
            catch (OracleException exception) when (
                exception.Number == 1
                && exception.Message.Contains("UK_USERS_PHONE", StringComparison.OrdinalIgnoreCase))
            {
                return UserPhoneUpdateWriteResult.DuplicatePhone;
            }
        }

        public UserAddress? GetPrimaryAddress(int userId)
        {
            using (OracleConnection conn = _connectionFactory.CreateConnection())
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
            using (OracleConnection connection = _connectionFactory.CreateConnection())
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
            try
            {
                using (OracleConnection connection = _connectionFactory.CreateConnection())
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
            catch (OracleException exception) when (exception.Number == 2290)
            {
                throw new RepositorySchemaException(
                    "账号状态约束尚未升级。",
                    exception);
            }
        }

        public AccountStatusProcedureResult ManageAccountStatus(int userId, string action)
        {
            using OracleConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using OracleTransaction transaction = connection.BeginTransaction();

            try
            {
                using OracleCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.BindByName = true;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "APPUSER.sp_manage_account_status";
                command.Parameters.Add(new OracleParameter("p_user_id", userId));
                command.Parameters.Add(new OracleParameter("p_action", action));
                var resultParameter = new OracleParameter("p_result", OracleDbType.Varchar2, 40)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(resultParameter);
                command.ExecuteNonQuery();

                string result = resultParameter.Value is OracleString oracleString
                    ? oracleString.Value
                    : Convert.ToString(resultParameter.Value) ?? string.Empty;
                AccountStatusProcedureResult mappedResult = result.Trim() switch
                {
                    "SUCCESS" => AccountStatusProcedureResult.Success,
                    "NOT_FOUND" => AccountStatusProcedureResult.NotFound,
                    "ROLE_NOT_MANAGEABLE" => AccountStatusProcedureResult.RoleNotManageable,
                    "NOT_NORMAL" or "NOT_BLOCKED" => AccountStatusProcedureResult.InvalidState,
                    "INVALID_ACTION" => AccountStatusProcedureResult.InvalidAction,
                    _ => AccountStatusProcedureResult.Failed
                };

                if (mappedResult == AccountStatusProcedureResult.Success)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }

                return mappedResult;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool RevokeRunnerQualification(int userId)
        {
            using (OracleConnection connection = _connectionFactory.CreateConnection())
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
