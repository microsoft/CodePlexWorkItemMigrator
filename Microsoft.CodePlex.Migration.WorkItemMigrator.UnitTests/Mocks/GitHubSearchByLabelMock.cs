using System.Collections.Generic;
using System.Linq;
using Octokit;
using Xunit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal class GitHubSearchByLabelMock : GitHubSearchMockBase
    {
        private readonly Dictionary<string, IReadOnlyList<Issue>> issues;

        public GitHubSearchByLabelMock()
        {
            issues = new Dictionary<string, IReadOnlyList<Issue>>();
        }

        public GitHubSearchByLabelMock SetSearchResults(string label, IEnumerable<Issue> issues)
        {
            this.issues[label] = issues.EmptyIfNull().ToArray();
            return this;
        }

        protected override SearchIssuesResult CreateSearchResults(SearchIssuesRequest searchIssuesRequest)
        {
            string label = searchIssuesRequest.Labels.Single();
            if (!issues.TryGetValue(label, out IReadOnlyList<Issue> searchResults))
            {
                searchResults = new Issue[0];
            }

            int totalCount = searchResults.Count;

            if (totalCount == 0)
            {
                Assert.Equal(1, searchIssuesRequest.Page);
            }
            else if (totalCount > 1)
            {
                if (searchIssuesRequest.Page == 1)
                {
                    searchResults = searchResults.Take(totalCount / 2).ToArray();
                }
                else if (searchIssuesRequest.Page == 2)
                {
                    searchResults = searchResults.Skip(totalCount / 2).ToArray();
                }
            }

            return new SearchIssuesResult(totalCount: totalCount, incompleteResults: false, items: searchResults);
        }
    }
}
