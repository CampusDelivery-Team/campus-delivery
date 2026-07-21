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

    public static string GetTaskStatusName(string value)
    {
        return value switch
        {
            "CREATED" => "已创建",
            "WAITING" => "待接单",
            "ASSIGNED" => "已接单",
            "PICKED_UP" => "已取件",
            "DELIVERING" => "配送中",
            "WAIT_CONFIRM" => "待确认",
            "FINISHED" => "已完成",
            "CANCELLED" => "已取消",
            "REFUNDING" => "退款中",
            "PAID" => "已支付",
            _ => value
        };
    }

    public static string GetPayMethodName(string value)
    {
        return value switch
        {
            "WECHAT" => "微信",
            "ALIPAY" => "支付宝",
            "CASH" => "现金",
            _ => value
        };
    }

    public static string GetPayStatusName(string value)
    {
        return value switch
        {
            "UNPAID" => "待付款",
            "PAID" => "已支付",
            "FAILED" => "支付异常",
            "REFUNDED" => "已退款",
            _ => value
        };
    }

    public static string GetRefundStatusName(string value)
    {
        return value switch
        {
            "PENDING" => "待审核",
            "APPROVED" => "已通过",
            "REJECTED" => "已拒绝",
            _ => value
        };
    }

    public static string GetUrgentFlagName(string value)
    {
        return value switch
        {
            "Y" => "加急",
            "N" => "普通",
            _ => value
        };
    }

    public static string GetTaskKindName(string value)
    {
        return value switch
        {
            "FOOD" => "外卖分发",
            "EXPRESS" => "快递代取",
            "PRIVATE" => "私人跑腿",
            _ => value
        };
    }
}
