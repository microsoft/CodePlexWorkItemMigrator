namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class MigratedWorkItem
    {
        /// <summary>
        /// Id of work item on CodePlex
        /// </summary>
        public int CodePlexWorkItemId { get; }

        /// <summary>
        /// Migration state of CodePlex work item
        /// </summary>
        public MigrationState MigrationState { get; }

        public MigratedWorkItem(int codePlexWorkItemId, MigrationState migrationState)
        {
            CodePlexWorkItemId = codePlexWorkItemId;
            MigrationState = migrationState;
        }
    }
}