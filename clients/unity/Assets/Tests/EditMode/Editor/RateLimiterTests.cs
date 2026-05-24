using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SpaceTraders.API;
using System.Diagnostics;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class RateLimiterTests
    {
        [Test]
        public async Task WaitAsync_LimitsRateProactively()
        {
            // 10 requests per second = 100ms per request
            var limiter = new RateLimiter(requestsPerSecond: 10, burstLimit: 1);
            
            var sw = Stopwatch.StartNew();
            
            // First request should be immediate
            await limiter.WaitAsync();
            long firstRequestTime = sw.ElapsedMilliseconds;
            
            // Second request should wait ~100ms
            await limiter.WaitAsync();
            long secondRequestTime = sw.ElapsedMilliseconds;

            Assert.LessOrEqual(firstRequestTime, 50, "First request should be fast");
            Assert.GreaterOrEqual(secondRequestTime, 90, "Second request should be throttled");
        }

        [Test]
        public async Task WaitAsync_AllowsBurst()
        {
            var limiter = new RateLimiter(requestsPerSecond: 1, burstLimit: 5);
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < 5; i++)
            {
                await limiter.WaitAsync();
            }
            
            Assert.Less(sw.ElapsedMilliseconds, 100, "Burst of 5 should be near-instant");
            
            await limiter.WaitAsync();
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 900, "6th request should wait for refill");
        }

        [Test]
        public async Task WaitAsync_RespectsResetTime()
        {
            var limiter = new RateLimiter(requestsPerSecond: 100, burstLimit: 100);
            
            // Set reset time to 500ms in the future
            limiter.SetResetTime(DateTime.UtcNow.AddMilliseconds(500));
            
            var sw = Stopwatch.StartNew();
            await limiter.WaitAsync();
            
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 450, "Should wait for reset time even if tokens are available");
        }
    }
}
