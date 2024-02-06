using System.ComponentModel.DataAnnotations;

namespace OrleansWebAPIServer.Models
{
    public class GreetingRequest
    {
        [Required]
        public string Greeting { get; set; }
    }
}
