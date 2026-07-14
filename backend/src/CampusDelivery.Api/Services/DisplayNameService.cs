namespace CampusDelivery.Api.Services;

public static class DisplayNameService
{
    public static string GetNodeTypeName(string value)
    {
        return value switch
        {
            "GATE" => "校门",
            "STATION" => "驿站",
            "DISTRIBUTION" => "分发点",
            _ => value
        };
    }

    public static string GetNodeStatusName(string value)
    {
        return value switch
        {
            "NORMAL" => "正常",
            "CLOSED" => "关闭",
            _ => value
        };
    }

    public static string GetServiceTypeStatusName(string value)
    {
        return value switch
        {
            "ENABLED" => "启用",
            "DISABLED" => "停用",
            _ => value
        };
    }

    public static string GetRunnerAuditStatusName(string value)
    {
        return value switch
        {
            "PENDING" => "待审核",
            "APPROVED" => "已通过",
            "REJECTED" => "已拒绝",
            _ => value
        };
    }

    public static string GetRunnerWorkStatusName(string value)
    {
        return value switch
        {
            "FREE" => "可接单",
            "BUSY" => "配送中",
            "OFFLINE" => "离线",
            _ => value
        };
    }

    public static string GetAccountStatusName(string value)
    {
        return value switch
        {
            "NORMAL" => "正常",
            "BLOCKED" => "已封控",
            "CANCELLED" => "已注销",
            _ => value
        };
    }
<<<<<<< HEAD
=======

    public static string GetTaskStatusName(string value)
    {
        return value switch
        {
            "CREATED" => "草稿创建",
            "PAID" => "已付款",
            "WAITING" => "待接单",
            "ASSIGNED" => "已分配",
            "PICKED_UP" => "已取件",
            "DELIVERING" => "配送中",
            "WAIT_CONFIRM" => "待确认收货",
            "FINISHED" => "已完成",
            "CANCELLED" => "已取消",
            "REFUNDING" => "退款中",
            _ => value
        };
    }

    public static string GetAssignOperationTypeName(string value)
    {
        return value switch
        {
            "SELF" => "跑腿员抢单",
            "ADMIN" => "管理员指派",
            "REASSIGN" => "异常重派",
            _ => value
        };
    }
>>>>>>> 04d69785da5b5263f868b722615079167137c30c
}
