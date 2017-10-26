using System;
using Moq;
using Octokit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal abstract class GitHubSearchMockBase
    {
        protected readonly Mock<ISearchClient> search;

        public ISearchClient Search => search.Object;
        public SearchIssuesRequest SearchIssuesRequest { get; private set; }

        public GitHubSearchMockBase()
        {
            search = new Mock<ISearchClient>(MockBehavior.Strict);

            search
                .Setup(search => search.SearchIssues(It.IsAny<SearchIssuesRequest>()))
                .ReturnsAsync((Func<SearchIssuesRequest, SearchIssuesResult>)CreateSearchResults)
                .Callback<SearchIssuesRequest>(searchIssuesRequest => SearchIssuesRequest = searchIssuesRequest);
        }

        public void VerifySearchCallCount(int callCount)
        {
            search.Verify(search => search.SearchIssues(It.IsAny<SearchIssuesRequest>()), Times.Exactly(callCount));
        }

        protected abstract SearchIssuesResult CreateSearchResults(SearchIssuesRequest searchIssuesRequest);
    }
}
