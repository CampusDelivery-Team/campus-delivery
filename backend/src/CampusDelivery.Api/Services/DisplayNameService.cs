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
}
