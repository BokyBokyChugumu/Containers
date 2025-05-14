
using Devices.Persistence.Repositories; 
using System.Data;
using Containers.Application;
using Containers.Models;
using Microsoft.Data.SqlClient;

namespace Devices.Application;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository deviceRepository) 
    {
        _deviceRepository = deviceRepository;
    }
    
    
    private DeviceDetailsResponse? MapToDeviceDetailsResponse(Device? device, object? specificDetail, string? deviceType)
    {
        if (device == null || string.IsNullOrEmpty(deviceType))
        {
            return null;
        }

        var response = new DeviceDetailsResponse
        {
            Id = device.Id,
            Name = device.Name,
            IsEnabled = device.IsEnabled,
            RowVersion = device.RowVersion,
            DeviceType = deviceType
        };

        switch (deviceType.ToLowerInvariant())
        {
            case "personalcomputer" when specificDetail is PersonalComputer pc:
                response.OperationSystem = pc.OperationSystem;
                break;
            case "embedded" when specificDetail is Embedded emb:
                response.IpAddress = emb.IpAddress;
                response.NetworkName = emb.NetworkName;
                break;
            case "smartwatch" when specificDetail is Smartwatch sw:
                response.BatteryPercentage = sw.BatteryPercentage;
                break;
        }
        return response;
    }

     public async Task<DeviceDetailsResponse?> CreateDeviceAsync(CreateDeviceRequest request)
    {
        string newDeviceId = Guid.NewGuid().ToString();
        var baseDevice = new Device
        {
            Id = newDeviceId,
            Name = request.Name,
            IsEnabled = request.IsEnabled
        };

        try
        {
            string normalizedDeviceType = request.DeviceType?.Trim().ToLowerInvariant() ?? string.Empty;
            switch (normalizedDeviceType)
            {
                case "personalcomputer":
                    var pc = new PersonalComputer { DeviceId = newDeviceId, OperationSystem = request.OperationSystem };
                    await _deviceRepository.AddPersonalComputerAsync(baseDevice, pc);
                    break;
                case "embedded":
                    var emb = new Embedded { DeviceId = newDeviceId, IpAddress = request.IpAddress, NetworkName = request.NetworkName };
                    await _deviceRepository.AddEmbeddedAsync(baseDevice, emb);
                    break;
                case "smartwatch":
                    var sw = new Smartwatch { DeviceId = newDeviceId, BatteryPercentage = request.BatteryPercentage };
                    await _deviceRepository.AddSmartwatchAsync(baseDevice, sw);
                    break;
                default:
                    throw new ArgumentException("Invalid device type provided.", nameof(request.DeviceType));
            }
            
            var (createdBaseDevice, createdSpecificDetail, createdDeviceType) = 
                await _deviceRepository.GetFullDeviceDataByIdAsync(newDeviceId);
            
            return MapToDeviceDetailsResponse(createdBaseDevice, createdSpecificDetail, createdDeviceType);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
             Console.WriteLine($"Error creating device (ID possibly exists): {ex.Message}");
             return null; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating device: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateDeviceAsync(string id, UpdateDeviceRequest request)
    {
        var (existingBaseDevice, existingSpecificDetail, existingDeviceType) = 
            await _deviceRepository.GetFullDeviceDataByIdAsync(id);

        if (existingBaseDevice == null || existingDeviceType == null)
        {
            return false; 
        }

        var deviceToUpdate = new Device
        {
            Id = id,
            Name = request.Name,
            IsEnabled = request.IsEnabled,
            RowVersion = request.RowVersion 
        };

        object? specificDetailsToUpdate = null;
        switch (existingDeviceType.ToLowerInvariant())
        {
            case "personalcomputer":
                specificDetailsToUpdate = new PersonalComputer { Id = ((PersonalComputer)existingSpecificDetail!).Id, DeviceId = id, OperationSystem = request.OperationSystem };
                break;
            case "embedded":
                specificDetailsToUpdate = new Embedded { Id = ((Embedded)existingSpecificDetail!).Id, DeviceId = id, IpAddress = request.IpAddress, NetworkName = request.NetworkName };
                break;
            case "smartwatch":
                specificDetailsToUpdate = new Smartwatch { Id = ((Smartwatch)existingSpecificDetail!).Id, DeviceId = id, BatteryPercentage = request.BatteryPercentage };
                break;
        }

        try
        {
            return await _deviceRepository.UpdateDeviceAsync(deviceToUpdate, specificDetailsToUpdate, existingDeviceType);
        }
        catch (DBConcurrencyException)
        {
            throw;
        }
    }
    
    public async Task<DeviceDetailsResponse?> GetDeviceDetailsByIdAsync(string id)
    {
        
        var (baseDevice, specificDetail, determinedDeviceType) = await _deviceRepository.GetFullDeviceDataByIdAsync(id);

        if (baseDevice == null || determinedDeviceType == null)
        {
            return null; 
        }

        
        var responseDto = new DeviceDetailsResponse
        {
            Id = baseDevice.Id,
            Name = baseDevice.Name,
            IsEnabled = baseDevice.IsEnabled,
            RowVersion = baseDevice.RowVersion,
            DeviceType = determinedDeviceType
        };

        switch (determinedDeviceType.ToLowerInvariant())
        {
            case "personalcomputer" when specificDetail is PersonalComputer pc:
                responseDto.OperationSystem = pc.OperationSystem;
                break;
            case "embedded" when specificDetail is Embedded emb:
                responseDto.IpAddress = emb.IpAddress;
                responseDto.NetworkName = emb.NetworkName;
                break;
            case "smartwatch" when specificDetail is Smartwatch sw:
                responseDto.BatteryPercentage = sw.BatteryPercentage;
                break;
        }
        return responseDto;
    }

    public async Task<IEnumerable<DeviceShortInfo>> GetAllDevicesShortInfoAsync()
    {
        var devices = await _deviceRepository.GetAllDevicesAsync();
        
        return devices.Select(d => new DeviceShortInfo
        {
            Id = d.Id,
            Name = d.Name,
            IsEnabled = d.IsEnabled
            
        }).ToList();
    }
    
    public async Task<IEnumerable<DeviceDetailsResponse>> GetAllDeviceDetailsAsync()
    {
        var devicesFromRepo = await _deviceRepository.GetAllDevicesAsync(); 
        var detailedDevices = new List<DeviceDetailsResponse>();

        foreach (var baseDevice in devicesFromRepo)
        {
            
            var details = await GetDeviceDetailsByIdAsync(baseDevice.Id);
            if (details != null)
            {
                detailedDevices.Add(details);
            }
        }
        return detailedDevices;
        
    }

    public async Task<bool> DeleteDeviceAsync(string id)
    {
        
        if (!await _deviceRepository.DeviceExistsAsync(id)) {
            return false;
        }
        return await _deviceRepository.DeleteDeviceAsync(id);
    }
}