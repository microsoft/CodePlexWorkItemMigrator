using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    public class WorkItemMigratorTests
    {
        [Fact]
        public async Task Migrate_OneItemToMigrate_Succeeds()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(1);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object);

            //Assert
            reader.VerifyReaderCalledForWorkItems();

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Once);
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
        }

        [Fact]
        public async Task Migrate_MultipleItemsToMigrate_Succeeds()
        {
            // Arrange
            int numberOfWorkItems = 5;
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(numberOfWorkItems);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object);

            //Assert
            reader.VerifyReaderCalledForWorkItems();

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Exactly(numberOfWorkItems));
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
        }

        [Fact]
        public async Task Migrate_ZeroItemsToMigrate_Succeeds()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(0);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object);

            //Assert
            reader.VerifyReaderCalledForWorkItems();

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
        }

        [Fact]
        public async Task Migrate_ParitallyMigratedItemsUpdated_Succeeds()
        {
            // Arrange
            int numberOfWorkItems = 5;
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(numberOfWorkItems);

            var partiallyMigratedItems = new List<MigratedWorkItem>();

            foreach (WorkItemSummary summary in reader.Summaries)
            {
                var status = new MigratedWorkItem(summary.Id, MigrationState.PartiallyMigrated);
                partiallyMigratedItems.Add(status);
            }

            destManager.Setup(m => m.GetMigratedWorkItemsAsync()).ReturnsAsync(partiallyMigratedItems);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object);

            //Assert
            reader.VerifyReaderCalledForWorkItems();

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Exactly(numberOfWorkItems));
        }

        [Fact]
        public async Task Migrate_FullyMigratedItemsNotReMigrated()
        {
            // Arrange
            int numberOfWorkItems = 5;
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(numberOfWorkItems);

            var fullyMigratedList = new List<MigratedWorkItem>();
            foreach (WorkItemSummary summary in reader.Summaries)
            {
                var status = new MigratedWorkItem(summary.Id, MigrationState.Migrated);
                fullyMigratedList.Add(status);
            }

            destManager.Setup(m => m.GetMigratedWorkItemsAsync()).ReturnsAsync(fullyMigratedList);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object);

            //Assert
            reader.mockReader.Verify(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>()), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>()), Times.Never);

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);

        }

        [Fact]
        public async Task Migrate_MixOfParialFullyAndNotMigratedStatus_Success()
        {
            // Arrange
            int numberOfWorkItems = 5;
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(numberOfWorkItems);

            var migrationStatusList = new List<MigratedWorkItem>();

            var migratedItem = new MigratedWorkItem(1, MigrationState.Migrated);
            migrationStatusList.Add(migratedItem);

            var partialItem = new MigratedWorkItem(2, MigrationState.PartiallyMigrated);
            migrationStatusList.Add(partialItem);

            destManager.Setup(m => m.GetMigratedWorkItemsAsync()).ReturnsAsync(migrationStatusList);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object);

            //Assert
            reader.mockReader.Verify(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>()), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>()), Times.Exactly(4));

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Exactly(3));
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_DoesNotReturnItemsInSkipList_Succeeds()
        {
            // Arrange
            int numberOfWorkItems = 5;
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();
            var migrationSettings = MigrationSettings.DefaultSettings;

            migrationSettings.WorkItemsToSkip = new List<int>
            {
                1,
                2,
                3
            };

            reader.BuildWorkItemLists(numberOfWorkItems);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, migrationSettings, logger.Object);

            //Assert
            reader.mockReader.Verify(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>()), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 0)), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 1)), Times.Never);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 2)), Times.Never);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 3)), Times.Never);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 4)), Times.Once);

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Exactly(2));
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
        }

        [Fact]
        public async Task Migrate_MigratedAndSkipListItemsCombine()
        {
            // Arrange
            int numberOfWorkItems = 6;
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();
            var migrationSettings = MigrationSettings.DefaultSettings;

            reader.BuildWorkItemLists(numberOfWorkItems);

            var fullyMigratedList = new List<MigratedWorkItem>
            {
                new MigratedWorkItem(0, MigrationState.Migrated),
                new MigratedWorkItem(1, MigrationState.Migrated),
                new MigratedWorkItem(2, MigrationState.PartiallyMigrated),
                new MigratedWorkItem(3, MigrationState.PartiallyMigrated)
            };

            migrationSettings.WorkItemsToSkip = new List<int>
            {
                1,
                2,
                5
            };

            destManager.Setup(m => m.GetMigratedWorkItemsAsync()).ReturnsAsync(fullyMigratedList);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, migrationSettings, logger.Object);

            //Assert
            reader.mockReader.Verify(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>()), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 0)), Times.Never);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 1)), Times.Never);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 2)), Times.Never);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 3)), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 4)), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x.Id == 5)), Times.Never);

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Once);
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Once);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        public async Task Migrate_MigratedOnlyDoesMaxIfMaxSet_Succeeds(int maxItemsToMigrate)
        {
            // Arrange
            int numberOfWorkItems = 5;
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();
            var migrationSettings = MigrationSettings.DefaultSettings;
            migrationSettings.MaxItemsToMigrate = maxItemsToMigrate;

            reader.BuildWorkItemLists(numberOfWorkItems);

            // Act
            await WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, migrationSettings, logger.Object);

            //Assert
            reader.mockReader.Verify(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>()), Times.Once);
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>()), Times.Exactly(maxItemsToMigrate));

            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Exactly(maxItemsToMigrate));
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Never);
        }

        [Fact]
        public async Task Migrate_ErrorOnGetWorkItems_Throws()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(1);

            reader.mockReader.Setup(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>())).Throws(new WorkItemMigratorTestsException("Mock exception"));

            // Act and Assert
            await Assert.ThrowsAsync<WorkItemMigratorTestsException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object));
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_HttpRequestFailedExceptionOnGetWorkItemAsync_Retried_AfterRetriesThrows()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            var migrationSetting = MigrationSettings.DefaultSettings;
            migrationSetting.RetryDelay = TimeSpan.Zero;

            reader.BuildWorkItemLists(1);

            reader.mockReader.Setup(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>())).ThrowsAsync(new HttpRequestFailedException("Mock exception"));

            // Act and Assert
            await Assert.ThrowsAsync<HttpRequestFailedException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, migrationSetting, logger.Object));
            // MaxRetryCount = 3 however this is retries, we also call once for the initial trial...
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>()), Times.Exactly(4));
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_NonHttpRequestFailedExceptionOnGetWorkItemAsync_NotRetried_Throws()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(1);

            reader.mockReader.Setup(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>())).Throws(new WorkItemMigratorTestsException("Mock exception"));

            // Act and Assert
            await Assert.ThrowsAsync<WorkItemMigratorTestsException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object));
            reader.mockReader.Verify(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>()), Times.Once);
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_HttpRequestFailedExceptionOnUpdateWorkItemAsync_Retried_AfterRetriesThrows()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();
            var migrationSettings = MigrationSettings.DefaultSettings;

            migrationSettings.RetryDelay = TimeSpan.Zero;

            reader.BuildWorkItemLists(1);

            var partiallyMigratedItems = new List<MigratedWorkItem>();

            foreach (WorkItemSummary summary in reader.Summaries)
            {
                var status = new MigratedWorkItem(summary.Id, MigrationState.PartiallyMigrated);
                partiallyMigratedItems.Add(status);
            }

            destManager.Setup(m => m.GetMigratedWorkItemsAsync()).ReturnsAsync(partiallyMigratedItems);

            destManager.Setup(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>())).ThrowsAsync(new HttpRequestFailedException("Mock exception"));

            // Act and Assert
            await Assert.ThrowsAsync<HttpRequestFailedException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, migrationSettings, logger.Object));
            // MaxRetryCount = 3 however this is retries, we also call once for the initial trial...
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Exactly(4));
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_NonHttpRequestFailedExceptionOnUpdateWorkItemAsync_NotRetried_Throws()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(1);

            var partiallyMigratedItems = new List<MigratedWorkItem>();

            foreach (WorkItemSummary summary in reader.Summaries)
            {
                var status = new MigratedWorkItem(summary.Id, MigrationState.PartiallyMigrated);
                partiallyMigratedItems.Add(status);
            }

            destManager.Setup(m => m.GetMigratedWorkItemsAsync()).ReturnsAsync(partiallyMigratedItems);

            destManager.Setup(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>())).ThrowsAsync(new WorkItemMigratorTestsException("Mock exception"));

            // Act and Assert
            await Assert.ThrowsAsync<WorkItemMigratorTestsException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object));
            destManager.Verify(m => m.UpdateWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Once);
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_HttpRequestFailedExceptionWriteWorkItemAsync_Retried_AfterRetriesThrows()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();
            var migrationSettings = MigrationSettings.DefaultSettings;

            migrationSettings.RetryDelay = TimeSpan.Zero;
            reader.BuildWorkItemLists(1);

            destManager.Setup(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>())).ThrowsAsync(new HttpRequestFailedException("Mock exception"));

            // Act and Assert
            await Assert.ThrowsAsync<HttpRequestFailedException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, migrationSettings, logger.Object));
            // MaxRetryCount = 3 however this is retries, we also call once for the initial trial...
            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Exactly(4));
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_NonHttpRequestFailedExceptionOnWriteToWorkItemAsync_Throws()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(1);

            destManager.Setup(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>())).Throws(new WorkItemMigratorTestsException("Mock exception"));

            // Act and Assert
            await Assert.ThrowsAsync<WorkItemMigratorTestsException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object));
            destManager.Verify(m => m.WriteWorkItemAsync(It.IsAny<WorkItemDetails>()), Times.Once);
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task Migrate_OnDuplicateCodePlexWorkItemIds_Throws()
        {
            // Arrange
            var reader = new CodePlexReaderMock();
            var destManager = new Mock<IWorkItemDestination>();
            var logger = new Mock<ILogger>();

            reader.BuildWorkItemLists(1);

            var migratedItems = new List<MigratedWorkItem>();
            foreach (WorkItemSummary summary in reader.Summaries)
            {
                var status = new MigratedWorkItem(summary.Id, MigrationState.Migrated);
                migratedItems.Add(status);
                migratedItems.Add(status);  // Add same issue twice so we get a duplicae CodePlex work item.
            }

            destManager.Setup(m => m.GetMigratedWorkItemsAsync()).ReturnsAsync(migratedItems);

            // Act and Assert
            await Assert.ThrowsAsync<WorkItemIdentificationException>(() => WorkItemMigrator.MigrateAsync(reader.mockReader.Object, destManager.Object, MigrationSettings.DefaultSettings, logger.Object));
            destManager.Verify(m => m.GetMigratedWorkItemsAsync(), Times.Once);
            logger.Verify(m => m.LogMessage(LogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }
    }

    internal class WorkItemMigratorTestsException : Exception
    {
        public WorkItemMigratorTestsException()
        {
        }

        public WorkItemMigratorTestsException(string message) : base(message)
        {
        }

        public WorkItemMigratorTestsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WorkItemMigratorTestsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
