



using Containers.Models;

namespace Devices.Persistence.Repositories;

public interface IDeviceRepository
{
    
    Task<Device?> GetDeviceByIdAsync(string id);
    Task<(Device? device, object? specificDetail, string? deviceType)> GetFullDeviceDataByIdAsync(string id);
    
    
    
    Task<IEnumerable<Device>> GetAllDevicesAsync();

    
    Task AddPersonalComputerAsync(Device device, PersonalComputer pcSpecifics);
    Task AddEmbeddedAsync(Device device, Embedded embeddedSpecifics);
    Task AddSmartwatchAsync(Device device, Smartwatch smartwatchSpecifics);

    
    Task<bool> UpdateDeviceAsync(Device device, object? specificDetails, string deviceType);
    
    
    Task<bool> DeleteDeviceAsync(string id);

    
    Task<bool> DeviceExistsAsync(string id);
}