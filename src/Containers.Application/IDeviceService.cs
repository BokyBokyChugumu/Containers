using Containers.Models;

namespace Containers.Application;

public interface IDeviceService
{
    Task<IEnumerable<DeviceShortInfo>> GetAllDevicesShortInfoAsync();
    Task<DeviceDetailsResponse?> GetDeviceDetailsByIdAsync(int id);
    Task<IEnumerable<DeviceDetailsResponse>> GetAllDeviceDetailsAsync();
    Task<Device?> CreateDeviceAsync(CreateDeviceRequest request);
    Task<bool> UpdateDeviceAsync(int id, UpdateDeviceRequest request);
    Task<bool> DeleteDeviceAsync(int id);
}