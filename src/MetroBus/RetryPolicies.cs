using System;

namespace MetroBus
{
    public class RetryPolicies
    {
        public Exception[] RetryOnSpecificExceptionTypes { get; set; }
        public int? RetryLimit { get; set; }
        public TimeSpan? InitialRetryIntervalTime { get; set; }
        public TimeSpan? IntervalRetryIncrementTime { get; set; }

        #region Fluent Methods
        public RetryPolicies UseIncrementalRetryPolicy(int retryLimit, TimeSpan initialIntervalTime, TimeSpan intervalIncrementTime, params Exception[] retryOnSpecificExceptionType)
        {
            RetryOnSpecificExceptionTypes = retryOnSpecificExceptionType;
            RetryLimit = retryLimit;
            InitialRetryIntervalTime = initialIntervalTime;
            IntervalRetryIncrementTime = intervalIncrementTime;

            return this;
        }

        public MetroBusInitializer Then()
        {
            return MetroBusInitializer.Instance;
        }
        #endregion
    }
}