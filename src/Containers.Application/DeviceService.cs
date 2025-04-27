using Containers.Models;
using Microsoft.Data.SqlClient;

namespace Containers.Application;

public class DeviceService : IDeviceService
{
    private readonly string _connectionString;

    public DeviceService(string connectionString)
    {
        _connectionString = !string.IsNullOrEmpty(connectionString)
            ? connectionString
            : throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
    }

    
    private void AddParameterWithValue(SqlCommand command, string parameterName, object? value)
    {
        
        command.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
    }

    
    public async Task<IEnumerable<DeviceShortInfo>> GetAllDevicesShortInfoAsync()
    {
        var devices = new List<DeviceShortInfo>();
        const string queryString = "SELECT Id, Name, IsEnabled FROM Device ORDER BY Name;";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(queryString, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                devices.Add(new DeviceShortInfo
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")), // Используем имена
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
                });
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Error in GetAllDevicesShortInfoAsync: {ex.Message}");
            throw;
        }
        return devices;
    }

    
    public async Task<DeviceDetailsResponse?> GetDeviceDetailsByIdAsync(int id)
    {
        DeviceDetailsResponse? deviceDetails = null;
        
        const string queryString = @"
            SELECT
                d.Id, d.Name, d.IsEnabled,
                pc.OperationSystem,
                e.IpAddress, e.NetworkName,
                sw.BatteryPercentage,
                CASE
                    WHEN pc.Id IS NOT NULL THEN 'PersonalComputer'
                    WHEN e.Id IS NOT NULL THEN 'Embedded'
                    WHEN sw.Id IS NOT NULL THEN 'Smartwatch'
                    ELSE 'Unknown'
                END AS DeviceType
            FROM Device d
            LEFT JOIN PersonalComputer pc ON d.Id = pc.DeviceId
            LEFT JOIN Embedded e ON d.Id = e.DeviceId
            LEFT JOIN Smartwatch sw ON d.Id = sw.DeviceId
            WHERE d.Id = @DeviceId;";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(queryString, connection);
            AddParameterWithValue(command, "@DeviceId", id); // Используем helper

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                deviceDetails = new DeviceDetailsResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                    DeviceType = reader.GetString(reader.GetOrdinal("DeviceType")),
                    OperationSystem = reader.IsDBNull(reader.GetOrdinal("OperationSystem")) ? null : reader.GetString(reader.GetOrdinal("OperationSystem")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                    NetworkName = reader.IsDBNull(reader.GetOrdinal("NetworkName")) ? null : reader.GetString(reader.GetOrdinal("NetworkName")),
                    BatteryPercentage = reader.IsDBNull(reader.GetOrdinal("BatteryPercentage")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("BatteryPercentage"))
                };
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Error in GetDeviceDetailsByIdAsync: {ex.Message}");
            throw;
        }
        return deviceDetails;
    }

    
    public async Task<Device?> CreateDeviceAsync(CreateDeviceRequest request)
    {
        
        string normalizedDeviceType = request.DeviceType?.Trim().ToLowerInvariant() ?? "";
        if (!new[] { "personalcomputer", "embedded", "smartwatch" }.Contains(normalizedDeviceType))
        {
            Console.WriteLine($"Error: Invalid DeviceType '{request.DeviceType}' provided.");
            
            throw new ArgumentException($"Invalid DeviceType: {request.DeviceType}");
            
        }

        Device? createdDevice = null;
        int newDeviceId = -1;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        using var transaction = connection.BeginTransaction();

        try
        {
            
            const string insertDeviceSql = "INSERT INTO Device (Name, IsEnabled) OUTPUT INSERTED.Id VALUES (@Name, @IsEnabled);";
            using (var commandDevice = new SqlCommand(insertDeviceSql, connection, transaction))
            {
                AddParameterWithValue(commandDevice, "@Name", request.Name);
                AddParameterWithValue(commandDevice, "@IsEnabled", request.IsEnabled);

                newDeviceId = (int)(await commandDevice.ExecuteScalarAsync() ?? throw new Exception("Failed to retrieve new Device ID."));
            }

            
            string insertSpecificSql = "";
            using (var commandSpecific = new SqlCommand { Connection = connection, Transaction = transaction })
            {
                 AddParameterWithValue(commandSpecific, "@DeviceId", newDeviceId);

                switch (normalizedDeviceType)
                {
                    case "personalcomputer":
                        insertSpecificSql = "INSERT INTO PersonalComputer (DeviceId, OperationSystem) VALUES (@DeviceId, @OperationSystem);";
                        AddParameterWithValue(commandSpecific, "@OperationSystem", request.OperationSystem);
                        break;
                    case "embedded":
                        insertSpecificSql = "INSERT INTO Embedded (DeviceId, IpAddress, NetworkName) VALUES (@DeviceId, @IpAddress, @NetworkName);";
                        AddParameterWithValue(commandSpecific, "@IpAddress", request.IpAddress);
                        AddParameterWithValue(commandSpecific, "@NetworkName", request.NetworkName);
                        break;
                    case "smartwatch":
                        insertSpecificSql = "INSERT INTO Smartwatch (DeviceId, BatteryPercentage) VALUES (@DeviceId, @BatteryPercentage);";
                        AddParameterWithValue(commandSpecific, "@BatteryPercentage", request.BatteryPercentage);
                        break;
                }
                commandSpecific.CommandText = insertSpecificSql;
                int specificRowsAffected = await commandSpecific.ExecuteNonQueryAsync();

                 if (specificRowsAffected <= 0) {
                     throw new Exception($"Failed to insert details for {request.DeviceType}.");
                 }
            }

            
            await transaction.CommitAsync();
            createdDevice = new Device { Id = newDeviceId, Name = request.Name, IsEnabled = request.IsEnabled };
        }
        catch (Exception ex)
        {
            
            await transaction.RollbackAsync();
            Console.WriteLine($"Error in CreateDeviceAsync transaction: {ex.Message}");
            throw;
        }

        return createdDevice;
    }
    
     public async Task<IEnumerable<DeviceDetailsResponse>> GetAllDeviceDetailsAsync()
    {
        var allDeviceDetails = new List<DeviceDetailsResponse>();
        
        const string queryString = @"
            SELECT
                d.Id, d.Name, d.IsEnabled,
                pc.OperationSystem,
                e.IpAddress, e.NetworkName,
                sw.BatteryPercentage,
                CASE
                    WHEN pc.Id IS NOT NULL THEN 'PersonalComputer'
                    WHEN e.Id IS NOT NULL THEN 'Embedded'
                    WHEN sw.Id IS NOT NULL THEN 'Smartwatch'
                    ELSE 'Unknown' -- Или просто 'Device'
                END AS DeviceType
            FROM Device d
            LEFT JOIN PersonalComputer pc ON d.Id = pc.DeviceId
            LEFT JOIN Embedded e ON d.Id = e.DeviceId
            LEFT JOIN Smartwatch sw ON d.Id = sw.DeviceId
            ORDER BY d.Id; -- Добавим сортировку для предсказуемого порядка
            ";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(queryString, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync()) 
            {
                 var deviceDetails = new DeviceDetailsResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                    DeviceType = reader.GetString(reader.GetOrdinal("DeviceType")),
                    OperationSystem = reader.IsDBNull(reader.GetOrdinal("OperationSystem")) ? null : reader.GetString(reader.GetOrdinal("OperationSystem")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                    NetworkName = reader.IsDBNull(reader.GetOrdinal("NetworkName")) ? null : reader.GetString(reader.GetOrdinal("NetworkName")),
                    BatteryPercentage = reader.IsDBNull(reader.GetOrdinal("BatteryPercentage")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("BatteryPercentage"))
                };
                allDeviceDetails.Add(deviceDetails);
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Error in GetAllDeviceDetailsAsync: {ex.Message}");
            throw; 
        }
        return allDeviceDetails;
    }
    
    
    public async Task<bool> UpdateDeviceAsync(int id, UpdateDeviceRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            
            var details = await GetDeviceDetailsByIdAsync(id); 
            if (details == null) return false;

            
            const string updateDeviceSql = "UPDATE Device SET Name = @Name, IsEnabled = @IsEnabled WHERE Id = @Id;";
            using (var commandDevice = new SqlCommand(updateDeviceSql, connection, transaction))
            {
                AddParameterWithValue(commandDevice, "@Name", request.Name);
                AddParameterWithValue(commandDevice, "@IsEnabled", request.IsEnabled);
                AddParameterWithValue(commandDevice, "@Id", id);
                await commandDevice.ExecuteNonQueryAsync();
            }

            
            string updateSpecificSql = "";
            using (var commandSpecific = new SqlCommand { Connection = connection, Transaction = transaction })
            {
                AddParameterWithValue(commandSpecific, "@DeviceId", id);

                switch (details.DeviceType.ToLowerInvariant())
                {
                     case "personalcomputer":
                        updateSpecificSql = "UPDATE PersonalComputer SET OperationSystem = @OperationSystem WHERE DeviceId = @DeviceId;";
                        AddParameterWithValue(commandSpecific, "@OperationSystem", request.OperationSystem);
                        break;
                    case "embedded":
                        updateSpecificSql = "UPDATE Embedded SET IpAddress = @IpAddress, NetworkName = @NetworkName WHERE DeviceId = @DeviceId;";
                        AddParameterWithValue(commandSpecific, "@IpAddress", request.IpAddress);
                        AddParameterWithValue(commandSpecific, "@NetworkName", request.NetworkName);
                        break;
                    case "smartwatch":
                        updateSpecificSql = "UPDATE Smartwatch SET BatteryPercentage = @BatteryPercentage WHERE DeviceId = @DeviceId;";
                        AddParameterWithValue(commandSpecific, "@BatteryPercentage", request.BatteryPercentage);
                        break;
                     default:
                         updateSpecificSql = "";
                         break;
                }

                 if (!string.IsNullOrEmpty(updateSpecificSql))
                 {
                    commandSpecific.CommandText = updateSpecificSql;
                    await commandSpecific.ExecuteNonQueryAsync();
                 }
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error in UpdateDeviceAsync transaction: {ex.Message}");
            throw; 
        }
    }

    
    public async Task<bool> DeleteDeviceAsync(int id)
    {
        
        const string deleteSql = "DELETE FROM Device WHERE Id = @Id;";
        int rowsAffected = 0;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(deleteSql, connection);
            AddParameterWithValue(command, "@Id", id);

            await connection.OpenAsync();
            rowsAffected = await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
             Console.WriteLine($"SQL Error in DeleteDeviceAsync: {ex.Message}");
             throw;
        }
        return rowsAffected > 0;
    }
}