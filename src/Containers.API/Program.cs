
using Containers.Application;
using Containers.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSingleton<IContainerService, ContainerService>(containerService => new ContainerService(connectionString));
builder.Services.AddScoped<IDeviceService>(provider =>
    new DeviceService(connectionString ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPut("api/devices/{id:int}", async (int id, UpdateDeviceRequest request, IDeviceService deviceService) =>
{
    try
    {
        var success = await deviceService.UpdateDeviceAsync(id, request);

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


app.MapPost("api/devices", async (CreateDeviceRequest request, IDeviceService deviceService) =>
    {
        try 
        {
            List<string> validationErrors = new(); 
            
            string normalizedDeviceType = request.DeviceType?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!new[] { "personalcomputer", "embedded", "smartwatch" }.Contains(normalizedDeviceType))
            {
                validationErrors.Add($"Invalid DeviceType '{request.DeviceType}'. Must be 'PersonalComputer', 'Embedded', or 'Smartwatch'.");
            }
            else
            {
                
                if (normalizedDeviceType == "embedded")
                {
                    
                    if (!string.IsNullOrWhiteSpace(request.IpAddress) && !System.Net.IPAddress.TryParse(request.IpAddress, out _))
                    {
                        validationErrors.Add("Invalid IP address format for Embedded device.");
                    }
                    
                    if (string.IsNullOrWhiteSpace(request.NetworkName))
                    {
                        validationErrors.Add("NetworkName is required for Embedded device.");
                    }
                }
                
                else if (normalizedDeviceType == "smartwatch")
                {
                    
                    if (request.BatteryPercentage.HasValue && (request.BatteryPercentage < 0 || request.BatteryPercentage > 100))
                    {
                        validationErrors.Add("BatteryPercentage must be between 0 and 100 for Smartwatch.");
                    }
                    
                    if (!request.BatteryPercentage.HasValue)
                    {
                         validationErrors.Add("BatteryPercentage is required for Smartwatch.");
                    }
                }
                 
                else if (normalizedDeviceType == "personalcomputer")
                {
                     if (string.IsNullOrWhiteSpace(request.OperationSystem))
                     {
                          validationErrors.Add("OperationSystem is required for PersonalComputer.");
                     }
                }
            }

            
            if (validationErrors.Any())
            {
                
                return Results.BadRequest(string.Join(" ", validationErrors));
                
            }
           


            
            var createdDevice = await deviceService.CreateDeviceAsync(request);

            if (createdDevice is not null)
            {
                return Results.CreatedAtRoute("GetDeviceById", new { id = createdDevice.Id }, createdDevice);
            }
            else
            {
                
                return Results.BadRequest("Failed to create device. Internal service issue?");
            }
        }
        
        catch (Exception e) 
        {
            return Results.Problem(e.Message);
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
    
app.MapGet("api/devices/{id:int}", async (int id, IDeviceService deviceService) =>
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


app.MapDelete("api/devices/{id:int}", async (int id, IDeviceService deviceService) =>
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
