

using Microsoft.Data.SqlClient;
using System.Data;

using Containers.Models;
using Microsoft.Extensions.Configuration;

namespace Devices.Persistence.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly string _connectionString;

    public DeviceRepository(IConfiguration configuration) 
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private void AddParameterWithValue(SqlCommand command, string parameterName, object? value)
    {
        command.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
    }


    public async Task AddPersonalComputerAsync(Device device, PersonalComputer pcSpecifics)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("AddPersonalComputer", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameterWithValue(command, "@DeviceId", device.Id); 
        AddParameterWithValue(command, "@Name", device.Name);
        AddParameterWithValue(command, "@IsEnabled", device.IsEnabled);
        AddParameterWithValue(command, "@OperationSystem", pcSpecifics.OperationSystem);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task AddEmbeddedAsync(Device device, Embedded embeddedSpecifics)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("AddEmbedded", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        AddParameterWithValue(command, "@DeviceId", device.Id);
        AddParameterWithValue(command, "@Name", device.Name);
        AddParameterWithValue(command, "@IsEnabled", device.IsEnabled);
        AddParameterWithValue(command, "@IpAddress", embeddedSpecifics.IpAddress);
        AddParameterWithValue(command, "@NetworkName", embeddedSpecifics.NetworkName);
        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task AddSmartwatchAsync(Device device, Smartwatch smartwatchSpecifics)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("AddSmartwatch", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        AddParameterWithValue(command, "@DeviceId", device.Id);
        AddParameterWithValue(command, "@Name", device.Name);
        AddParameterWithValue(command, "@IsEnabled", device.IsEnabled);
        AddParameterWithValue(command, "@BatteryPercentage", smartwatchSpecifics.BatteryPercentage);
        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Device>> GetAllDevicesAsync()
    {
        var devices = new List<Device>();
        
        const string queryString = "SELECT Id, Name, IsEnabled, RowVersion FROM Device ORDER BY Name;";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(queryString, connection);
        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            devices.Add(new Device
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                RowVersion = (byte[])reader.GetValue(reader.GetOrdinal("RowVersion"))
            });
        }
        return devices;
    }
    
    public async Task<Device?> GetDeviceByIdAsync(string id)
    {
        Device? device = null;
        
        const string queryString = "SELECT Id, Name, IsEnabled, RowVersion FROM Device WHERE Id = @Id;";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(queryString, connection);
        AddParameterWithValue(command, "@Id", id);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            device = new Device
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                RowVersion = (byte[])reader.GetValue(reader.GetOrdinal("RowVersion")) // Читаем RowVersion
            };
        }
        return device;
    }

    public async Task<(Device? device, object? specificDetail, string? deviceType)> GetFullDeviceDataByIdAsync(string id)
{
    Device? baseDevice = null;
    object? specificDetail = null;
    string? determinedDeviceType = null;

    
    const string queryString = @"
        SELECT
            d.Id AS DeviceId, d.Name AS DeviceName, d.IsEnabled AS DeviceIsEnabled, d.RowVersion AS DeviceRowVersion,
            pc.Id AS PcId, pc.OperationSystem,
            e.Id AS EmbId, e.IpAddress, e.NetworkName,
            sw.Id AS SwId, sw.BatteryPercentage
        FROM Device d
        LEFT JOIN PersonalComputer pc ON d.Id = pc.DeviceId
        LEFT JOIN Embedded e ON d.Id = e.DeviceId
        LEFT JOIN Smartwatch sw ON d.Id = sw.DeviceId
        WHERE d.Id = @DeviceIdParam;";
    try
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(queryString, connection);
        AddParameterWithValue(command, "@DeviceIdParam", id);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            baseDevice = new Device
            {
                Id = reader.GetString(reader.GetOrdinal("DeviceId")),
                Name = reader.GetString(reader.GetOrdinal("DeviceName")),
                IsEnabled = reader.GetBoolean(reader.GetOrdinal("DeviceIsEnabled")),
                RowVersion = (byte[])reader.GetValue(reader.GetOrdinal("DeviceRowVersion"))
            };

            if (!reader.IsDBNull(reader.GetOrdinal("PcId")))
            {
                determinedDeviceType = "PersonalComputer";
                specificDetail = new PersonalComputer
                {
                    Id = reader.GetInt32(reader.GetOrdinal("PcId")),
                    OperationSystem = reader.GetString(reader.GetOrdinal("OperationSystem")),
                    DeviceId = baseDevice.Id
                };
            }
            else if (!reader.IsDBNull(reader.GetOrdinal("EmbId")))
            {
                determinedDeviceType = "Embedded";
                specificDetail = new Embedded
                {
                    Id = reader.GetInt32(reader.GetOrdinal("EmbId")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                    NetworkName = reader.IsDBNull(reader.GetOrdinal("NetworkName")) ? null : reader.GetString(reader.GetOrdinal("NetworkName")),
                    DeviceId = baseDevice.Id
                };
            }
            else if (!reader.IsDBNull(reader.GetOrdinal("SwId")))
            {
                determinedDeviceType = "Smartwatch";
                specificDetail = new Smartwatch
                {
                    Id = reader.GetInt32(reader.GetOrdinal("SwId")),
                    BatteryPercentage = reader.IsDBNull(reader.GetOrdinal("BatteryPercentage")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("BatteryPercentage")),
                    DeviceId = baseDevice.Id
                };
            }
        }
    }
    catch (SqlException ex)
    {
        Console.WriteLine($"SQL Error in GetFullDeviceDataByIdAsync for ID '{id}': {ex.Message}");
        throw;
    }
    return (baseDevice, specificDetail, determinedDeviceType);
}
    

    public async Task<bool> UpdateDeviceAsync(Device deviceToUpdate, object? specificDetails, string deviceType)
    {
        int rowsAffectedDevice;
        int rowsAffectedSpecific = 0;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {

            const string updateDeviceSql = @"
                UPDATE Device
                SET Name = @Name, IsEnabled = @IsEnabled
                OUTPUT INSERTED.RowVersion -- Получаем новую RowVersion
                WHERE Id = @Id AND RowVersion = @RowVersion;"; 

            using (var commandDevice = new SqlCommand(updateDeviceSql, connection, transaction))
            {
                AddParameterWithValue(commandDevice, "@Name", deviceToUpdate.Name);
                AddParameterWithValue(commandDevice, "@IsEnabled", deviceToUpdate.IsEnabled);
                AddParameterWithValue(commandDevice, "@Id", deviceToUpdate.Id);
                AddParameterWithValue(commandDevice, "@RowVersion", deviceToUpdate.RowVersion); 

                var newRowVersion = await commandDevice.ExecuteScalarAsync(); 
                if (newRowVersion == null || newRowVersion == DBNull.Value)
                {
                    
                    await transaction.RollbackAsync();
                    
                    throw new DBConcurrencyException("Update failed due to a concurrency conflict or record not found.");
                }
                
            }

            
            string updateSpecificSql = "";
            SqlCommand? commandSpecific = null;

            switch (deviceType.ToLowerInvariant())
            {
                case "personalcomputer" when specificDetails is PersonalComputer pc:
                    updateSpecificSql = "UPDATE PersonalComputer SET OperationSystem = @OperationSystem WHERE DeviceId = @DeviceId;";
                    commandSpecific = new SqlCommand(updateSpecificSql, connection, transaction);
                    AddParameterWithValue(commandSpecific, "@OperationSystem", pc.OperationSystem);
                    break;
                case "embedded" when specificDetails is Embedded emb:
                    updateSpecificSql = "UPDATE Embedded SET IpAddress = @IpAddress, NetworkName = @NetworkName WHERE DeviceId = @DeviceId;";
                    commandSpecific = new SqlCommand(updateSpecificSql, connection, transaction);
                    AddParameterWithValue(commandSpecific, "@IpAddress", emb.IpAddress);
                    AddParameterWithValue(commandSpecific, "@NetworkName", emb.NetworkName);
                    break;
                case "smartwatch" when specificDetails is Smartwatch sw:
                    updateSpecificSql = "UPDATE Smartwatch SET BatteryPercentage = @BatteryPercentage WHERE DeviceId = @DeviceId;";
                    commandSpecific = new SqlCommand(updateSpecificSql, connection, transaction);
                    AddParameterWithValue(commandSpecific, "@BatteryPercentage", sw.BatteryPercentage);
                    break;
            }

            if (commandSpecific != null)
            {
                AddParameterWithValue(commandSpecific, "@DeviceId", deviceToUpdate.Id);
                rowsAffectedSpecific = await commandSpecific.ExecuteNonQueryAsync();

            }

            await transaction.CommitAsync(); 
            return true;
        }
        catch (DBConcurrencyException) 
        {
             if (transaction.Connection != null) await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            if (transaction.Connection != null) await transaction.RollbackAsync();
            Console.WriteLine($"Error in UpdateDeviceAsync transaction for ID '{deviceToUpdate.Id}': {ex.Message}");
            throw;
        }
    }

    
    public async Task<bool> DeleteDeviceAsync(string id)
    {
        int rowsAffected;
        const string deleteSql = "DELETE FROM Device WHERE Id = @Id;";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var command = new SqlCommand(deleteSql, connection, transaction))
            {
                AddParameterWithValue(command, "@Id", id);
                rowsAffected = await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync(); 
        }
        catch (Exception ex)
        {
            if (transaction.Connection != null) await transaction.RollbackAsync();
            Console.WriteLine($"SQL Error in DeleteDeviceAsync for ID '{id}': {ex.Message}");
            throw;
        }
        return rowsAffected > 0;
    }

    public async Task<bool> DeviceExistsAsync(string id)
    {
        const string queryString = "SELECT 1 FROM Device WHERE Id = @Id;";
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(queryString, connection);
        AddParameterWithValue(command, "@Id", id);
        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }
}