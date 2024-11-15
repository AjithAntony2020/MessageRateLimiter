namespace MessageRateLimiter.Models
{
    public class PhoneLimitRecord
    {
        public int PhoneMessageCount { get; set; }
        public DateTime LastPhoneMessage { get; set; }
    }
}
