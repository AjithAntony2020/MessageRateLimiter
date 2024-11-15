namespace MessageRateLimiter.Models
{
    public class MessageLimitRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string Phone { get; set;} = string.Empty;
        
    }
}
