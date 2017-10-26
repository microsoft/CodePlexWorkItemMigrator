using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal static class WorkItemMigrator
    {
        /// <summary>
        /// Migrates all work items from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        public static async Task MigrateAsync(
            IWorkItemSource source, IWorkItemDestination destination, MigrationSettings settings, ILogger logger)
        {
            ArgValidate.IsNotNull(source, nameof(source));
            ArgValidate.IsNotNull(destination, nameof(destination));
            ArgValidate.IsNotNull(logger, nameof(logger));
            ArgValidate.IsNotNull(settings, nameof(settings));

            logger.LogMessage(LogLevel.Info, Resources.BeginMigrationMessage);

            try
            {
                // Retrive all work items that have already been migrated.
                logger.LogMessage(LogLevel.Info, Resources.LookupMigratedWorkItemsMessage);
                IReadOnlyList<MigratedWorkItem> migratedWorkItems = await destination.GetMigratedWorkItemsAsync();

                IDictionary<int, MigrationState> migratedWorkItemState = GetWorkItemDictionary(migratedWorkItems);

                // Update the skip list to include any work items the user indicated we should not retrieve.
                AddSkipItemsToMigrateState(settings.WorkItemsToSkip, migratedWorkItemState);

                // Get the list of potential work items to migrate.
                logger.LogMessage(LogLevel.Info, Resources.LookupWorkItemsToMigrate);
                IReadOnlyList<WorkItemSummary> notMigratedWorkItems = 
                    await source.GetWorkItemsAsync(id => GetMigrationState(migratedWorkItemState, id) != MigrationState.Migrated);

                // Get the actual list of work items to migrate taking into account the limit specified in migration settings.
                int countToMigrate = settings.MaxItemsToMigrate == -1 ? notMigratedWorkItems.Count : settings.MaxItemsToMigrate;
                logger.LogMessage(LogLevel.Info, Resources.StartingMigrationOfXWorkItems, countToMigrate);

                logger.LogMessage(LogLevel.Warning, Resources.ProgressOfMigrationWillBeSlow);

                IEnumerable<WorkItemSummary> workItemsToMigrate = notMigratedWorkItems.Take(countToMigrate);

                int migratedWorkItemCount = 0;
                // Download the details from CodePlex and push it to the destination.
                foreach (WorkItemSummary workItemSummary in workItemsToMigrate)
                {
                    logger.LogMessage(LogLevel.Trace, Resources.LookupIndividualWorkItem, workItemSummary.Id);
                    WorkItemDetails item = null;
                    await RetryAsync(
                        async () => item = await source.GetWorkItemAsync(workItemSummary), settings.MaxRetryCount, settings.RetryDelay);

                    if (GetMigrationState(migratedWorkItemState, workItemSummary.Id) == MigrationState.PartiallyMigrated)
                    {
                        logger.LogMessage(LogLevel.Trace, Resources.UpdatingWorkItem, workItemSummary.Id);
                        await RetryAsync(() => destination.UpdateWorkItemAsync(item), settings.MaxRetryCount, settings.RetryDelay);
                    }
                    else
                    {
                        logger.LogMessage(LogLevel.Trace, Resources.AddingWorkItem, workItemSummary.Id);
                        await RetryAsync(() => destination.WriteWorkItemAsync(item), settings.MaxRetryCount, settings.RetryDelay);
                    }

                    logger.LogMessage(LogLevel.Info, Resources.SuccessfullyMigratedWorkItemIdXTitleY, workItemSummary.Id, ++migratedWorkItemCount, 
                                      countToMigrate, workItemSummary.Title);
                }
            }
            catch(Exception ex)
            {
                logger.LogMessage(LogLevel.Error, Resources.LogExceptionMessage, ex.Message);
                throw;
            }

            logger.LogMessage(LogLevel.Info, Resources.MigrationCompletedSuccessfully);
        }

        private static async Task RetryAsync(Func<Task> action, int maxRetryCount, TimeSpan retryDelay)
        {
            for (int retryCount = 0; ; ++retryCount)
            {
                try
                {
                    await action();
                    return;
                }
                catch (HttpRequestFailedException)
                {
                    if (retryCount >= maxRetryCount)
                    {
                        throw;
                    }

                    await Task.Delay(retryDelay);
                }
            }
        }

        private static IDictionary<int, MigrationState> GetWorkItemDictionary(IReadOnlyList<MigratedWorkItem> migratedWorkItems)
        {
            var result = new Dictionary<int, MigrationState>();

            foreach (MigratedWorkItem workItem in migratedWorkItems.EmptyIfNull())
            {
                int workItemId = workItem.CodePlexWorkItemId;
                if (result.ContainsKey(workItemId))
                {
                    throw new WorkItemIdentificationException(string.Format(Resources.EncounteredCollisionWorkItemIdX, workItemId));
                }

                result[workItemId] = workItem.MigrationState;
            }

            return result;
        }

        private static void AddSkipItemsToMigrateState(IEnumerable<int> skipItems, IDictionary<int, MigrationState> migrateState)
        {
            foreach(int item in skipItems.EmptyIfNull())
            {
                migrateState[item] = MigrationState.Migrated;
            }
        }

        private static MigrationState GetMigrationState(IDictionary<int, MigrationState> workItems, int workItemId) =>
            workItems.TryGetValue(workItemId, out MigrationState result) ? result : MigrationState.None;
    }
}
