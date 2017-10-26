using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal class GitHubSearchInBodyMock : GitHubSearchMockBase
    {
        private IReadOnlyList<Issue> issues;

        public GitHubSearchInBodyMock(IEnumerable<Issue> searchResults)
        {
            issues = searchResults.EmptyIfNull().ToArray();
        }

        protected override SearchIssuesResult CreateSearchResults(SearchIssuesRequest searchIssuesRequest) =>
            new SearchIssuesResult(totalCount: issues.Count, incompleteResults: false, items: issues);
    }
}
