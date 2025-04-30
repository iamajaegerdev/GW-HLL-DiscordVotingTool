using Discord.Net;
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

namespace Services
{
    public partial class DiscordRateLimitService
    {
        private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitBuckets = new();
        private readonly SemaphoreSlim _apiRateLimitSemaphore = new(1, 5); // Allow 5 concurrent API calls
        private readonly Queue<DateTime> _apiCallTimes = new();
        private readonly object _apiCallTimesLock = new();
        private const int RATE_LIMIT_PER_SECOND = 1; // requests per second
        private const int RATE_LIMIT_WINDOW_MS = 1000; // 1 second window

        public async Task<T> ExecuteWithRateLimitAsync<T>(string routeKey, Func<Task<T>> action)
        {
            try
            {
                await _apiRateLimitSemaphore.WaitAsync();
                try
                {
                    // Check if we need to wait based on the current rate limit info
                    await EnsureDynamicRateLimitComplianceAsync(routeKey);

                    // Execute the action
                    var result = await action();

                    // Record this API call
                    UpdateRateLimitEstimate(routeKey, true);

                    return result;
                }
                finally
                {
                    _apiRateLimitSemaphore.Release();
                }
            }
            catch (RateLimitedException rateLimitEx)
            {
                // Extract retry time from the exception message
                double retryAfter = ExtractRetryAfterFromException(rateLimitEx);

                Logger.LogWithTimestamp($"Rate limit hit. Waiting {retryAfter} seconds before retry...");

                // Update our rate limit tracker
                UpdateRateLimitFromException(routeKey, retryAfter);

                // Wait the specified time plus a small buffer
                await Task.Delay(TimeSpan.FromSeconds(retryAfter) + TimeSpan.FromMilliseconds(50));

                // Recursive call after waiting for rate limit
                return await ExecuteWithRateLimitAsync(routeKey, action);
            }
            catch (HttpException httpEx)
            {
                Logger.LogWithTimestamp($"HTTP Error: {httpEx.HttpCode} - {httpEx.Reason}");

                if (httpEx.HttpCode == HttpStatusCode.TooManyRequests)
                {
                    // Parse the retry-after value from the JSON response
                    double retryAfter = 5.0; // Default fallback

                    try
                    {
                        // Try to extract the retry_after value from the error JSON
                        if (!string.IsNullOrEmpty(httpEx.Message))
                        {
                            var match = MyRegex1().Match(httpEx.Message);
                            if (match.Success && match.Groups.Count > 1)
                            {
                                if (double.TryParse(match.Groups[1].Value, out double parsed))
                                {
                                    retryAfter = parsed;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWithTimestamp($"Error parsing retry-after: {ex.Message}");
                    }

                    // Update our rate limit tracker
                    UpdateRateLimitFromException(routeKey, retryAfter);

                    Logger.LogWithTimestamp($"Rate limit hit. Waiting {retryAfter} seconds before retry...");
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter) + TimeSpan.FromMilliseconds(50));

                    // Recursive call after waiting for rate limit
                    return await ExecuteWithRateLimitAsync(routeKey, action);
                }
                throw; // Re-throw other HTTP exceptions
            }
        }

        // Token bucket rate limiter implementation
        private async Task EnsureRateLimitComplianceAsync()
        {
            DateTime now = DateTime.UtcNow;
            lock (_apiCallTimesLock)
            {
                // Remove API calls that are outside the rate limit window
                while (_apiCallTimes.Count > 0 && now.Subtract(_apiCallTimes.Peek()).TotalMilliseconds > RATE_LIMIT_WINDOW_MS)
                {
                    _apiCallTimes.Dequeue();
                }

                // If we have capacity, return immediately
                if (_apiCallTimes.Count < RATE_LIMIT_PER_SECOND)
                {
                    return;
                }
            }

            // Calculate how long to wait before making another request
            int waitTime;
            lock (_apiCallTimesLock)
            {
                // Get the oldest API call in our window
                DateTime oldestCall = _apiCallTimes.Peek();

                // Calculate when that call will expire from our window
                DateTime timeWhenSlotAvailable = oldestCall.AddMilliseconds(RATE_LIMIT_WINDOW_MS);

                // Calculate how many milliseconds until then
                waitTime = (int)Math.Max(0, (timeWhenSlotAvailable - now).TotalMilliseconds);

                // Add a small buffer to be safe
                waitTime += 50;
            }

            if (waitTime > 0)
            {
                Logger.LogWithTimestamp($"Rate limit window full. Waiting {waitTime}ms for a slot to open.");
                await Task.Delay(waitTime);
            }
        }

        // Helper method to extract retry-after value from rate limit exception message
        private static double ExtractRetryAfterFromException(RateLimitedException ex)
        {
            // Default fallback value
            double retryAfter = 5.0;

            try
            {
                // Try to extract the retry time from the exception message using regex
                var match = MyRegex2().Match(ex.Message);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (double.TryParse(match.Groups[1].Value, out double parsed))
                    {
                        retryAfter = parsed;
                        Logger.LogWithTimestamp($"Extracted retry-after from exception: {retryAfter} seconds");
                        return retryAfter;
                    }
                }

                // Try another common format
                match = MyRegex().Match(ex.Message);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (double.TryParse(match.Groups[1].Value, out double parsed))
                    {
                        retryAfter = parsed;
                        Logger.LogWithTimestamp($"Extracted retry-after from exception (format 2): {retryAfter} seconds");
                        return retryAfter;
                    }
                }

                // Log that we're using the default
                Logger.LogWithTimestamp($"Could not extract retry-after from exception message: '{ex.Message}'. Using default: {retryAfter} seconds");
            }
            catch (Exception parseEx)
            {
                Logger.LogWithTimestamp($"Error parsing retry time from exception: {parseEx.Message}. Using default: {retryAfter} seconds");
            }

            return retryAfter;
        }

        // Updates our rate limit info when we get a rate limit exception
        private void UpdateRateLimitFromException(string routeKey, double retryAfter)
        {
            if (!_rateLimitBuckets.TryGetValue(routeKey, out var bucketInfo))
            {
                bucketInfo = new RateLimitInfo();
                _rateLimitBuckets[routeKey] = bucketInfo;
            }

            bucketInfo.Remaining = 0;
            bucketInfo.Reset = DateTimeOffset.UtcNow.AddSeconds(retryAfter);
            bucketInfo.LastUpdated = DateTime.UtcNow;

            Logger.LogWithTimestamp($"Updated rate limit for {routeKey}: Remaining=0, Reset in {retryAfter}s");
        }

        // Updates our rate limit tracker with estimated values when we make successful requests
        private void UpdateRateLimitEstimate(string routeKey, bool success)
        {
            if (!_rateLimitBuckets.TryGetValue(routeKey, out var bucketInfo))
            {
                bucketInfo = new RateLimitInfo();
                _rateLimitBuckets[routeKey] = bucketInfo;
            }

            if (success)
            {
                // Decrement our remaining count
                bucketInfo.Remaining = Math.Max(0, bucketInfo.Remaining - 1);

                // If we're getting low, start being more conservative
                if (bucketInfo.Remaining < 3)
                {
                    Logger.LogWithTimestamp($"Rate limit for {routeKey} is getting low: {bucketInfo.Remaining} remaining.");
                }
            }

            bucketInfo.LastUpdated = DateTime.UtcNow;
        }

        // Dynamic rate limit compliance checking
        private async Task EnsureDynamicRateLimitComplianceAsync(string routeKey)
        {
            // Get or create the rate limit info for this bucket
            if (!_rateLimitBuckets.TryGetValue(routeKey, out var bucketInfo))
            {
                bucketInfo = new RateLimitInfo();
                _rateLimitBuckets[routeKey] = bucketInfo;
            }

            // Check if the rate limit has reset
            if (DateTimeOffset.UtcNow > bucketInfo.Reset)
            {
                // Reset has occurred, refresh our estimates
                bucketInfo.Remaining = bucketInfo.Limit;
                bucketInfo.Reset = DateTimeOffset.UtcNow.AddSeconds(RATE_LIMIT_WINDOW_MS / 1000.0);
                Logger.LogWithTimestamp($"Rate limit for {routeKey} has reset. New remaining: {bucketInfo.Remaining}");
            }

            // If we've exhausted our requests, wait until reset
            if (bucketInfo.Remaining <= 0)
            {
                // Calculate the time to wait
                TimeSpan timeToWait = bucketInfo.Reset - DateTimeOffset.UtcNow;

                // Add a small buffer (50ms)
                if (timeToWait > TimeSpan.Zero)
                {
                    timeToWait += TimeSpan.FromMilliseconds(50);
                    Logger.LogWithTimestamp($"Rate limit exhausted for route {routeKey}. " +
                                           $"Waiting {timeToWait.TotalMilliseconds}ms until reset.");
                    await Task.Delay(timeToWait);

                    // After waiting, reset our counter
                    bucketInfo.Remaining = bucketInfo.Limit;
                    bucketInfo.Reset = DateTimeOffset.UtcNow.AddSeconds(RATE_LIMIT_WINDOW_MS / 1000.0);
                }
            }

            // As a fallback, also use the token bucket approach
            await EnsureRateLimitComplianceAsync();
        }

        // Class to store rate limit information per bucket
        private class RateLimitInfo
        {
            public int Limit { get; set; } = 5; // Default value
            public int Remaining { get; set; } = 5;
            public DateTimeOffset Reset { get; set; } = DateTimeOffset.UtcNow.AddSeconds(1);
            public double ResetAfter { get; set; } = 1.0;
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        }

        [GeneratedRegex(@"retry after (\d+\.\d+)s")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"retry_after[""]*:\s*([\d\.]+)")]
        private static partial Regex MyRegex1();
        [GeneratedRegex(@"Try again in (\d+) seconds")]
        private static partial Regex MyRegex2();
    }
} 