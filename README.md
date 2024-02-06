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

### 1.2. 

## 2. Create the Client application (Blazor Web)



## 3. Run and test the Server




## 4. Run and test the Client

