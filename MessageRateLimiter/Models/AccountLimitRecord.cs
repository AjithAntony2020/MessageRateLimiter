namespace MessageRateLimiter.Models
{
    public class AccountLimitRecord
    {
        public int AccountMessageCount { get; set; }
        public DateTime LastAccountMessage { get; set; }
    }
}
