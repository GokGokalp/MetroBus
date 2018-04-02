using System;

namespace MetroBus
{
    public class RetryPolicies
    {
        internal Exception[] RetryOnSpecificExceptionTypes { get; set; }
        internal int? RetryLimit { get; set; }
        internal TimeSpan? InitialRetryIntervalTime { get; set; }
        internal TimeSpan? IntervalRetryIncrementTime { get; set; }

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