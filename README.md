# Microsoft Orleans dotNet8 WebAPI(Server) Blazor(Client)

## 1. Create the Server application (.Net 8 WebAPI)

### 1.1. Configure the middleware (program.cs)

This code snippet is a setup for a .NET Core application that integrates ASP.NET Core for web API functionality with Orleans, a cross-platform framework for building distributed applications

It's structured to run an application host that combines web and Orleans services, configured for development and potentially production environments

Here's a breakdown of its key components:

**ASP.NET Core Setup**:

**Swagger**: It configures Swagger, a tool used for documenting APIs

This allows for easy testing and interaction with the web API by generating a user-friendly interface where users can see the endpoints, their requirements, and test them directly in the browser

**CORS (Cross-Origin Resource Sharing)**: It adds and configures CORS policy to specify how web applications running on one origin can access resources from a different origin, which is crucial for web security

This configuration allows requests from https://localhost:7013, with any header and method

**MVC Controllers**: It adds support for controllers, enabling the MVC (Model-View-Controller) pattern to be used for handling web requests

**Routing and Middleware**: It sets up the application's request processing pipeline with routing, CORS, authorization, and endpoints for MVC controllers

**Orleans Setup**:

**Orleans**: A framework for building high-scale, distributed applications with a focus on simplicity and performance

It's often used for cloud services, gaming, and IoT solutions

**Clustering**: The application is configured to use localhost clustering, suitable for development and testing

This means the Orleans silo (a server node in the Orleans framework) will run on the local machine and can communicate with other silos if configured.

**Configuration**: It specifies the ClusterId and ServiceId, which are essential for identifying the cluster and services within Orleans

This setup is crucial for the framework to manage grain (basic units of computation and state in Orleans) activations and requests efficiently

**Logging**: Configures logging to the console, useful for development and debugging

**Application Execution**:

The application builds the host with the configured services and starts running, awaiting web requests and Orleans grain calls

It includes exception handling to catch and log any unhandled exceptions that occur during the application's lifetime

A console message prompts the user that the application is running and waits for an Enter key press to terminate, demonstrating a simple interaction for controlling the application's lifecycle

This code is a comprehensive example of setting up a modern, distributed application using .NET Core's ASP.NET for web APIs and Orleans for distributed systems, showcasing middleware configuration, API documentation, cross-origin requests handling, and basic Orleans clustering and logging

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

The code provides is a basic example of defining a **grain interface** in **Orleans**, a framework for building distributed applications in .NET. Orleans uses the concept of grains as the fundamental units of isolation, distribution, and persistence. A grain is a unit of computation and state that communicates with other grains and external clients via asynchronous messages

The **IHello interface extends IGrainWithIntegerKey**, indicating that it is a grain interface where the grain instances are identified by integer keys

This is a common pattern for grains that represent entities or services that can be uniquely identified by an integer

Here's a brief explanation of the components of the code:

**namespace OrleansWebAPIServer.GrainsIntefaces**: This declares the namespace for the grain interface, which is a logical grouping of related code. This helps in organizing the codebase, especially in larger projects

**public interface IHello IGrainWithIntegerKey**: This line defines the IHello interface, which inherits from IGrainWithIntegerKey

The inheritance implies that any class implementing IHello will also need to implement the members of IGrainWithIntegerKey, which primarily includes an integer-based identifier for the grain

**ValueTask<string> SayHello(string greeting);**: This line declares a method signature for SayHello, which is an asynchronous operation that accepts a string parameter named greeting and returns a ValueTask<string>

ValueTask<T> is a value type used for optimizing small asynchronous operations

It's a more efficient alternative to Task<T> in scenarios where the method may complete synchronously

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

This code snippet is an implementation of a grain class for use with the Orleans framework, a distributed system framework that simplifies the development of scalable, fault-tolerant applications in .NET

Let's break down the key components of this code:

**Namespaces and Directives**:

The code starts with using directives, which import namespaces containing classes and other resources that the HelloGrain class depends on

These namespaces provide functionality for logging (Microsoft.Extensions.Logging), working with the Orleans framework (Orleans), and other base .NET functionality (System, etc.)

**Namespace Declaration**:

The HelloGrain class is defined within the OrleansWebAPIServer.Grains namespace, which logically organizes this class alongside potentially other grain classes and related types within the OrleansWebAPIServer project

**Class Definition**:

HelloGrain is a public class that inherits from Grain and implements the IHello interface

Inheriting from Grain makes HelloGrain an Orleans grain, which is a fundamental building block in Orleans for building distributed applications

The IHello interface defines the contract that this grain must adhere to, likely specifying a method for receiving a greeting


**Logger Field**:

A private readonly field _logger of type ILogger is declared, which is used for logging

This logger is specific to the HelloGrain class, as indicated by the generic parameter ILogger<HelloGrain> in its constructor


**Constructor**:

The HelloGrain class has a constructor that takes an ILogger<HelloGrain> parameter

his logger is provided via dependency injection when the grain is instantiated, enabling the class to log information

**SayHello Method**:

SayHello is an asynchronous method returning a ValueTask<string>

This method takes a string parameter greeting, logs a message including the greeting, and then returns a greeting message from the HelloGrain

The use of ValueTask indicates an optimization for scenarios where the method may complete synchronously, which can be common in high-performance applications

The log message is structured, using a template that includes the greeting in the output. This structured logging is beneficial for later searching or analyzing log data

The response string uses an interpolated verbatim string literal (signified by $""" and """), allowing for easy inclusion of the greeting in a potentially multi-line response format

Overall, this HelloGrain class demonstrates a simple Orleans grain implementation that logs incoming messages and responds with a greeting, showcasing basic concepts of Orleans such as grains, logging, and asynchronous method patterns

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

