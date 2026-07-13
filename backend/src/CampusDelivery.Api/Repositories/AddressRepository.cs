using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using CampusDelivery.Api.Models;
using Microsoft.Extensions.Configuration;

namespace CampusDelivery.Api.Repositories
{
    public class AddressRepository
    {
        private readonly string _connectionString;

        public AddressRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        // 1. 查询某个用户的所有地址
        public List<UserAddress> GetAddressesByUserId(int userId)
        {
            var list = new List<UserAddress>();
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                // 注意 APPUSER 前缀。且这里用 ORDER BY 让默认地址(Y)排在最上面
                string sql = @"SELECT user_id, address_no, contact_name, contact_phone, campus, building_room, is_default 
                               FROM APPUSER.user_addresses 
                               WHERE user_id = :user_id 
                               ORDER BY is_default DESC, address_no ASC";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("user_id", userId));
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new UserAddress
                            {
                                UserId = Convert.ToInt32(reader["user_id"]),
                                AddressNo = Convert.ToInt32(reader["address_no"]),
                                ContactName = reader["contact_name"].ToString(),
                                ContactPhone = reader["contact_phone"].ToString(),
                                Campus = reader["campus"].ToString(),
                                BuildingRoom = reader["building_room"].ToString(),
                                IsDefault = reader["is_default"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 2. 新增地址
        public int InsertAddress(UserAddress address)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();

                int nextAddressNo = 1;
                string sqlMax = "SELECT NVL(MAX(address_no), 0) + 1 FROM APPUSER.user_addresses WHERE user_id = :user_id";
                using (OracleCommand cmdMax = new OracleCommand(sqlMax, conn))
                {
                    cmdMax.Parameters.Add(new OracleParameter("user_id", address.UserId));
                    object result = cmdMax.ExecuteScalar();
                    if (result != DBNull.Value) nextAddressNo = Convert.ToInt32(result);
                }

                string sqlInsert = @"INSERT INTO APPUSER.user_addresses 
                                     (user_id, address_no, contact_name, contact_phone, campus, building_room, is_default) 
                                     VALUES 
                                     (:user_id, :address_no, :contact_name, :contact_phone, :campus, :building_room, :is_default)";
                using (OracleCommand cmdInsert = new OracleCommand(sqlInsert, conn))
                {
                    cmdInsert.Parameters.Add(new OracleParameter("user_id", address.UserId));
                    cmdInsert.Parameters.Add(new OracleParameter("address_no", nextAddressNo));
                    cmdInsert.Parameters.Add(new OracleParameter("contact_name", address.ContactName));
                    cmdInsert.Parameters.Add(new OracleParameter("contact_phone", address.ContactPhone));
                    cmdInsert.Parameters.Add(new OracleParameter("campus", address.Campus));
                    cmdInsert.Parameters.Add(new OracleParameter("building_room", address.BuildingRoom));
                    cmdInsert.Parameters.Add(new OracleParameter("is_default", address.IsDefault));

                    // 如果插入成功，返回新算出来的序号；否则返回 0
                    return cmdInsert.ExecuteNonQuery() > 0 ? nextAddressNo : 0;
                }
            }
        }

        // 3. 设置默认地址 (连环操作)
        public bool SetDefaultAddress(int userId, int addressNo)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();

                // 为了保证全局只有一个默认地址，最安全的做法是：先一棒子打死，把该用户所有地址设为 'N'
                string sql1 = "UPDATE APPUSER.user_addresses SET is_default = 'N' WHERE user_id = :user_id";
                using (OracleCommand cmd1 = new OracleCommand(sql1, conn))
                {
                    cmd1.Parameters.Add(new OracleParameter("user_id", userId));
                    cmd1.ExecuteNonQuery();
                }

                // 然后再精准打击，把用户选中的那个地址设为 'Y'
                string sql2 = "UPDATE APPUSER.user_addresses SET is_default = 'Y' WHERE user_id = :user_id AND address_no = :address_no";
                using (OracleCommand cmd2 = new OracleCommand(sql2, conn))
                {
                    cmd2.Parameters.Add(new OracleParameter("user_id", userId));
                    cmd2.Parameters.Add(new OracleParameter("address_no", addressNo));
                    return cmd2.ExecuteNonQuery() > 0;
                }
            }
        }

        // 4. 获取单条地址（修改页面读取旧数据时使用）
        public UserAddress? GetAddress(int userId, int addressNo)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM APPUSER.user_addresses WHERE user_id = :user_id AND address_no = :address_no";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("user_id", userId));
                    cmd.Parameters.Add(new OracleParameter("address_no", addressNo));
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserAddress
                            {
                                UserId = Convert.ToInt32(reader["user_id"]),
                                AddressNo = Convert.ToInt32(reader["address_no"]),
                                ContactName = reader["contact_name"].ToString(),
                                ContactPhone = reader["contact_phone"].ToString(),
                                Campus = reader["campus"].ToString(),
                                BuildingRoom = reader["building_room"].ToString(),
                                IsDefault = reader["is_default"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        // 5. 保存修改后的地址
        public bool UpdateAddress(UserAddress address)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                string sql = @"UPDATE APPUSER.user_addresses 
                               SET contact_name = :contact_name, contact_phone = :contact_phone, 
                                   campus = :campus, building_room = :building_room 
                               WHERE user_id = :user_id AND address_no = :address_no";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("contact_name", address.ContactName));
                    cmd.Parameters.Add(new OracleParameter("contact_phone", address.ContactPhone));
                    cmd.Parameters.Add(new OracleParameter("campus", address.Campus));
                    cmd.Parameters.Add(new OracleParameter("building_room", address.BuildingRoom));
                    cmd.Parameters.Add(new OracleParameter("user_id", address.UserId));
                    cmd.Parameters.Add(new OracleParameter("address_no", address.AddressNo));
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // 6. 物理删除地址
        public bool DeleteAddress(int userId, int addressNo)
        {
            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM APPUSER.user_addresses WHERE user_id = :user_id AND address_no = :address_no";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("user_id", userId));
                    cmd.Parameters.Add(new OracleParameter("address_no", addressNo));
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
