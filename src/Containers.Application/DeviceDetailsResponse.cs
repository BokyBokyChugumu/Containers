namespace Containers.Application;

public class DeviceDetailsResponse
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public bool IsEnabled { get; set; }
    public byte[]? RowVersion { get; set; }
    public required string DeviceType { get; set; }
    
    public string? OperationSystem { get; set; }

    
    public string? IpAddress { get; set; }
    public string? NetworkName { get; set; }

    
    public int? BatteryPercentage { get; set; }
}