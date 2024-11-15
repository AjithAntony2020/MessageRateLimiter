namespace MessageRateLimiter.Models
{
    public class MessageRateResponse
    {
        public string AccountId { get; set; }  = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public int PhoneMessageCount { get; set; }

        public int AccountMessageCount { get; set; }

        public DateTime LastPhoneMessage { get; set; }

        public DateTime LastAccountMessage { get; set; }

        public bool IsRateLimitOkay { get; set; }
    }
}
