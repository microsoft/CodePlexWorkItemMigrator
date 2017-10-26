using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class ConsoleWorkItemWriter : IWorkItemDestination
    {
        #region IWorkItemDestination

        /// <summary>
        /// Gets the list of migrated work items.
        /// </summary>
        /// <remarks>
        /// In this case because the writer is dumping the data to console, there are never any known migrated work items so the list is empty.
        /// </remarks>
        public Task<IReadOnlyList<MigratedWorkItem>> GetMigratedWorkItemsAsync() => Task.FromResult<IReadOnlyList<MigratedWorkItem>>(new MigratedWorkItem[0]);

        /// <summary>
        /// Writes some of the work item information to the console.
        /// </summary>
        public Task WriteWorkItemAsync(WorkItemDetails workItemDetails)
        {
            ArgValidate.IsNotNull(workItemDetails, nameof(workItemDetails));
            ArgValidate.IsNotNull(workItemDetails.WorkItem, nameof(workItemDetails.WorkItem));

            string output = $"{workItemDetails.WorkItem.Id}{Environment.NewLine}{workItemDetails.WorkItem.Summary}{Environment.NewLine}{workItemDetails.WorkItem.Description}{Environment.NewLine}{Environment.NewLine}";

            Console.WriteLine(output);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Writes some of the work item information to the console
        /// </summary>
        /// <remarks>
        /// In this case because the writer is dumping the data to console, there is no way to update the work item so we just dump it to console.
        /// </remarks>
        public Task UpdateWorkItemAsync(WorkItemDetails value) => WriteWorkItemAsync(value);

        #endregion
    }
}
