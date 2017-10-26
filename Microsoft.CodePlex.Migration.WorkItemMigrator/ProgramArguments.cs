using PowerArgs;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    internal class ProgramArguments
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgRequired, ArgShortcut("-p"), ArgDescription("The name of the CodePlex project to migrate")]
        public string CodePlexProject { get; set; }

        [ArgRequired, ArgShortcut("-o"), ArgDescription("The owner of the GitHub repository to which issues are migrated")]
        public string GitHubRepoOwner { get; set; }

        [ArgRequired, ArgShortcut("-r"), ArgDescription("The name of the GitHub repository to which issues are migrated")]
        public string GitHubRepo { get; set; }

        [ArgRequired, ArgShortcut("-t"), ArgDescription("Personal Access Token to be used for accessing the GitHub repository")]
        public string GitHubPersonalAccessToken { get; set; }

        [ArgShortcut("-m"), ArgDescription("The maximum number of items to migrate. If -1 all items will be migrated"), DefaultValue(-1), ArgRange(-1, int.MaxValue)]
        public int MaxItemsToMigrate { get; set; }

        [ArgShortcut("-l"), ArgDescription("Path and name of the log file"), DefaultValue("migration.log")]
        public string LogFilePath { get; set; }

        [ArgShortcut("-c"), ArgDescription("Flag to tell migrator if the closed work items should be migrated"), DefaultValue(true)]
        public bool MigrateClosedItems { get; set; }

        [ArgShortcut("-s"), ArgDescription("Comma-separated list of work item IDs to exclude from migration")]
        public int[] WorkItemsToSkip { get; set; }

        [ArgShortcut("-WhatIf"), ArgDescription("Flag to tell migrator to only read work items and print results to console"), DefaultValue(false)]
        public bool OnlyWriteMigrationToConsole { get; set; }
    }
}
