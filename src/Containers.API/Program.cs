
using System.Data;
using Containers.Application;
using Containers.Models;
using Devices.Application;
using Devices.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPut("api/devices/{id}", async (string id, UpdateDeviceRequest request, IDeviceService deviceService) =>
{
    
    if (request.RowVersion == null)
    {
        return Results.BadRequest(new { message = "RowVersion is required for updates." });
    }

    try
    {
        var success = await deviceService.UpdateDeviceAsync(id, request);
        if (success)
        {
            return Results.NoContent(); 
        }
        else
        {
            
            return Results.NotFound(new { message = $"Device with ID '{id}' not found." });
        }
    }
    catch (DBConcurrencyException)
    {
        
        return Results.Conflict(new { message = "Update conflict. The device has been modified by another user. Please refresh and try again." }); // 409
    }
    catch (ArgumentException argEx)
    {
        return Results.BadRequest(new { message = argEx.Message });
    }
    catch (Exception e)
    {
        Console.WriteLine($"Unhandled error on PUT /api/devices/{id}: {e.Message}");
        return Results.Problem("An unexpected error occurred while updating the device.");
    }
});


app.MapPost("api/devices", async (CreateDeviceRequest request, IDeviceService deviceService) =>
    {
        
        try
        {
            var createdDeviceDetails = await deviceService.CreateDeviceAsync(request);
            if (createdDeviceDetails != null)
            {
                return Results.CreatedAtRoute("GetDeviceById", new { id = createdDeviceDetails.Id }, createdDeviceDetails);
            }
            
            return Results.Conflict(new { message = "Device with the proposed ID might already exist or another creation error occurred." });
        }
        catch (ArgumentException argEx)
        {
            return Results.BadRequest(new { message = argEx.Message });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unhandled error on POST /api/devices: {e.Message}");
            return Results.Problem("An unexpected error occurred while creating the device.");
        }
    })
    .WithName("CreateDevice");

app.MapGet("api/devices/details", async (IDeviceService deviceService) =>
{
    try
    {
        var devices = await deviceService.GetAllDevicesShortInfoAsync();
        return Results.Ok(devices);
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message);
    }
});
    
app.MapGet("api/devices/{id}", async (string id, IDeviceService deviceService) =>
    {
        try
        {
            
            var device = await deviceService.GetDeviceDetailsByIdAsync(id);

            
            if (device is not null)
            {
                
                return Results.Ok(device);
            }
            else
            {
                
                return Results.NotFound();
            }
        }
        catch (Exception e)
        {
            
            return Results.Problem(e.Message);
        }
    })
    .WithName("GetDeviceById");


app.MapDelete("api/devices/{id}", async (string id, IDeviceService deviceService) =>
{
    try
    {
        var success = await deviceService.DeleteDeviceAsync(id);
        if (success)
        {
            
            return Results.Ok();
        }
        else
        {
            
            return Results.NotFound();
        }
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message);
    }
});

app.Run();
