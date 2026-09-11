using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services
{
    public sealed class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IAddressRepository _addressRepository;
        private readonly IServiceTypeRepository _serviceTypeRepository;
        private readonly INodeRepository _nodeRepository;
        private readonly IServiceNodeRuleRepository _serviceNodeRuleRepository;

        public TaskService(
            ITaskRepository taskRepository,
            IAddressRepository addressRepository,
            IServiceTypeRepository serviceTypeRepository,
            INodeRepository nodeRepository,
            IServiceNodeRuleRepository serviceNodeRuleRepository)
        {
            _taskRepository = taskRepository;
            _addressRepository = addressRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _nodeRepository = nodeRepository;
            _serviceNodeRuleRepository = serviceNodeRuleRepository;
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
            IReadOnlyList<ServiceNodeRule> serviceNodeRules =
                await _serviceNodeRuleRepository.GetAllAsync(cancellationToken);

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
                if (serviceType.TypeStatus != "ENABLED"
                    || !TaskKindCodes.TryFromServiceName(serviceType.ServiceName, out string taskKind))
                {
                    continue;
                }

                serviceTypeOptions.Add(new TaskOptionViewModel
                {
                    Value = serviceType.ServiceTypeId,
                    Text = $"{serviceType.ServiceName}（基础费 {serviceType.BasePrice:F2} 元）",
                    BasePrice = serviceType.BasePrice,
                    TaskKind = taskKind
                });
            }

            HashSet<int> availableServiceTypeIds = serviceTypeOptions
                .Select(option => option.Value)
                .ToHashSet();
            Dictionary<int, IReadOnlyList<int>> allowedServiceTypeIdsByNode = serviceNodeRules
                .Where(rule => rule.ServiceTypeStatus == "ENABLED"
                    && rule.NodeStatus == "NORMAL"
                    && availableServiceTypeIds.Contains(rule.ServiceTypeId))
                .GroupBy(rule => rule.NodeId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<int>)group
                        .Select(rule => rule.ServiceTypeId)
                        .Distinct()
                        .OrderBy(serviceTypeId => serviceTypeId)
                        .ToList());

            List<TaskOptionViewModel> nodeOptions = new List<TaskOptionViewModel>();
            foreach (Node node in nodes)
            {
                if (node.NodeStatus != "NORMAL"
                    || !allowedServiceTypeIdsByNode.TryGetValue(
                        node.NodeId,
                        out IReadOnlyList<int>? allowedServiceTypeIds))
                {
                    continue;
                }

                nodeOptions.Add(new TaskOptionViewModel
                {
                    Value = node.NodeId,
                    Text = $"{node.NodeName} · {DisplayNameService.GetNodeTypeName(node.NodeType)}",
                    AllowedServiceTypeIds = allowedServiceTypeIds
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
            IReadOnlyList<ServiceType> serviceTypes = await _serviceTypeRepository.GetAllAsync(cancellationToken);
            ServiceType? selectedServiceType = serviceTypes.SingleOrDefault(
                serviceType => serviceType.ServiceTypeId == model.ServiceTypeId
                    && serviceType.TypeStatus == "ENABLED");
            if (selectedServiceType == null
                || !TaskKindCodes.TryFromServiceName(selectedServiceType.ServiceName, out string taskKind))
            {
                return new TaskOperationResult(false, "所选任务类型不可用，请重新选择");
            }

            string? detailError = ValidateTaskDetails(model, taskKind);
            if (detailError != null)
            {
                return new TaskOperationResult(false, detailError);
            }

            TaskPublishRequest request = new TaskPublishRequest
            {
                PublisherUserId = userId,
                ServiceTypeId = model.ServiceTypeId!.Value,
                AddressNo = model.AddressNo!.Value,
                NodeId = model.NodeId!.Value,
                TaskTitle = model.TaskTitle.Trim(),
                ExtraAmount = model.ExtraAmount!.Value,
                UrgentFlag = model.UrgentFlag == "Y" ? "Y" : "N",
                TaskKind = taskKind,
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
                return new TaskOperationResult(false, "所选任务类型不可用，请重新选择");
            }

            if (result.Result == TaskCreateResult.PriceCalculationFailed)
            {
                return new TaskOperationResult(false, "基础费与附加费合计超出可保存金额，请降低附加费");
            }

            if (result.Result == TaskCreateResult.NodeUnavailable)
            {
                return new TaskOperationResult(false, "所选交接节点不可用，请重新选择");
            }

            return new TaskOperationResult(false, "任务类型与交接节点不匹配，当前规则不允许发布该任务");
        }

        private static string? ValidateTaskDetails(TaskCreateViewModel model, string taskKind)
        {
            if (taskKind == TaskKindCodes.Food && string.IsNullOrWhiteSpace(model.MerchantName))
            {
                return "请填写商家名称";
            }

            if (taskKind == TaskKindCodes.Express)
            {
                if (string.IsNullOrWhiteSpace(model.ExpressCompany))
                {
                    return "请填写快递公司";
                }

                if (string.IsNullOrWhiteSpace(model.WaybillNo))
                {
                    return "请填写物流单号";
                }

                if (string.IsNullOrWhiteSpace(model.PickupCode))
                {
                    return "请填写取件码";
                }
            }

            if (taskKind == TaskKindCodes.Private)
            {
                if (string.IsNullOrWhiteSpace(model.ItemCategory))
                {
                    return "请填写物品类别";
                }

                if (string.IsNullOrWhiteSpace(model.PickupLocation))
                {
                    return "请填写取货地点";
                }

                if (string.IsNullOrWhiteSpace(model.DeliveryLocation))
                {
                    return "请填写送达地点";
                }
            }

            return null;
        }

        public async Task<TaskIndexViewModel> GetIndexAsync(
            int currentUserId,
            bool includeAll,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TaskRecord> tasks = await _taskRepository.GetListAsync(
                currentUserId,
                includeAll,
                cancellationToken);

            List<TaskListItemViewModel> items = new List<TaskListItemViewModel>();
            foreach (TaskRecord task in tasks)
            {
                items.Add(new TaskListItemViewModel
                {
                    TaskId = task.TaskId,
                    PublisherUsername = task.PublisherUsername,
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
                IsAdminView = includeAll,
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

        public async Task<TaskDetailsViewModel?> GetDetailsAsync(
            int taskId,
            int currentUserId,
            bool includeAll,
            CancellationToken cancellationToken = default)
        {
            TaskDetailsRecord? record = await _taskRepository.GetDetailsAsync(
                taskId,
                currentUserId,
                includeAll,
                cancellationToken);
            if (record == null)
            {
                return null;
            }

            TaskRecord task = record.Task;
            TaskDetailsViewModel model = new TaskDetailsViewModel
            {
                TaskId = task.TaskId,
                RecordId = record.RecordId,
                CanReview = !includeAll && record.RecordId.HasValue && task.TaskStatus == "FINISHED",
                CanComplain = !includeAll && record.RecordId.HasValue && task.TaskStatus == "FINISHED",
                CanCancel = !includeAll && task.TaskStatus == "WAITING",
                RunnerRealName = record.RunnerRealName,
                RunnerCreditScore = record.RunnerCreditScore,
                RunnerWorkStatus = record.RunnerWorkStatus,
                PublisherUsername = task.PublisherUsername,
                TaskKindDisplayName = DisplayNameService.GetTaskKindName(task.TaskKind),
                ServiceName = task.ServiceName,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                UrgentFlagDisplayName = DisplayNameService.GetUrgentFlagName(task.UrgentFlag),
                TaskStatusDisplayName = DisplayNameService.GetTaskStatusName(task.TaskStatus),
                ContactName = task.ContactName,
                ContactPhone = task.ContactPhone,
                AddressSummary = $"{task.Campus} {task.BuildingRoom}".Trim(),
                NodeName = task.NodeName,
                CreatedAt = task.CreatedAt
            };

            void AddField(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    model.DetailFields.Add(new TaskDetailFieldViewModel { Label = label, Value = value });
                }
            }

            if (task.TaskKind == "FOOD")
            {
                AddField("商家名称", record.MerchantName);
                AddField("平台订单号", record.PlatformOrderNo);
                AddField("取餐说明", record.FoodPickupNote);
            }
            else if (task.TaskKind == "EXPRESS")
            {
                AddField("快递公司", record.ExpressCompany);
                AddField("运单号", record.WaybillNo);
                AddField("取件码", record.PickupCode);
                AddField("取件说明", record.ExpressPickupNote);
            }
            else if (task.TaskKind == "PRIVATE")
            {
                AddField("物品类别", record.ItemCategory);
                AddField("取件地点", record.PickupLocation);
                AddField("送达地点", record.DeliveryLocation);
                AddField("期望完成时间", record.ExpectedFinishAt?.ToString("yyyy-MM-dd HH:mm"));
                AddField("任务说明", record.PrivateDescription);
            }

            return model;
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
