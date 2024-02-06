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

**Orleans**: A framework for building high-scale, distributed applications with a focus on simplicity and performance. It's often used for cloud services, gaming, and IoT solutions

**Clustering**: The application is configured to use localhost clustering, suitable for development and testing. This means the Orleans silo (a server node in the Orleans framework) will run on the local machine and can communicate with other silos if configured.

**Configuration**: It specifies the ClusterId and ServiceId, which are essential for identifying the cluster and services within Orleans. This setup is crucial for the framework to manage grain (basic units of computation and state in Orleans) activations and requests efficiently

**Logging**: Configures logging to the console, useful for development and debugging

**Application Execution**: The application builds the host with the configured services and starts running, awaiting web requests and Orleans grain calls

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

The **IHello interface extends IGrainWithIntegerKey**, indicating that it is a grain interface where the grain instances are identified by integer keys. This is a common pattern for grains that represent entities or services that can be uniquely identified by an integer

Here's a brief explanation of the components of the code:

**namespace OrleansWebAPIServer.GrainsIntefaces**: This declares the namespace for the grain interface, which is a logical grouping of related code. This helps in organizing the codebase, especially in larger projects

**public interface IHello IGrainWithIntegerKey**: This line defines the IHello interface, which inherits from IGrainWithIntegerKey. The inheritance implies that any class implementing IHello will also need to implement the members of IGrainWithIntegerKey, which primarily includes an integer-based identifier for the grain

**ValueTask<string> SayHello(string greeting);**: This line declares a method signature for SayHello, which is an asynchronous operation that accepts a string parameter named greeting and returns a ValueTask<string>

ValueTask<T> is a value type used for optimizing small asynchronous operations. It's a more efficient alternative to Task<T> in scenarios where the method may complete synchronously

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

This code snippet defines a simple C# class named GreetingRequest within the OrleansWebAPIServer.Models namespace

The purpose of this class is to represent a request model, specifically for operations that involve a greeting message

Let's break down the key components of this code:

**Namespace and Directive**:

The using System.ComponentModel.DataAnnotations; directive is included at the top of the file

This namespace contains classes that are used for defining metadata for ASP.NET Core model validation

It's used here to import attributes that can be applied to model properties to specify validation requirements or describe schema

**Namespace Declaration**:

The GreetingRequest class is defined within the OrleansWebAPIServer.Models namespace. Namespaces are used in C# to organize and provide a level of separation for classes, interfaces, and other types

**Class Definition**:

GreetingRequest is defined as a public class, making it accessible from other parts of the application or potentially other applications that reference this assembly

**Property Definition**:

The class defines a single property named Greeting of type string. This property represents the greeting message that will be part of the request handled by the application

**Data Annotations**:

The [Required] attribute from the System.ComponentModel.DataAnnotations namespace is applied to the Greeting property

This attribute indicates that the Greeting property is required; thus, it must have a value for the model to be considered valid

This is particularly useful in scenarios where the model is being automatically validated by ASP.NET Core's model binding and validation features, such as in API request handling

When a GreetingRequest object is instantiated and populated from an incoming request, the presence of the Greeting property will be enforced, and an error will be generated if it is missing

In summary, the GreetingRequest class serves as a data transfer object (DTO) that defines the structure of a greeting request, including any validation rules that must be satisfied

This class is a typical pattern in web applications for encapsulating data sent to and from the client, with the [Required] attribute ensuring that necessary data is present

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

This code snippet defines a controller class named HelloController in an ASP.NET Core application that is integrated with the Orleans distributed application framework

This controller is responsible for handling HTTP GET requests by invoking a method on an Orleans grain and returning its response. Let's break down the key components of this code:

**Namespaces and Directives**:

The code starts with using directives to include necessary namespaces for ASP.NET Core MVC functionality (Microsoft.AspNetCore.Mvc), Orleans client functionality (Orleans), the interface that defines grain contracts (OrleansWebAPIServer.GrainsIntefaces), and data models (OrleansWebAPIServer.Models)

**Namespace Declaration**:

The HelloController class is part of the OrleansWebAPIServer.Controllers namespace, which organizes it with other controller classes within the ASP.NET Core application

**Class and Attributes**:

HelloController inherits from ControllerBase, making it a controller class. It's decorated with [ApiController] and [Route("[controller]")] attributes

The [ApiController] attribute denotes it as a controller with API-specific behavior (e.g., automatic model validation). The [Route("[controller]")] attribute sets up the routing to this controller, using the controller's name as the route path

**Private Field**:

A private readonly field _client of type IClusterClient is declared. This field represents the Orleans cluster client, which is used to communicate with grains in the Orleans cluster

**Constructor**:

The HelloController constructor accepts an IClusterClient instance via dependency injection and assigns it to the _client field

This setup allows the controller to interact with Orleans grains

**SayHello Action Method**:

The SayHello method is an asynchronous action method that handles HTTP GET requests

It is decorated with the [HttpGet] attribute to map GET requests to this method

The method takes a GreetingRequest parameter, which is populated from the query string of the request ([FromQuery] attribute)

This model contains the greeting message to be sent to the grain

Inside the method, it first checks if the model state is valid (ModelState.IsValid)

If not, it returns a bad request response (BadRequest(ModelState)), which includes validation errors

It then retrieves a grain reference using _client.GetGrain<IHello>(0)

The IHello interface represents the contract for the grain, and 0 is used as the grain's unique key. In a real application, you'd use an appropriate key based on your application's logic

The SayHello method of the grain is called asynchronously with await, passing the greeting from the request

The response from the grain is awaited and then returned as an HTTP 200 OK response (Ok(response)), containing the grain's reply.

In summary, this controller provides an HTTP endpoint for a web API that processes greeting requests

It validates the incoming request, interacts with an Orleans grain to perform some operation (in this case, saying hello), and returns the result

This pattern illustrates how ASP.NET Core MVC controllers can be integrated with Orleans to expose grain functionality over HTTP

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

This code snippet defines a HelloService class within a Blazor client application that interacts with an external service to retrieve a greeting message. 

Here's a breakdown of its components and functionality:

**Namespace and Imports**: The code uses namespaces and using directives to organize its structure and reference necessary .NET classes for HTTP communication and asynchronous programming

It imports the System.Net.Http, System.Net.Http.Json, and System.Threading.Tasks namespaces for HTTP operations and asynchronous tasks

The BlazorOrleansClient.Models namespace is also imported, which likely contains model classes used within the application, such as HelloModel

**HelloService Class**: This is a service class designed to encapsulate the logic for making HTTP requests to an external service

It demonstrates a common pattern in ASP.NET Core and Blazor applications where services are defined to handle specific pieces of business logic, in this case, saying hello

**HttpClient Dependency Injection**: The class has a constructor that accepts an HttpClient instance as a dependency

This HttpClient is injected at runtime by the Blazor application's dependency injection (DI) container

The DI pattern is used here to provide the HelloService class with the necessary HttpClient instance for making HTTP requests without tightly coupling it to a specific HTTP client implementation

**SayHello Method**: This asynchronous method SayHello takes a string parameter named greeting and uses the injected HttpClient to make a GET request to a predefined URL (https://localhost:7068/Hello)

The greeting parameter is appended to the query string of the request URL

Instead of directly deserializing the JSON response into a HelloModel object using GetFromJsonAsync, the method uses GetStringAsync to fetch the response as a string

**Response Handling**: Upon receiving the response, the method instantiates a HelloModel object and sets its Message property to the response string. This object is then returned to the caller

**HelloModel Class**: Although not defined in the provided code snippet, it's apparent that HelloModel is a model class with at least one property named Message. This class is used to encapsulate the data returned by the hello service

This service could be used in a Blazor application to dynamically fetch and display a greeting message based on user input or other logic, showcasing an example of client-server communication in a modern web application using Blazor and potentially Orleans for distributed systems

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

This code snippet is a part of the startup process for a Blazor WebAssembly application

It sets up the application's hosting environment, configures services, and starts the application. Here's a breakdown of its key components:

**Namespace Imports**: The code begins by importing necessary namespaces:

Microsoft.AspNetCore.Components.Web and Microsoft.AspNetCore.Components.WebAssembly.Hosting are essential for working with Blazor WebAssembly, including components and hosting features

BlazorOrleansClient is likely the namespace of the main application, which includes the App component

BlazorOrleansClient.Services contains application-specific services, such as the HelloService

**WebAssemblyHostBuilder**:

WebAssemblyHostBuilder.CreateDefault(args) creates an instance of WebAssemblyHostBuilder with default settings. This builder is used to configure the app's services, components, and other settings. The args parameter can include runtime arguments passed to the application

**Root Components Registration**:

builder.RootComponents.Add<App>("#app") registers the App component as a root component of the application and specifies that it should be attached to an HTML element with the ID app. This is typically the main component that serves as the entry point for the Blazor application

builder.RootComponents.Add<HeadOutlet>("head::after") registers the HeadOutlet component for injecting elements into the HTML <head> tag, following the existing content.

**Service Configuration**:

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }) adds an HttpClient to the DI container with a scoped lifetime. It's configured with a BaseAddress taken from the host environment's base address. This HttpClient instance can be injected into components and services throughout the application, enabling them to make HTTP requests relative to the application's base URL

builder.Services.AddScoped<HelloService>() adds the HelloService to the application's DI container with a scoped lifetime. This makes HelloService available for injection into components and other services, facilitating separation of concerns and reusability

**Application Startup**:

await builder.Build().RunAsync(); builds the application and starts it asynchronously. Build() finalizes the configuration and prepares the application for execution, while RunAsync() starts the application, beginning the event loop and enabling the app to handle user interactions and render UI updates

In summary, this code is responsible for configuring and launching a Blazor WebAssembly application, setting up its environment, registering components, configuring services for dependency injection, and finally, starting the application to make it available to users

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

This code snippet is a Blazor component that provides a user interface for sending a greeting and receiving a response

It showcases various Blazor concepts such as routing, dependency injection, data binding, event handling, and conditional rendering. Here's a breakdown of its key parts:

**Routing with @page Directive**: The @page "/hello" directive at the top makes this component accessible via a specific URL path (/hello). This is how Blazor implements routing to different components within the application

**Dependency Injection**: The @inject directive is used to inject a service (HelloService) from the BlazorOrleansClient.Services namespace into the component. This service is used to send the greeting to a backend and receive a response

**User Interface**:

The component displays a static heading ```<h1>Say Hello</h1>```

It includes an <input> element bound to a greeting variable, allowing the user to enter a greeting. The @bind directive creates a two-way binding between the input field and the greeting variable

A <button> is provided for sending the greeting, with an @onclick event handler that triggers the GetGreeting method when clicked

**Conditional Rendering**:

The ```@if``` statement checks if helloMessage is not null. If true, it displays the message received from the HelloService within a paragraph ```<p>```. This demonstrates conditional rendering based on the state of the component

**Code Block**:

The @code block contains the C# logic for the component, defining variables for the user input (greeting) and the message received (helloMessage)

The GetGreeting method is an asynchronous task that calls HelloService.SayHello(greeting) to fetch a greeting message. It updates helloMessage with the response, which is then displayed to the user

Exception handling is implemented to catch and log different types of errors that may occur during the HTTP request or the JSON parsing process

This includes handling for HttpRequestException, JsonException, and a generic Exception to cover other unexpected errors

This component is an example of a simple interactive Blazor WebAssembly application feature

It demonstrates how to interact with backend services, manage component state, and dynamically update the UI based on user input and asynchronous operations

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


This code snippet defines a simple model class named HelloModel within the BlazorOrleansClient.Models namespace

This class is a part of a Blazor application that likely communicates with a backend service, possibly using Orleans, a framework for building distributed applications

Here's a breakdown of its structure and purpose:

**Namespace**: The BlazorOrleansClient.Models namespace suggests that this class is part of the Models directory or assembly in a Blazor project named BlazorOrleansClient. Namespaces help organize code and prevent naming conflicts.

**Class Definition**: The HelloModel class is declared as public, meaning it can be accessed from other classes and assemblies

This class serves as a data model, which is a common practice in MVC (Model-View-Controller) and similar patterns used in web and application development

**Properties**:

**public string Message { get; set; }**: This property is of type string and is named Message. It is auto-implemented with get and set accessors, allowing it to be read from and written to by other parts of the application

This property is intended to store a message, likely a greeting or response fetched from a backend service or generated based on user input

The purpose of the HelloModel class is to encapsulate data related to a greeting message

In the context of a Blazor application, it could be used to transfer data between the frontend and a backend service, enabling dynamic content to be displayed to the user based on interactions or processes within the application

This class follows the principle of keeping data models simple and focused on representing the structure of the data being handled

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

We add a new item in the left hand side menu for accessin the Hello.razor page

This is the code we add:

```razor
 <div class="nav-item px-3">
            <NavLink class="nav-link" href="hello">
                <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Hello
            </NavLink>
        </div>
```

This is the **NavMenu.razor** whole code

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

