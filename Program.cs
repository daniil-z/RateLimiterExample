using System;

namespace RateLimiter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Rate limiter example");

            TokenBucket tbRateLimiter = new TokenBucket(6, 1, 1, TimeSpan.FromSeconds(1));

            for (int i = 1; i<20; i++)
            {
                int tokens = 6;

                if (i % 2 == 0)
                    tokens = 1;
                
                Console.WriteLine($"Request #{i} is allowed: {tbRateLimiter.IsRequestAllowed(tokens)}");
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            Console.WriteLine("Press any key");
            Console.ReadKey();
        }
    }

    public class TokenBucket
    {
        #region Public properties

        /// <summary>
        /// Default refill interval value 1 time
        /// </summary>
        public long DEFAULT_REFILL_INTERVAL
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Default refill duration value 1000 msec
        /// </summary>
        public long DEFAULT_REFILL_DURATION
        {
            get
            {
                return 1000;
            }
        }

        #endregion Public properties


        #region Private properties

        /// <summary>
        /// Maximum bucket size
        /// </summary>
        private long _maxBucketSize;

        /// <summary>
        /// Current bucket size
        /// </summary>
        private long _currentBacketSize;

        /// <summary>
        /// Number of tokens when refilling the bucket
        /// </summary>
        private long _refillAmount;

        /// <summary>
        /// Bucket refill interval, times
        /// </summary>
        private long _refillInterval;

        /// <summary>
        /// Bucket refill duration, milliseconds
        /// </summary>
        private long _refillDuration;

        /// <summary>
        /// Calculated bucket refill interval in milliseconds
        /// </summary>
        private long _refillCalculatedInterval;

        /// <summary>
        /// Timestamp of next refill
        /// </summary>
        private long _nextRefillTimestamp;

        /// <summary>
        /// Lock primitive
        /// </summary>
        private static readonly object rootLock = new object();

        #endregion Private properties


        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="maxBucketSize">Maximum tokens in the backet</param>
        /// <param name="refillAmount">Number of tokens when refilling the bucket</param>
        /// <param name="refillInterval">Bucket refill interval</param>
        public TokenBucket(long maxBucketSize, long refillAmount, long refillInterval, TimeSpan refillDuration)
        {
            _maxBucketSize = maxBucketSize;
            _currentBacketSize = maxBucketSize;
            _refillAmount = refillAmount > maxBucketSize ? _maxBucketSize : refillAmount;
            _refillInterval = refillInterval < 0 ? DEFAULT_REFILL_INTERVAL : refillInterval;
            _refillDuration = refillDuration.Milliseconds < 0 ? DEFAULT_REFILL_DURATION : refillDuration.Milliseconds;
            _refillCalculatedInterval = TimeSpan.FromMilliseconds(_refillInterval * _refillDuration).Ticks;
            _nextRefillTimestamp = DateTime.Now.Ticks + _refillCalculatedInterval;
        }

        /// <summary>
        /// Returns true if the request is allowed, otherwise false
        /// </summary>
        /// <param name="tokensCost">Cost of the request in tokens</param>
        /// <returns></returns>
        public bool IsRequestAllowed(long tokensCost)
        {
            if (tokensCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(tokensCost), tokensCost, "Param \"tokens\" must be positive");

            lock (rootLock)
            {
                RefillBucket();

                if (_currentBacketSize >= tokensCost)
                {
                    _currentBacketSize -= tokensCost;

                    return true;
                }

                return false;
            }
        }

        protected void RefillBucket()
        {
            var currTimestamp = DateTime.Now.Ticks;

            if (currTimestamp <= _nextRefillTimestamp) return;

            _currentBacketSize += _refillAmount;

            if (_currentBacketSize > _maxBucketSize)
                _currentBacketSize = _maxBucketSize;

            _nextRefillTimestamp = currTimestamp + _refillInterval;
        }
    }
}
