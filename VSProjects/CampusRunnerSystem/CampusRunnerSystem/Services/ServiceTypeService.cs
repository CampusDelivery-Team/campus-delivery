using CampusRunnerSystem.Models;
using CampusRunnerSystem.Repositories;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public class ServiceTypeService : IServiceTypeService
{
    private readonly IServiceTypeRepository _serviceTypeRepository;

    public ServiceTypeService(IServiceTypeRepository serviceTypeRepository)
    {
        _serviceTypeRepository = serviceTypeRepository;
    }

    public Result<List<ServiceTypeViewModel>> GetAllServiceTypes()
    {
        try
        {
            return Result<List<ServiceTypeViewModel>>.Ok(_serviceTypeRepository.GetAllServiceTypes());
        }
        catch (Exception ex)
        {
            return Result<List<ServiceTypeViewModel>>.Fail($"服务类型读取失败：{ex.Message}");
        }
    }

    public Result<ServiceTypeViewModel> GetServiceTypeById(int serviceTypeId)
    {
        try
        {
            var serviceType = _serviceTypeRepository.GetServiceTypeById(serviceTypeId);
            return serviceType == null
                ? Result<ServiceTypeViewModel>.Fail("未找到该服务类型。")
                : Result<ServiceTypeViewModel>.Ok(serviceType);
        }
        catch (Exception ex)
        {
            return Result<ServiceTypeViewModel>.Fail($"服务类型详情读取失败：{ex.Message}");
        }
    }

    public Result AddServiceType(ServiceTypeViewModel serviceType)
    {
        var check = ValidateServiceType(serviceType);
        if (!check.Success)
        {
            return check;
        }

        try
        {
            _serviceTypeRepository.AddServiceType(serviceType);
            return Result.Ok("服务类型新增成功。");
        }
        catch (Exception ex)
        {
            return Result.Fail($"服务类型新增失败：{ex.Message}");
        }
    }

    public Result UpdateServiceType(ServiceTypeViewModel serviceType)
    {
        var check = ValidateServiceType(serviceType);
        if (!check.Success)
        {
            return check;
        }

        try
        {
            _serviceTypeRepository.UpdateServiceType(serviceType);
            return Result.Ok("服务类型修改成功。");
        }
        catch (Exception ex)
        {
            return Result.Fail($"服务类型修改失败：{ex.Message}");
        }
    }

    public Result DeleteServiceType(int serviceTypeId)
    {
        try
        {
            if (_serviceTypeRepository.CountTaskReferences(serviceTypeId) > 0)
            {
                return Result.Fail("该服务类型已被任务引用，不能删除。");
            }

            if (_serviceTypeRepository.CountRuleReferences(serviceTypeId) > 0)
            {
                return Result.Fail("该服务类型已绑定节点，请先删除服务节点规则。");
            }

            _serviceTypeRepository.DeleteServiceType(serviceTypeId);
            return Result.Ok("服务类型删除成功。");
        }
        catch (Exception)
        {
            return Result.Fail("服务类型删除失败，该服务类型可能已被其他业务数据引用。");
        }
    }

    private static Result ValidateServiceType(ServiceTypeViewModel serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType.ServiceName))
        {
            return Result.Fail("请填写服务名称。");
        }

        if (serviceType.BasePrice < 0)
        {
            return Result.Fail("基础价格不能小于 0。");
        }

        if (serviceType.TypeStatus != SystemConstants.ServiceTypeStatus.Enabled
            && serviceType.TypeStatus != SystemConstants.ServiceTypeStatus.Disabled)
        {
            return Result.Fail("类型状态只能是“启用”或“禁用”。");
        }

        return Result.Ok();
    }
}
