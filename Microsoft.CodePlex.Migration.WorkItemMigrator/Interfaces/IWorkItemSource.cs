using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal interface IWorkItemSource
    {
        Task<IReadOnlyList<WorkItemSummary>> GetWorkItemsAsync(Func<int, bool> includePredicate);
        Task<WorkItemDetails> GetWorkItemAsync(WorkItemSummary workItem);
    }
}
