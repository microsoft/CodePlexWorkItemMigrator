using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using Octokit.Internal;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class RateLimitingHttpClientAdapter : Octokit.Internal.IHttpClient
    {
        private readonly Octokit.Internal.IHttpClient httpClient;
        private readonly TimeSpan timeInterval;
        private readonly int maxRequestsPerTimeInterval;
        private readonly object lockObject;

        private Queue<DateTimeOffset> requestTimeStamps;

        public RateLimitingHttpClientAdapter(Octokit.Internal.IHttpClient httpClient, TimeSpan timeInterval, int maxRequestsPerTimeInterval)
        {
            ArgValidate.IsNotNull(httpClient, nameof(httpClient));
            ArgValidate.IsInRange(maxRequestsPerTimeInterval, nameof(maxRequestsPerTimeInterval), min: 1, max: Int32.MaxValue);

            if (timeInterval <= TimeSpan.Zero)
            {
                throw new ArgumentException(message: Resources.TimeIntervalMustBeGreaterThanZero, paramName: nameof(timeInterval));
            }

            this.httpClient = httpClient;
            this.timeInterval = timeInterval;
            this.maxRequestsPerTimeInterval = maxRequestsPerTimeInterval;
            requestTimeStamps = new Queue<DateTimeOffset>();
            lockObject = new object();
        }

        #region IHttpClient

        public async Task<IResponse> Send(IRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgValidate.IsNotNull(request, nameof(request));

            lock (lockObject)
            {
                DateTimeOffset requestTimeStamp = DateTimeOffset.UtcNow;

                // If we hit the request rate limit then we check to see if the oldest/first request on record occurred 
                // more than one time interval ago. If this is the case we process the request immediately. Otherwise, 
                // we wait until one time interval since our oldest request on record elapses.
                if (requestTimeStamps.Count == maxRequestsPerTimeInterval)
                {
                    DateTimeOffset oldestRequestTimeStamp = requestTimeStamps.Peek();
                    TimeSpan timeSinceOldestRequest = requestTimeStamp - oldestRequestTimeStamp;

                    if (timeSinceOldestRequest < timeInterval)
                    {
                        Task.Delay(oldestRequestTimeStamp + timeInterval - requestTimeStamp).Wait(cancellationToken);
                    }

                    requestTimeStamps.Dequeue();
                }

                // We need to add the time *right now* cause this is (almost) the time the current request is getting processed.
                requestTimeStamps.Enqueue(DateTimeOffset.UtcNow);
            }

            return await httpClient.Send(request, cancellationToken);
        }

        public void Dispose() => httpClient.Dispose();

        #endregion
    }
}