using Microsoft.AspNetCore.Mvc;
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
