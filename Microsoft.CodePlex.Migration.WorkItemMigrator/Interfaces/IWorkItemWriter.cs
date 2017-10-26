using System.Threading.Tasks;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal interface IWorkItemWriter
    {
        Task WriteWorkItemAsync(WorkItemDetails workItemDetails);
        Task UpdateWorkItemAsync(WorkItemDetails workItemDetails);
    }
}
