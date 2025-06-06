﻿namespace Containers.Application;

public class CreateDeviceRequest
{
    public required string Name { get; set; }
    public bool IsEnabled { get; set; } = true;
    public required string DeviceType { get; set; }
    
    public string? OperationSystem { get; set; }
    public string? IpAddress { get; set; }
    public string? NetworkName { get; set; }
    public int? BatteryPercentage { get; set; }
}