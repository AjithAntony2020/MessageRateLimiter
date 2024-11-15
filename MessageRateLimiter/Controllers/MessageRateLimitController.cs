using MessageRateLimiter.Models;
using MessageRateLimiter.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace MessageRateLimiter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageRateLimitController : ControllerBase
    {
        private readonly IRateLimiterService _rateLimiterService;
        private readonly ILogger<MessageRateLimitController> _logger;
        private readonly IHubContext<LiveUpdateHub> _hubContext;
        public MessageRateLimitController(IRateLimiterService rateLimiterService, IHubContext<LiveUpdateHub> hubContext, ILogger<MessageRateLimitController> logger)
        {
            _rateLimiterService = rateLimiterService;
            _hubContext = hubContext;
            _logger = logger;
        }

        // POST api/<ValuesController>  
        [HttpPost]
        public Task<MessageRateResponse> Post([FromBody] MessageLimitRequest request)
        {
            var response = _rateLimiterService.IsRateLimitOkay(request);
            string jsonResponse = JsonConvert.SerializeObject(new {AccountId = response.Result.AccountId, MessageCount = response.Result.AccountMessageCount, 
                                                                   Phone = response.Result .Phone, PhoneMessageCount = response.Result .PhoneMessageCount});
            _hubContext.Clients.All.SendAsync("ReceiveMessage", jsonResponse);
            return response;
        }

    }
}
