using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Client.Controllers
{
    public class HomeController : Controller
    {
        public IConfiguration Configuration { get; }

        public HomeController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy() => View();

        [Authorize]
        [HttpGet("/call-api")]
        public async Task<IActionResult> CallApi()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            if (accessToken == null) throw new InvalidOperationException("Could not find access token");
            
            var client = new HttpClient(); // you shouldn't do this. Instead use IHttpClientFactory
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var apiServer = Configuration.GetValue<string>("APIServer:Host");
            var apiUrl = $"{apiServer}/weatherforecast";

            var response = await client.GetAsync(apiUrl);

            return Ok(response.IsSuccessStatusCode 
                ? "API access authorized!" : $"API access failed. Status code: {response.StatusCode}");
        }
    }
}
