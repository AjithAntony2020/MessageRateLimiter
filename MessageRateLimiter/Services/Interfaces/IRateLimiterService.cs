using MessageRateLimiter.Models;
using System.Threading.Tasks;

namespace MessageRateLimiter.Services.Interfaces
{
    public interface IRateLimiterService
    {
        Task<MessageRateResponse> IsRateLimitOkay(MessageLimitRequest messageLimitRequest);
    }
}
