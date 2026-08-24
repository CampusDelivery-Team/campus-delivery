using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class DatabaseService(IDatabaseRepository databaseRepository) : IDatabaseService
{
    public async Task<DatabaseStatusViewModel> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            int userCount = await databaseRepository.GetUserCountAsync(cancellationToken);
            return new DatabaseStatusViewModel
            {
                IsConnected = true,
                UserCount = userCount,
                Message = "Oracle connected successfully."
            };
        }
        catch
        {
            return new DatabaseStatusViewModel
            {
                IsConnected = false,
                Message = "数据库连接失败，请联系服务器负责人查看后端日志。"
            };
        }
    }
}
