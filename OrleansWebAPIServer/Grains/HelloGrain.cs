using OrleansWebAPIServer.GrainsIntefaces;
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
