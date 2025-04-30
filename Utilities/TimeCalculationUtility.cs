using System;

namespace Utilities
{
    public static class TimeCalculationUtility
    {
        public class ProcessingTimeResult
        {
            public int TotalMinutes { get; set; }
            public int RemainingSeconds { get; set; }
            public string MinuteString { get; set; } = string.Empty;
            public string SecondString { get; set; } = string.Empty;
            public string FormattedString { get; set; } = string.Empty; // Initialize with a default value
        }

        public static ProcessingTimeResult CalculateProcessingTime(DateTime startTime, DateTime? endTime = null)
        {
            DateTime end = endTime ?? DateTime.Now;
            TimeSpan timeDifference = end - startTime;

            // Convert TotalMinutes to an integer value
            int truncatedTotalMinutes = (int)Math.Truncate(timeDifference.TotalMinutes);

            // Calculate the remaining seconds correctly
            int remainingSeconds = (int)Math.Truncate(timeDifference.TotalSeconds % 60);

            // Create plural strings
            string minuteString = truncatedTotalMinutes == 1 ? "minute" : "minutes";
            string secondString = remainingSeconds == 1 ? "second" : "seconds";

            return new ProcessingTimeResult
            {
                TotalMinutes = truncatedTotalMinutes,
                RemainingSeconds = remainingSeconds,
                MinuteString = minuteString,
                SecondString = secondString,
                FormattedString = $"{truncatedTotalMinutes} {minuteString} and {remainingSeconds} {secondString}"
            };
        }
    }
} 