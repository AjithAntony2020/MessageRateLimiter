using MessageRateLimiter.Models;
using MessageRateLimiter.Services.Implementation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageRateLimiterTest
{
    [TestFixture]
    public class RateLimiterServiceTests
    {
        private IConfiguration _configuration;
        private ILogger<RateLimiterService> _logger;

        [SetUp]
        public void SetUp()
        {

            //Arrange
            var inMemorySettings = new Dictionary<string, string> {
                {"Provider:MessageLimitPerPhone", "10"},
                {"Provider:MessageLimitPerAccount", "100"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            var mock = new Mock<ILogger<RateLimiterService>>();
            _logger = mock.Object;

        }

        [Test]
        public async Task IsRateLimitOkay_ReturnsFalse_WhenMessageCountGreaterThanAccountLimit()
        {
            //Arrange
            MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var rateLimitCacheRecord = memoryCache!.GetOrCreate("123", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new AccountLimitRecord
                {
                    AccountMessageCount = 101,
                    LastAccountMessage = DateTime.UtcNow
                };
            });

            var rateLimiterService = new RateLimiterService(memoryCache!, _logger, _configuration);
            var request = new MessageLimitRequest { AccountId = "123", Phone = "9898989898" };

            //Act
            MessageRateResponse result = await rateLimiterService.IsRateLimitOkay(request);

            //Assert
            Assert.IsFalse(result.IsRateLimitOkay);
            AccountLimitRecord? record = memoryCache.Get<AccountLimitRecord>("123");
            Assert.Greater(record?.AccountMessageCount, 100);
        }

        [Test]
        public async Task IsRateLimitOkay_ReturnsFalse_WhenMessageCountGreaterThanPhoneLimit()
        {
            //Arrange
            MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var rateLimitCacheRecord = memoryCache!.GetOrCreate("9898989898", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new PhoneLimitRecord
                {
                    PhoneMessageCount = 12,
                    LastPhoneMessage = DateTime.UtcNow
                };
            });

            var rateLimiterService = new RateLimiterService(memoryCache!, _logger, _configuration);
            var request = new MessageLimitRequest { AccountId = "123", Phone = "9898989898" };

            //Act
            MessageRateResponse result = await rateLimiterService.IsRateLimitOkay(request);

            //Assert
            Assert.IsFalse(result.IsRateLimitOkay);
            PhoneLimitRecord? record = memoryCache.Get<PhoneLimitRecord>("9898989898");
            Assert.Greater(record?.PhoneMessageCount, 10);
        }

        [Test]
        public async Task IsRateLimitOkay_ReturnsFalse_WhenMessageCountEqualsPhoneLimit()
        {
            //Arrange
            MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var rateLimitCacheRecord = memoryCache!.GetOrCreate("9898989898", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new PhoneLimitRecord
                {
                    PhoneMessageCount = 10,
                    LastPhoneMessage = DateTime.UtcNow,
                };
            });

            var rateLimiterService = new RateLimiterService(memoryCache!, _logger, _configuration);
            var request = new MessageLimitRequest { AccountId = "123", Phone = "9898989898" };

            //Act
            MessageRateResponse result = await rateLimiterService.IsRateLimitOkay(request);

            //Assert
            Assert.IsFalse(result.IsRateLimitOkay);
            PhoneLimitRecord? record = memoryCache.Get<PhoneLimitRecord>("9898989898");
            Assert.That(record!.PhoneMessageCount.Equals(10));
        }

        [Test]
        public async Task IsRateLimitOkay_ReturnsFalse_WhenMessageCountEqualsAccountLimit()
        {
            //Arrange
            MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var rateLimitCacheRecord = memoryCache!.GetOrCreate("123", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new AccountLimitRecord
                {
                    AccountMessageCount = 100,
                    LastAccountMessage = DateTime.UtcNow
                };
            });

            var rateLimiterService = new RateLimiterService(memoryCache!, _logger, _configuration);
            var request = new MessageLimitRequest { AccountId = "123", Phone = "9898989898" };

            //Act
            MessageRateResponse result = await rateLimiterService.IsRateLimitOkay(request);

            //Assert
            Assert.IsFalse(result.IsRateLimitOkay);
            AccountLimitRecord? record = memoryCache.Get<AccountLimitRecord>("123");
            Assert.That(record!.AccountMessageCount.Equals(100));
        }

        [Test]
        public async Task IsRateLimitOkay_ReturnsTrue_WhenMessageCountLessThanAllLimits()
        {
            //Arrange
            MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var rateLimitCacheRecordAccount = memoryCache!.GetOrCreate("123", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new AccountLimitRecord
                {
                    AccountMessageCount = 45,
                    LastAccountMessage = DateTime.UtcNow
                };
            });

            var rateLimitCacheRecordPhone = memoryCache!.GetOrCreate("9898989898", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new PhoneLimitRecord
                {
                    PhoneMessageCount = 6,
                    LastPhoneMessage = DateTime.UtcNow
                };
            });

            var rateLimiterService = new RateLimiterService(memoryCache!, _logger, _configuration);
            var request = new MessageLimitRequest { AccountId = "123", Phone = "9898989898" };

            //Act
            MessageRateResponse result = await rateLimiterService.IsRateLimitOkay(request);

            //Assert
            Assert.IsTrue(result.IsRateLimitOkay);
            AccountLimitRecord? recordA = memoryCache.Get<AccountLimitRecord>("123");
            PhoneLimitRecord? recordP = memoryCache.Get<PhoneLimitRecord>("9898989898");
            Assert.Less(recordP?.PhoneMessageCount-1, 10);
            Assert.Less(recordA?.AccountMessageCount-1, 100);
        }

        [Test]
        public async Task IsRateLimitOkay_ReturnsTrue_WhenLimitExceededAfterOneSecond()
        {
            //Arrange
            MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var rateLimitCacheRecordAccount = memoryCache!.GetOrCreate("123", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new AccountLimitRecord
                {
                    AccountMessageCount = 112,
                    LastAccountMessage = DateTime.UtcNow - TimeSpan.FromSeconds(1)
                };
            });

            var rateLimitCacheRecordPhone = memoryCache!.GetOrCreate("9898989898", record =>
            {
                record.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                return new PhoneLimitRecord
                {
                    PhoneMessageCount = 15,
                    LastPhoneMessage = DateTime.UtcNow - TimeSpan.FromSeconds(1)
                };
            });

            var rateLimiterService = new RateLimiterService(memoryCache!, _logger, _configuration);
            var request = new MessageLimitRequest { AccountId = "123", Phone = "9898989898" };

            //Act
            MessageRateResponse result = await rateLimiterService.IsRateLimitOkay(request);

            //Assert
            Assert.IsTrue(result.IsRateLimitOkay);
            AccountLimitRecord? recordA = memoryCache.Get<AccountLimitRecord>("123");
            PhoneLimitRecord? recordP = memoryCache.Get<PhoneLimitRecord>("9898989898");
            Assert.Greater(recordP?.PhoneMessageCount - 1, 10);
            Assert.Greater(recordA?.AccountMessageCount - 1, 100);
        }

    }
}