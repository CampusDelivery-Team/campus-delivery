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
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        // 1. 根据账号查找用户（用于登录校验，以及注册时检查账号是否已存在）
        public User GetUserByUsername(string username)
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
                                Username = reader["username"].ToString(),
                                Phone = reader["phone"].ToString(),
                                PasswordHash = reader["password_hash"].ToString(),
                                UserRole = reader["user_role"].ToString(),
                                AccountStatus = reader["account_status"].ToString()
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
    }

}
