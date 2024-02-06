# Microsoft Orleans dotNet8 WebAPI(Server) Blazor(Client)

## 1. Create the Server application (.Net 8 WebAPI)

### 1.1. Configure the middleware (program.cs)

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;

try
{
    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureServices(services =>
            {
                services.AddControllers();
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                });
                // Add CORS services here
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowSpecificOrigin",
                        builder =>
                        {
                            builder.WithOrigins("https://localhost:7013") // Adjust this as necessary
                                   .AllowAnyHeader()
                                   .AllowAnyMethod();
                        });
                });
            });

            webBuilder.Configure((context, app) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    app.UseSwagger();
                    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
                }

                app.UseRouting();

                // Apply CORS policy here
                app.UseCors("AllowSpecificOrigin");

                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            });
        })
        .UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering()
                       .Configure<ClusterOptions>(options =>
                       {
                           options.ClusterId = "dev";
                           options.ServiceId = "OrleansBasics";
                       })
                       .ConfigureLogging(logging => logging.AddConsole());
        });

    using var host = builder.Build();
    Console.WriteLine("\n\nPress Enter to terminate...\n\n");
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
```

### 1.2. Load the project dependencies (OrleansWebAPIServer.csproj)

```
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.1" />
    <PackageReference Include="Microsoft.Orleans.Core" Version="8.0.0" />
    <PackageReference Include="Microsoft.Orleans.Core.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Orleans.Server" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
 </ItemGroup>
```

### 1.3. Create the Grain Interfaces (IHello.cs)

```csharp
﻿using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansWebAPIServer.GrainsIntefaces
{
    public interface IHello : IGrainWithIntegerKey
    {
        ValueTask<string> SayHello(string greeting);
    }
}
```

### 1.4. Create the Grains (HelloGrain.cs)

```csharp
﻿using OrleansWebAPIServer.GrainsIntefaces;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansWebAPIServer.Grains
{
    public class HelloGrain : Grain, IHello
    {
        private readonly ILogger _logger;

        public HelloGrain(ILogger<HelloGrain> logger)
        {
            _logger = logger;
        }

        public ValueTask<string> SayHello(string greeting)
        {
            _logger.LogInformation(
            "SayHello message received: greeting = '{Greeting}'", greeting);

            return ValueTask.FromResult(
                $"""
            Client said: '{greeting}', so HelloGrain says: Hello!
            """);
        }
    }
}
```

### 1.5. Create the Models (HelloModel.cs)

```csharp
﻿using System.ComponentModel.DataAnnotations;

namespace OrleansWebAPIServer.Models
{
    public class GreetingRequest
    {
        [Required]
        public string Greeting { get; set; }
    }
}
```

### 1.6. Create the Controllers (HelloController.cs)

```csharp
﻿using Microsoft.AspNetCore.Mvc;
using Orleans;
using OrleansWebAPIServer.GrainsIntefaces;
using OrleansWebAPIServer.Models;
using System.Threading.Tasks;

namespace OrleansWebAPIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloController : ControllerBase
    {
        private readonly IClusterClient _client;

        public HelloController(IClusterClient client)
        {
            _client = client;
        }

        [HttpGet]
        public async Task<IActionResult> SayHello([FromQuery] GreetingRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var grain = _client.GetGrain<IHello>(0); // Use an appropriate grain key
            var response = await grain.SayHello(request.Greeting);
            return Ok(response);
        }
    }
}
```

## 2. Create the Client application (Blazor Web)

### 2.1. Create the Services (HelloService.cs)

```csharp
﻿using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BlazorOrleansClient.Models;

namespace BlazorOrleansClient.Services
{
    public class HelloService
    {
        private readonly HttpClient _httpClient;

        public HelloService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HelloModel> SayHello(string greeting)
        {
            // Use GetStringAsync instead of GetFromJsonAsync
            var responseString = await _httpClient.GetStringAsync($"https://localhost:7068/Hello?Greeting={greeting}");

            // Create a new instance of HelloModel and populate it with the response
            var responseModel = new HelloModel
            {
                Message = responseString
            };

            return responseModel;
        }
    }
}
```

### 2.2. Configure the middleware (program.cs)

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorOrleansClient;
using BlazorOrleansClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<HelloService>();

await builder.Build().RunAsync();
```

### 2.3. Create a new page (Hello.razor)

```razor
﻿@page "/hello"
@inject BlazorOrleansClient.Services.HelloService HelloService
<h1>Say Hello</h1>

<div>
    <input type="text" @bind="greeting" placeholder="Enter a greeting" />
    <button @onclick="GetGreeting">Get Greeting</button>
</div>

@if (helloMessage != null)
{
    <p>@helloMessage.Message</p>
}

@code {
    private string greeting;
    private HelloModel helloMessage;

    private async Task GetGreeting()
    {
        try
        {
            helloMessage = await HelloService.SayHello(greeting);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            Console.WriteLine($"HTTPRequestException: {ex.Message}");
            // Optionally, update the UI to reflect the error
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            Console.WriteLine($"JSON Parsing Error: {jsonEx.Message}");
            // Optionally, update the UI to reflect the error
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            // Optionally, update the UI to reflect the error
        }
    }
}
```

### 2.4. Create Models (HelloModel.cs)

```csharp
﻿namespace BlazorOrleansClient.Models
{
    public class HelloModel
    {
        public string Message { get; set; }
    }
}
```
### 2.5.  Modify the navigation menu (NavMenu.razor)

```razor
﻿<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">BlazorOrleansClient</a>
        <button title="Navigation menu" class="navbar-toggler" @onclick="ToggleNavMenu">
            <span class="navbar-toggler-icon"></span>
        </button>
    </div>
</div>

<div class="@NavMenuCssClass nav-scrollable" @onclick="ToggleNavMenu">
    <nav class="flex-column">
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="counter">
                <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Counter
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="hello">
                <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Hello
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="weather">
                <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Weather
            </NavLink>
        </div>
    </nav>
</div>

@code {
    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}
```

## 3. Run and test the Server




## 4. Run and test the Client

