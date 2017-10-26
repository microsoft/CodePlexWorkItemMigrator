using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class GitHubRepoIssueReaderWriter : IWorkItemDestination
    {
        private static readonly StringComparer DefaultStringComparer = StringComparer.OrdinalIgnoreCase;

        private readonly string repoOwner;
        private readonly string repo;
        private readonly IIssuesClient issues;
        private readonly ISearchClient search;

        /// <summary>
        /// Creates a <see cref="GitHubRepoIssueReaderWriter"/> object. This object is used to interface with GitHub to manage issues that are being migrated.
        /// </summary>
        public GitHubRepoIssueReaderWriter(string repoOwner, string repo, IIssuesClient issues, ISearchClient search)
        {
            ArgValidate.IsNotNullNotEmptyNotWhiteSpace(repoOwner, nameof(repoOwner));
            ArgValidate.IsNotNullNotEmptyNotWhiteSpace(repo, nameof(repo));
            ArgValidate.IsNotNull(issues, nameof(issues));
            ArgValidate.IsNotNull(search, nameof(search));

            this.repoOwner = repoOwner;
            this.repo = repo;
            this.issues = issues;
            this.search = search;
        }

        #region IWorkItemDestination

        /// <summary>
        /// Writes <paramref name="workItemDetails"/> as a new issue to GitHub.
        /// </summary>
        public Task WriteWorkItemAsync(WorkItemDetails workItemDetails)
        {
            ArgValidate.IsNotNull(workItemDetails, nameof(workItemDetails));
            ArgValidate.IsNotNull(workItemDetails.WorkItem, nameof(workItemDetails.WorkItem));

            return InvokeAsync(
                async () =>
                {
                    Issue createdIssue = await CreateNewIssueAsync(workItemDetails.WorkItem, workItemDetails.FileAttachments);
                    await SetIssueDetailsAsync(createdIssue, workItemDetails);
                    return true;    // This return statement is only here because we're expected to return Task<T>.
                });
        }

        /// <summary>
        /// Searches for issue in GitHub and updates it.
        /// </summary>
        /// <remarks>
        /// This is only expected to happen if the initial write failed on a previous run.
        /// </remarks>
        public Task UpdateWorkItemAsync(WorkItemDetails workItemDetails)
        {
            ArgValidate.IsNotNull(workItemDetails, nameof(workItemDetails));
            ArgValidate.IsNotNull(workItemDetails.WorkItem, nameof(workItemDetails.WorkItem));

            return InvokeAsync(
                async () =>
                {
                    Issue issue = await GetCorrespondingIssueAsync(workItemDetails.WorkItem.Id);
                    await UpdateIssueAsync(issue, workItemDetails);
                    return true;    // This return statement is only here because we're expected to return Task<T>.
                });
        }

        /// <summary>
        /// Searches the issues in GitHub and builds a list of ones that have already been migrated along with their state.
        /// </summary>
        public Task<IReadOnlyList<MigratedWorkItem>> GetMigratedWorkItemsAsync()
        {
            return InvokeAsync(
                async () =>
                {
                    IReadOnlyList<int> migratedWorkItemIds = await GetWorkItemIdsByLabel(GitHubLabels.CodePlexMigrated);
                    IReadOnlyList<int> partiallyMigratedWorkItemIds = await GetWorkItemIdsByLabel(GitHubLabels.CodePlexMigrationInitiated);

                    IEnumerable<MigratedWorkItem> migratedWorkItems = migratedWorkItemIds.Select(i => new MigratedWorkItem(i, MigrationState.Migrated));

                    IEnumerable<MigratedWorkItem> partiallyMigratedWorkItems =
                        partiallyMigratedWorkItemIds.Select(i => new MigratedWorkItem(i, MigrationState.PartiallyMigrated));

                    return (IReadOnlyList<MigratedWorkItem>)migratedWorkItems.Concat(partiallyMigratedWorkItems).ToArray();
                });
        }

        #endregion

        #region Privates

        private Task<Issue> CreateNewIssueAsync(WorkItem workItem, IEnumerable<WorkItemFileAttachment> attachments)
        {
            var newIssue = new NewIssue(title: workItem.Summary)
            {
                Body = TextUtilities.GetFormattedWorkItemBody(workItem, attachments),
            };

            SetNewIssueLabels(newIssue.Labels, workItem);

            return issues.Create(owner: repoOwner, name: repo, newIssue: newIssue);
        }

        private async Task DeleteAllCommentsAsync(int issueNumber)
        {
            IReadOnlyList<IssueComment> comments = await issues.Comment.GetAllForIssue(owner: repoOwner, name: repo, number: issueNumber);

            foreach (IssueComment comment in comments)
            {
                await issues.Comment.Delete(owner: repoOwner, name: repo, id: comment.Id);
            }
        }

        private async Task<Issue> GetCorrespondingIssueAsync(int workItemId)
        {
            var searchIssuesRequest = new SearchIssuesRequest(term: TextUtilities.GetFormattedWorkItemId(workItemId))
            {
                Type = IssueTypeQualifier.Issue,
                In = new[] { IssueInQualifier.Body },
            };

            searchIssuesRequest.Repos.Add(owner: repoOwner, name: repo);

            SearchIssuesResult searchIssuesResult = await search.SearchIssues(searchIssuesRequest);

            int searchResultCount = searchIssuesResult.Items.Count;
            if (searchResultCount == 0)
            {
                throw new WorkItemIdentificationException(string.Format(Resources.NoIssueFoundInGitHubRepoWithWorkItemIdX, workItemId));
            }

            if (searchResultCount > 1)
            {
                throw new WorkItemIdentificationException(string.Format(Resources.MultipleIssuesFoundInGitHubRepoWithWorkItemIdX, workItemId));
            }

            return searchIssuesResult.Items[0];
        }

        private async Task<IReadOnlyList<int>> GetWorkItemIdsByLabel(string label)
        {
            var result = new List<int>();

            // Search for all issues marked with CodePlex migration labels sorted in ascending fashion by creation date.
            var searchIssuesRequest = new SearchIssuesRequest
            {
                Type = IssueTypeQualifier.Issue,
                Labels = new[] { label },
                Order = SortDirection.Ascending,
                SortField = IssueSearchSort.Created,
                Page = 1,
            };

            searchIssuesRequest.Repos.Add(owner: repoOwner, name: repo);

            int resultCount = 0;
            SearchIssuesResult searchIssuesResult = await search.SearchIssues(searchIssuesRequest);
            int totalResultCount = searchIssuesResult.TotalCount;

            // Extract CodePlex work item ID from issue body, migration state from from assigned labels. 
            while (searchIssuesResult.Items.Count > 0)
            {
                foreach (Issue issue in searchIssuesResult.Items)
                {
                    try
                    {
                        int codePlexWorkItemId = TextUtilities.GetCodePlexWorkItemId(issue.Body);
                        result.Add(codePlexWorkItemId);
                    }
                    catch (Exception ex)
                    {
                        throw new WorkItemIdentificationException(
                            string.Format(Resources.ErrorExtractingCodePlexWorkItemIdFromGitHubIssue, issue.Number, ex.Message), ex);
                    }
                }

                resultCount += searchIssuesResult.Items.Count;
                if (resultCount >= totalResultCount)
                {
                    break;
                }

                searchIssuesRequest.Page += 1;
                searchIssuesResult = await search.SearchIssues(searchIssuesRequest);
            }

            return result;
        }

        private async Task SetIssueDetailsAsync(Issue issue, WorkItemDetails workItemDetails)
        {
            await AddCommentsAsync(issue.Number, workItemDetails);
            await UpdateLabelsAndStateAsync(issue, closeIssue: workItemDetails.WorkItem.IsClosed());
        }

        private async Task AddCommentsAsync(int issueNumber, WorkItemDetails workItemDetails)
        {
            foreach (string comment in TextUtilities.GetFormattedComments(workItemDetails))
            {
                await issues.Comment.Create(owner: repoOwner, name: repo, number: issueNumber, newComment: comment);
            }
        }

        private Task UpdateLabelsAndStateAsync(Issue issue, bool closeIssue)
        {
            IssueUpdate issueUpdate = issue.ToUpdate();
            issueUpdate.Labels.Remove(GitHubLabels.CodePlexMigrationInitiated);
            issueUpdate.Labels.Add(GitHubLabels.CodePlexMigrated);

            if (closeIssue)
            {
                issueUpdate.State = ItemState.Closed;
            }

            return issues.Update(owner: repoOwner, name: repo, number: issue.Number, issueUpdate: issueUpdate);
        }

        private async Task UpdateIssueAsync(Issue issue, WorkItemDetails workItemDetails)
        {
            WorkItem workItem = workItemDetails.WorkItem;

            IssueUpdate issueUpdate = issue.ToUpdate();
            issueUpdate.Title = workItem.Summary;
            issueUpdate.Body = TextUtilities.GetFormattedWorkItemBody(workItem, workItemDetails.FileAttachments);

            SetNewIssueLabels(issueUpdate.Labels, workItem);

            issue = await issues.Update(owner: repoOwner, name: repo, number: issue.Number, issueUpdate: issueUpdate);

            await DeleteAllCommentsAsync(issue.Number);
            await SetIssueDetailsAsync(issue, workItemDetails);
        }

        #endregion

        #region Helpers

        private static void SetNewIssueLabels(ICollection<string> labels, WorkItem workItem)
        {
            labels.Clear();

            // This label marks newly created issues until their migration is finalized.
            labels.Add(GitHubLabels.CodePlexMigrationInitiated);

            // Release is added as a label to facilitate searching/filtering.
            string release = workItem.PlannedForRelease;
            if (!string.IsNullOrEmpty(release))
            {
                labels.Add(release);
            }

            // Affected Component Name is added as a label to facilitate searching/filtering.
            string affectedComponentName = workItem.AffectedComponent?.DisplayName;
            if (!string.IsNullOrEmpty(affectedComponentName))
            {
                labels.Add(affectedComponentName);
            }

            // Add a label for impact.
            string impact = workItem.Priority?.Name;
            if (!string.IsNullOrEmpty(impact))
            {
                labels.Add($"{GitHubLabels.Impact}: {impact}");
            }

            // Apply "duplicate" label if issue was marked as such.
            string reasonClosed = workItem.ReasonClosed?.Name;
            if (DefaultStringComparer.Equals(reasonClosed, CodePlexStrings.Duplicate))
            {
                labels.Add(GitHubLabels.Duplicate);
            }

            string issueType = workItem.Type?.Name;
            if (!string.IsNullOrEmpty(issueType))
            {
                // Convert: Feature -> enhancement, Issue -> bug. 
                // Any other issue type is added verbatim as a new label unless it is "Unassigned".
                if (DefaultStringComparer.Equals(issueType, CodePlexStrings.Feature))
                {
                    labels.Add(GitHubLabels.Enhancement);
                }
                else if (DefaultStringComparer.Equals(issueType, CodePlexStrings.Issue))
                {
                    labels.Add(GitHubLabels.Bug);
                }
                else if (!DefaultStringComparer.Equals(issueType, CodePlexStrings.Unassigned))
                {
                    labels.Add(issueType);
                }
            }
        }

        private static async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (ApiValidationException ex)
            {
                string errorMessage = ex?.ApiError?.Errors.FirstOrDefault()?.Message;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    throw new WorkItemIdentificationException(errorMessage, ex);
                }

                throw;
            }
        }

        #endregion
    }
}
