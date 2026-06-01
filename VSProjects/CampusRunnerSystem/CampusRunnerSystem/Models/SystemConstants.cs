namespace CampusRunnerSystem.Models;

public static class SystemConstants
{
    public static class Roles
    {
        public const string User = "普通用户";
        public const string Runner = "跑腿员";
        public const string Admin = "管理员";
    }

    public static class AccountStatus
    {
        public const string Normal = "正常";
        public const string Disabled = "禁用";
    }

    public static class TaskStatus
    {
        public const string Created = "已创建";
        public const string Paid = "已支付";
        public const string WaitingForAccept = "待接单";
        public const string Accepted = "已接单";
        public const string PickedUp = "已取件";
        public const string Delivering = "配送中";
        public const string WaitingForConfirm = "待确认";
        public const string Completed = "已完成";
        public const string Canceled = "已取消";
        public const string Refunding = "退款中";
    }

    public static class YesNo
    {
        public const string Yes = "是";
        public const string No = "否";
    }

    public static class NodeStatus
    {
        public const string Normal = "正常";
        public const string Closed = "关闭";
    }

    public static class ServiceTypeStatus
    {
        public const string Enabled = "启用";
        public const string Disabled = "禁用";
    }
}
