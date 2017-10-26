using System;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal static class WorkItemExtensions
    {
        /// <summary>
        /// Checks whether the work item is closed.
        /// </summary>
        public static bool IsClosed(this WorkItem workItem)
        {
            ArgValidate.IsNotNull(workItem, nameof(workItem));

            return string.Equals(workItem.Status?.Name, CodePlexStrings.Closed, StringComparison.OrdinalIgnoreCase);
        }
    }
}
