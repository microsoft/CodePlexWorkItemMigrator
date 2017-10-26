using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class CodePlexWorkItemReader : IWorkItemSource
    {
        // Issues REST endpoint has the following paths:
        // .../project/api/issues - returns a list of issues
        //     Optional query string parameters:
        //         start=#, the point in the list to start from (used for paging)
        //         take=#, the number of items to return, defaults to 1024
        //         showClosed=bool, tells if closed items should be returned, defaults to true
        // .../project/api/issues/[issueId] - returns details about a specific issue

        private const string IssuesListUrlTemplate = "https://{0}.codeplex.com/project/api/issues?showClosed={1}";
        private const string IssuesListUrlWithPagingTemplate = "https://{0}.codeplex.com/project/api/issues?start={1}&showClosed={2}";
        private const string IssuesDetailsUrlTemplate = "https://{0}.codeplex.com/project/api/issues/{1}";

        private readonly string project;
        private readonly bool includeClosedWorkItems;
        private readonly IHttpClient httpClient;

        /// <summary>
        /// Creates a <see cref="CodePlexWorkItemReader"/> object.
        /// </summary>
        public CodePlexWorkItemReader(string project, bool includeClosedWorkItems, IHttpClient httpClient)
        {
            ArgValidate.IsNotNullNotEmptyNotWhiteSpace(project, nameof(project));
            ArgValidate.IsNotNull(httpClient, nameof(httpClient));

            this.project = project;
            this.includeClosedWorkItems = includeClosedWorkItems;
            this.httpClient = httpClient;
        }

        #region IWorkItemSource

        /// <summary>
        /// Gets the list of work items that should be migrated.
        /// </summary>
        public async Task<IReadOnlyList<WorkItemSummary>> GetWorkItemsAsync(Func<int, bool> includePredicate)
        {
            ArgValidate.IsNotNull(includePredicate, nameof(includePredicate));

            string issuesListUrl = string.Format(IssuesListUrlTemplate, project, includeClosedWorkItems);

            var workItemSummaries = new List<WorkItemSummary>();

            // Get the first page of work items.
            PagedWorkItemList workItemList = await DownloadWorkItemSummaryPage(issuesListUrl);
            int totalWorkItems = workItemList.TotalItems;
            int itemsRetrieved = workItemList.WorkItemSummaries.Count;

            workItemSummaries.AddRange(workItemList.WorkItemSummaries);

            // Continue getting work item pages until we have all of them.
            while (itemsRetrieved < totalWorkItems)
            {
                string pagedUrl = string.Format(IssuesListUrlWithPagingTemplate, project, itemsRetrieved, includeClosedWorkItems);
                workItemList = await DownloadWorkItemSummaryPage(pagedUrl);
                itemsRetrieved += workItemList.WorkItemSummaries.Count;

                workItemSummaries.AddRange(workItemList.WorkItemSummaries);
            }

            // Now that we have the full list, consult the predicate to trim the list and return that.
            return workItemSummaries.Where(w => includePredicate(w.Id)).ToArray();
        }

        /// <summary>
        /// Gets the details about an individual work item.
        /// </summary>
        public async Task<WorkItemDetails> GetWorkItemAsync(WorkItemSummary workItem)
        {
            ArgValidate.IsNotNull(workItem, nameof(workItem));

            WorkItemDetails workItemToReturn = null;
            string detailedUrl = string.Format(IssuesDetailsUrlTemplate, project, workItem.Id);

            try
            {
                string workItemJson = await httpClient.DownloadStringAsync(detailedUrl);
                workItemToReturn = JsonConvert.DeserializeObject<WorkItemDetails>(workItemJson);
            }
            catch (JsonReaderException ex)
            {
                // If the object coming back from CodePlex cannot be parsed, something went wrong
                // on network so indicate to the calling method this could be retried.
                throw new HttpRequestFailedException(ex.Message, ex);
            }

            return workItemToReturn;
        }

        #endregion

        private async Task<PagedWorkItemList> DownloadWorkItemSummaryPage(string url)
        {
            string json = await httpClient.DownloadStringAsync(url);
            return JsonConvert.DeserializeObject<PagedWorkItemList>(json);
        }
    }

    /// <remarks>
    /// Keeping this class at the <see cref="CodePlexWorkItemReader"/> level. It is intended to help with the JSON representation. 
    /// Outside of <see cref="CodePlexWorkItemReader"/> the <see cref="WorkItem"/> object is the expected contract.
    /// </remarks>
    internal class PagedWorkItemList
    {
        [JsonProperty("List")]
        public List<WorkItemSummary> WorkItemSummaries { get; set; }

        [JsonProperty("TotalItemCount")]
        public int TotalItems { get; set; }
    }
}
