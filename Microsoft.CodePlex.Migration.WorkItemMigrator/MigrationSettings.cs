using System;
using System.Collections.Generic;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class MigrationSettings
    {
        public static MigrationSettings DefaultSettings =>
            new MigrationSettings
            {
                MaxRetryCount = 3,
                RetryDelay = TimeSpan.FromSeconds(3),
                WorkItemsToSkip = null,
                MaxItemsToMigrate = -1
            };

        public int MaxRetryCount { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public IEnumerable<int> WorkItemsToSkip { get; set; }
        public int MaxItemsToMigrate { get; set; }
    }
}
