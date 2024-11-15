using MessageRateLimiter.Models;
using MessageRateLimiter.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace MessageRateLimiter.Services.Implementation
{
    public class RateLimiterService : IRateLimiterService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RateLimiterService> _logger;
        private static readonly TimeSpan TimeRange = TimeSpan.FromSeconds(1);
        private readonly int _maximumMessagesPerSecondPerPhone;
        private readonly int _maximumMessagesPerSecondPerAccount;
        private static ConcurrentDictionary<string, DateTime>? CacheKeyData;

        public RateLimiterService(IMemoryCache memoryCache, ILogger<RateLimiterService> logger, IConfiguration configuration)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _maximumMessagesPerSecondPerPhone = Convert.ToInt32(configuration.GetSection("Provider:MessageLimitPerPhone").Value);
            _maximumMessagesPerSecondPerAccount = Convert.ToInt32(configuration.GetSection("Provider:MessageLimitPerAccount").Value);

            if (CacheKeyData == null)
                CacheKeyData = new ConcurrentDictionary<string, DateTime>();
        }

        public async Task<MessageRateResponse> IsRateLimitOkay(MessageLimitRequest messageLimitRequest)
        {
            _logger.LogInformation("Message Limit check started");

            //Assuming AccountId and phone are unique and non overlapping sets
            var rateLimitCacheKeyPhone = $"{messageLimitRequest.Phone}";
            var rateLimitCacheKeyAccount = $"{messageLimitRequest.AccountId}";

            var rateLimitCacheRecordPhone = _memoryCache.GetOrCreate(rateLimitCacheKeyPhone, record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                return new PhoneLimitRecord
                {
                    PhoneMessageCount = 0,
                    LastPhoneMessage = DateTime.UtcNow
                };
            });

            var rateLimitCacheRecordAccount = _memoryCache.GetOrCreate(rateLimitCacheKeyAccount, record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                return new AccountLimitRecord
                {
                    AccountMessageCount = 0,
                    LastAccountMessage = DateTime.UtcNow
                };
            });

            // Checking phone Limit
            if (await IsExceeded(rateLimitCacheRecordPhone!.PhoneMessageCount, rateLimitCacheRecordPhone.LastPhoneMessage, _maximumMessagesPerSecondPerPhone ))
            {
                _logger.LogInformation($"Message limit exceeded for Phone - {messageLimitRequest.Phone}");
                return new MessageRateResponse { IsRateLimitOkay = false };
            }

            // checking Account limit
            if (await IsExceeded(rateLimitCacheRecordAccount!.AccountMessageCount, rateLimitCacheRecordAccount.LastAccountMessage, _maximumMessagesPerSecondPerAccount))
            {
                _logger.LogInformation($"Message limit exceeded for Account - {messageLimitRequest.AccountId}");
                return new MessageRateResponse { IsRateLimitOkay = false };
            }

            // update phone and account counts
            rateLimitCacheRecordPhone.PhoneMessageCount++;
            rateLimitCacheRecordAccount.AccountMessageCount++;

            // update Last time for phone and account
            rateLimitCacheRecordPhone.LastPhoneMessage = DateTime.UtcNow;
            rateLimitCacheRecordAccount.LastAccountMessage = DateTime.UtcNow;

            //Set up response object for success
            MessageRateResponse messageRateResponse = new MessageRateResponse();
            messageRateResponse.AccountId = messageLimitRequest.AccountId;
            messageRateResponse.AccountMessageCount = rateLimitCacheRecordAccount.AccountMessageCount;
            messageRateResponse.LastAccountMessage = rateLimitCacheRecordAccount.LastAccountMessage;
            messageRateResponse.Phone = messageLimitRequest.Phone;
            messageRateResponse.PhoneMessageCount = rateLimitCacheRecordPhone.PhoneMessageCount;          
            messageRateResponse.LastPhoneMessage = rateLimitCacheRecordPhone.LastPhoneMessage;

            messageRateResponse.IsRateLimitOkay = true;

            _logger.LogInformation("Message limit not reached");
            return messageRateResponse;
        }

        private async Task<bool> IsExceeded(int LastCount, DateTime LastTime, int Limit)
        {
            if(DateTime.UtcNow - LastTime > TimeRange)
            {
                LastCount = 0;
            }

            return await Task.FromResult(LastCount >= Limit);
        }

    }
}
