using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Logging;

namespace SpaceTraders.API
{
    public class RateLimiter
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly int _requestsPerSecond;
        private readonly int _burstLimit;
        
        private int _availableTokens;
        private DateTime _lastRefill;

        private DateTime _resetTime = DateTime.MinValue;

        public RateLimiter(int requestsPerSecond = 2, int burstLimit = 10)
        {
            _requestsPerSecond = requestsPerSecond;
            _burstLimit = burstLimit;
            _availableTokens = burstLimit;
            _lastRefill = DateTime.UtcNow;
        }

        /// <summary>
        /// Proactively waits for a rate limit token to become available.
        /// This ensures we stay within limits BEFORE making the network call.
        /// </summary>
        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Handle 429 reset time from previous responses if any
                if (DateTime.UtcNow < _resetTime)
                {
                    TimeSpan delay = _resetTime - DateTime.UtcNow;
                    Log.Info("[RateLimiter] 429 reset period active. Proactively waiting {Seconds:F2}s...", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                    _resetTime = DateTime.MinValue; // Clear after waiting
                }

                Refill();

                while (_availableTokens <= 0)
                {
                    // Wait for the next token to be generated
                    double msToWait = 1000.0 / _requestsPerSecond;
                    await Task.Delay((int)msToWait, cancellationToken);
                    Refill();
                }

                _availableTokens--;
                Log.Debug("[RateLimiter] Token consumed. Available: {Count}", _availableTokens);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void SetResetTime(DateTime resetTime)
        {
            _resetTime = resetTime;
            Log.Warning("[RateLimiter] Server-side reset time updated: {Time}", _resetTime);
        }

        private void Refill()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - _lastRefill;
            
            double tokensToAdd = elapsed.TotalSeconds * _requestsPerSecond;
            if (tokensToAdd >= 1.0)
            {
                int wholeTokens = (int)tokensToAdd;
                _availableTokens = Math.Min(_burstLimit, _availableTokens + wholeTokens);
                
                // Advance lastRefill by the exact duration of the whole tokens consumed
                // This preserves fractional time for the next token refill.
                _lastRefill = _lastRefill.AddSeconds(wholeTokens / (double)_requestsPerSecond);
            }
        }
    }
}
