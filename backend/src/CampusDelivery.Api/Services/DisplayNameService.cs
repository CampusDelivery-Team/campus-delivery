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
}
