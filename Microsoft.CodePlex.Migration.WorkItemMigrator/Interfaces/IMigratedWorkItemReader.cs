using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal interface IMigratedWorkItemReader
    {
        /// <summary>
        /// An abstraction for retrieving a list of CodePlex work items that have already been migrated.
        /// </summary>
        /// <returns>A list of all CodePlex work items present in the target repository</returns>
        Task<IReadOnlyList<MigratedWorkItem>> GetMigratedWorkItemsAsync();
    }
}
