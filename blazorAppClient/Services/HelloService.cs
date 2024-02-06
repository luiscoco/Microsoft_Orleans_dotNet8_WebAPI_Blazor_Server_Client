using System.Net.Http;
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
