using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;

namespace CampusDelivery.Api.Services
{
    public sealed class TaskService
    {
        private readonly TaskRepository _taskRepository;
        private readonly AddressRepository _addressRepository;
        private readonly ServiceTypeRepository _serviceTypeRepository;
        private readonly NodeRepository _nodeRepository;

        public TaskService(
            TaskRepository taskRepository,
            AddressRepository addressRepository,
            ServiceTypeRepository serviceTypeRepository,
            NodeRepository nodeRepository)
        {
            _taskRepository = taskRepository;
            _addressRepository = addressRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _nodeRepository = nodeRepository;
        }

        public async Task<TaskCreateViewModel> BuildCreateModelAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            TaskCreateViewModel model = new TaskCreateViewModel();
            await PopulateCreateOptionsAsync(model, userId, cancellationToken);
            return model;
        }

        public async Task PopulateCreateOptionsAsync(
            TaskCreateViewModel model,
            int userId,
            CancellationToken cancellationToken = default)
        {
            List<UserAddress> addresses = _addressRepository.GetAddressesByUserId(userId);
            IReadOnlyList<ServiceType> serviceTypes = await _serviceTypeRepository.GetAllAsync(cancellationToken);
            IReadOnlyList<Node> nodes = await _nodeRepository.GetAllAsync(cancellationToken);

            List<TaskOptionViewModel> addressOptions = new List<TaskOptionViewModel>();
            foreach (UserAddress address in addresses)
            {
                addressOptions.Add(new TaskOptionViewModel
                {
                    Value = address.AddressNo,
                    Text = $"{address.ContactName} / {address.Campus} / {address.BuildingRoom}" +
                           (address.IsDefault == "Y" ? "（默认）" : string.Empty)
                });
            }

            List<TaskOptionViewModel> serviceTypeOptions = new List<TaskOptionViewModel>();
            foreach (ServiceType serviceType in serviceTypes)
            {
                if (serviceType.TypeStatus != "ENABLED")
                {
                    continue;
                }

                serviceTypeOptions.Add(new TaskOptionViewModel
                {
                    Value = serviceType.ServiceTypeId,
                    Text = $"{serviceType.ServiceName}（{serviceType.BasePrice:F2} 元起）"
                });
            }

            List<TaskOptionViewModel> nodeOptions = new List<TaskOptionViewModel>();
            foreach (Node node in nodes)
            {
                if (node.NodeStatus != "NORMAL")
                {
                    continue;
                }

                nodeOptions.Add(new TaskOptionViewModel
                {
                    Value = node.NodeId,
                    Text = $"{node.NodeName} · {DisplayNameService.GetNodeTypeName(node.NodeType)}"
                });
            }

            model.AddressOptions = addressOptions;
            model.ServiceTypeOptions = serviceTypeOptions;
            model.NodeOptions = nodeOptions;

            if (!model.AddressNo.HasValue)
            {
                UserAddress? defaultAddress = addresses
                    .OrderByDescending(address => address.IsDefault == "Y")
                    .ThenBy(address => address.AddressNo)
                    .FirstOrDefault();

                if (defaultAddress != null)
                {
                    model.AddressNo = defaultAddress.AddressNo;
                }
            }
        }

        public async Task<TaskOperationResult> CreateAsync(
            int userId,
            TaskCreateViewModel model,
            CancellationToken cancellationToken = default)
        {
            TaskPublishRequest request = new TaskPublishRequest
            {
                PublisherUserId = userId,
                ServiceTypeId = model.ServiceTypeId!.Value,
                AddressNo = model.AddressNo!.Value,
                NodeId = model.NodeId!.Value,
                TaskTitle = model.TaskTitle.Trim(),
                TaskPrice = model.TaskPrice!.Value,
                UrgentFlag = model.UrgentFlag == "Y" ? "Y" : "N",
                TaskKind = model.TaskKind,
                MerchantName = NormalizeText(model.MerchantName),
                PlatformOrderNo = NormalizeText(model.PlatformOrderNo),
                FoodPickupNote = NormalizeText(model.FoodPickupNote),
                ExpressCompany = NormalizeText(model.ExpressCompany),
                WaybillNo = NormalizeText(model.WaybillNo),
                PickupCode = NormalizeText(model.PickupCode),
                ExpressPickupNote = NormalizeText(model.ExpressPickupNote),
                ItemCategory = NormalizeText(model.ItemCategory),
                PickupLocation = NormalizeText(model.PickupLocation),
                DeliveryLocation = NormalizeText(model.DeliveryLocation),
                ExpectedFinishAt = model.ExpectedFinishAt,
                PrivateDescription = NormalizeText(model.PrivateDescription)
            };

            TaskCreateWriteResult result = await _taskRepository.CreateAsync(request, cancellationToken);
            if (result.Result == TaskCreateResult.Success)
            {
                return new TaskOperationResult(true, string.Empty, result.TaskId);
            }

            if (result.Result == TaskCreateResult.AddressNotFound)
            {
                return new TaskOperationResult(false, "所选地址不存在，请先检查收货地址");
            }

            if (result.Result == TaskCreateResult.ServiceTypeUnavailable)
            {
                return new TaskOperationResult(false, "所选服务类型不可用，请重新选择");
            }

            if (result.Result == TaskCreateResult.NodeUnavailable)
            {
                return new TaskOperationResult(false, "所选交接节点不可用，请重新选择");
            }

            return new TaskOperationResult(false, "服务类型与交接节点不匹配，当前规则不允许发布该任务");
        }

        public async Task<TaskIndexViewModel> GetIndexAsync(
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TaskRecord> tasks = await _taskRepository.GetListAsync(
                currentUserId,
                cancellationToken);

            List<TaskListItemViewModel> items = new List<TaskListItemViewModel>();
            foreach (TaskRecord task in tasks)
            {
                items.Add(new TaskListItemViewModel
                {
                    TaskId = task.TaskId,
                    TaskKindDisplayName = DisplayNameService.GetTaskKindName(task.TaskKind),
                    ServiceName = task.ServiceName,
                    TaskTitle = task.TaskTitle,
                    TaskPrice = task.TaskPrice,
                    UrgentFlagDisplayName = DisplayNameService.GetUrgentFlagName(task.UrgentFlag),
                    TaskStatusDisplayName = DisplayNameService.GetTaskStatusName(task.TaskStatus),
                    AddressSummary = $"{task.ContactName} / {task.Campus} / {task.BuildingRoom}",
                    NodeName = task.NodeName,
                    CreatedAt = task.CreatedAt,
                    CanCancel = task.TaskStatus == "WAITING"
                });
            }

            return new TaskIndexViewModel
            {
                Tasks = items
            };
        }

        public async Task<string> CancelAsync(
            int taskId,
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            TaskCancelResult result = await _taskRepository.CancelAsync(
                taskId,
                currentUserId,
                cancellationToken);

            if (result == TaskCancelResult.Success)
            {
                return "任务已取消";
            }

            if (result == TaskCancelResult.InvalidState)
            {
                return "当前任务状态不允许取消，只有待接单任务可以取消";
            }

            return "任务不存在或你没有权限取消该任务";
        }

        private static string? NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}

public sealed class TaskOperationResult
{
    public TaskOperationResult(bool success, string errorMessage, int taskId = 0)
    {
        Success = success;
        ErrorMessage = errorMessage;
        TaskId = taskId;
    }

    public bool Success { get; }

    public string ErrorMessage { get; }

    public int TaskId { get; }
}
