using Containers.Models;

namespace Containers.Application;

public interface IDeviceService
{
    Task<IEnumerable<DeviceShortInfo>> GetAllDevicesShortInfoAsync();
    Task<DeviceDetailsResponse?> GetDeviceDetailsByIdAsync(string id);
    Task<IEnumerable<DeviceDetailsResponse>> GetAllDeviceDetailsAsync();
    Task<DeviceDetailsResponse?> CreateDeviceAsync(CreateDeviceRequest request);
    Task<bool> UpdateDeviceAsync(string id, UpdateDeviceRequest request);
    Task<bool> DeleteDeviceAsync(string id);
}