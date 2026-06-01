using CampusRunnerSystem.Helpers;
using CampusRunnerSystem.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace CampusRunnerSystem.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly OracleDbHelper _dbHelper;

    public AccountRepository(OracleDbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public LoginUserViewModel? FindByUsername(string username)
    {
        const string sql = @"
SELECT user_id, username, password_hash, user_role, account_status
FROM users
WHERE username = :username";

        var table = _dbHelper.QueryDataTable(
            sql,
            new OracleParameter("username", username));

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new LoginUserViewModel
        {
            UserId = Convert.ToInt32(row["user_id"]),
            Username = row["username"].ToString() ?? string.Empty,
            PasswordHash = row["password_hash"].ToString() ?? string.Empty,
            UserRole = row["user_role"].ToString() ?? string.Empty,
            AccountStatus = row["account_status"].ToString() ?? string.Empty
        };
    }
}
