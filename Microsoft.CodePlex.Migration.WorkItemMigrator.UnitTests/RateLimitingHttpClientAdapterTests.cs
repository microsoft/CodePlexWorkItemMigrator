using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Octokit;
using Octokit.Internal;
using Xunit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    public class RateLimitingHttpClientAdapterTests
    {
        [Fact]
        public void Ctor_IfNullOrInvalidArgs_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateTarget(new CtorArgs { HttpClient = null }));
            Assert.Throws<ArgumentException>(() => CreateTarget(new CtorArgs { TimeInterval = TimeSpan.Zero }));
            Assert.Throws<ArgumentException>(() => CreateTarget(new CtorArgs { MaxRequestsPerTimeInterval = 0 }));
            Assert.Throws<ArgumentException>(() => CreateTarget(new CtorArgs { MaxRequestsPerTimeInterval = -1 }));
        }

        [Fact]
        public async Task Send_OnNullRequest_Throws()
        {
            // Arrange
            RateLimitingHttpClientAdapter target = CreateTarget();

            // Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.Send(request: null));
        }

        [Fact]
        public async Task Send_IfValidMaxRequestsPerTimeInterval_LimitsSentRequests()
        {
            // Arrange
            IResponse response = new Mock<IResponse>().Object;
            IRequest request = new Mock<IRequest>().Object;

            var httpClientMock = new Mock<Octokit.Internal.IHttpClient>();
            httpClientMock
                .Setup(httpClient => httpClient.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync<IRequest, CancellationToken, Octokit.Internal.IHttpClient, IResponse>(
                    (r, t) =>
                    {
                        Task.Delay(TimeSpan.FromMilliseconds(10)).Wait();
                        return response;
                    });

            var ctorArgs = new CtorArgs { HttpClient = httpClientMock.Object,  MaxRequestsPerTimeInterval = 5 };
            RateLimitingHttpClientAdapter target = CreateTarget(ctorArgs);

            DateTimeOffset startTime = DateTimeOffset.UtcNow;

            // Act
            var tasks = new List<Task>();
            int requestCount = ctorArgs.MaxRequestsPerTimeInterval + 1;
            for (int i = 0; i < requestCount; ++i)
            {
                tasks.Add(Task.Run(() => target.Send(request)));
            }

            // Assert
            await Task.WhenAll(tasks);
            Assert.True(DateTimeOffset.Now - startTime > ctorArgs.TimeInterval);
            httpClientMock.Verify(httpClient => httpClient.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(requestCount));
        }

        private static RateLimitingHttpClientAdapter CreateTarget(CtorArgs ctorArgs = null)
        {
            CtorArgs args = ctorArgs ?? new CtorArgs();
            return new RateLimitingHttpClientAdapter(args.HttpClient, args.TimeInterval, args.MaxRequestsPerTimeInterval);
        }

        private class CtorArgs
        {
            public Octokit.Internal.IHttpClient HttpClient { get; set; }
            public int MaxRequestsPerTimeInterval { get; set; }
            public TimeSpan TimeInterval { get; set; }

            public CtorArgs()
            {
                HttpClient = new Mock<Octokit.Internal.IHttpClient>().Object;
                MaxRequestsPerTimeInterval = 20;
                TimeInterval = TimeSpan.FromSeconds(1);
            }
        }
    }
}
