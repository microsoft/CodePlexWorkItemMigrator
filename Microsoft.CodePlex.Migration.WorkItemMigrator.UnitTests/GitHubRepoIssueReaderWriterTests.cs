using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Octokit;
using Xunit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    public class GitHubRepoIssueReaderWriterTests
    {
        private static readonly Random RandomGenerator = new Random(Guid.NewGuid().GetHashCode());

        #region Ctor Tests

        public class CtorTests
        {
            [Fact]
            public static void ThrowsOnNullArgs()
            {
                Assert.Throws<ArgumentNullException>(() => CreateTarget(new CtorArgs { RepoOwner = null }));
                Assert.Throws<ArgumentNullException>(() => CreateTarget(new CtorArgs { Repo = null }));
                Assert.Throws<ArgumentNullException>(() => CreateTarget(new CtorArgs { Issues = null }));
                Assert.Throws<ArgumentNullException>(() => CreateTarget(new CtorArgs { Search = null }));
            }

            [Fact]
            public static void ThrowsOnEmptyOrWhiteSpaceRepoOwnerOrRepo()
            {
                Assert.Throws<ArgumentException>(() => CreateTarget(new CtorArgs { RepoOwner = string.Empty }));
                Assert.Throws<ArgumentException>(() => CreateTarget(new CtorArgs { RepoOwner = " " }));
                Assert.Throws<ArgumentException>(() => CreateTarget(new CtorArgs { Repo = string.Empty }));
                Assert.Throws<ArgumentException>(() => CreateTarget(new CtorArgs { Repo = " " }));
            }
        }

        #endregion

        #region IWorkItemDestination Tests

        #region CreateWorkItemAsync Tests

        [Fact]
        public async Task CreateWorkItemAsync_OnNullWorkItemDetailsOrWorkItem_Throws()
        {
            // Arrange
            GitHubRepoIssueReaderWriter target = CreateTarget();

            // Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.WriteWorkItemAsync(workItemDetails: null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.WriteWorkItemAsync(new WorkItemDetails { WorkItem = null }));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task CreateWorkItemAsync_OnNonNullWorkItemDetailsAndWorkItem_CreatesIssue(bool isIssueClosed, bool hasAttachments)
        {
            // Arrange
            var issuesMock = new GitHubIssueMock();
            var ctorArgs = new CtorArgs { Issues = issuesMock.Issues };
            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);
            WorkItemDetails workItemDetails = CreateSampleWorkItemDetails(hasAttachments: hasAttachments, isClosed: isIssueClosed);

            // Act
            await target.WriteWorkItemAsync(workItemDetails);

            // Assert: Owner + Repo
            Assert.NotNull(issuesMock.CreateIssueArgs.NewIssue);
            Assert.Equal(ctorArgs.RepoOwner, issuesMock.CreateIssueArgs.Owner);
            Assert.Equal(ctorArgs.Repo, issuesMock.CreateIssueArgs.Name);

            // Assert: Title + Body
            Assert.Equal(workItemDetails.WorkItem.Summary, issuesMock.CreateIssueArgs.NewIssue.Title);
            Assert.Contains(TextUtilities.GetFormattedWorkItemBody(workItemDetails.WorkItem, workItemDetails.FileAttachments), issuesMock.CreateIssueArgs.NewIssue.Body);

            // Assert: Labels
            Assert.Single(issuesMock.CreateIssueArgs.NewIssue.Labels);
            Assert.Contains(issuesMock.CreateIssueArgs.NewIssue.Labels, label => label == GitHubLabels.CodePlexMigrationInitiated);
            Assert.Single(issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            Assert.Contains(issuesMock.UpdateIssueArgs.IssueUpdate.Labels, label => label == GitHubLabels.CodePlexMigrated);

            // Assert: Attachments
            if (hasAttachments)
            {
                Assert.Contains(Resources.Attachments, issuesMock.CreateIssueArgs.NewIssue.Body);
            }
            else
            {
                Assert.DoesNotContain(Resources.Attachments, issuesMock.CreateIssueArgs.NewIssue.Body);
            }

            // Assert: Issue state: closed/open
            Assert.Equal(isIssueClosed ? ItemState.Closed : (ItemState?)null, issuesMock.UpdateIssueArgs.IssueUpdate.State);

            issuesMock.VerifyIssuesCallCount(methodName: nameof(IIssuesClient.Create), callCount: 1);
            issuesMock.VerifyIssuesCallCount(methodName: nameof(IIssuesClient.Update), callCount: 1);
            issuesMock.VerifyCommentCallCount(methodName: nameof(IIssueCommentsClient.Create), callCount: 0);
            issuesMock.VerifyCommentCallCount(methodName: nameof(IIssueCommentsClient.Delete), callCount: 0);
        }

        [Fact]
        public async Task CreateWorkItemAsync_OnWorkItemWithComments_CreatesCommentsOnIssue()
        {
            // Arrange
            var issuesMock = new GitHubIssueMock();
            var ctorArgs = new CtorArgs { Issues = issuesMock.Issues };
            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);
            WorkItemDetails workItemDetails = CreateSampleWorkItemDetails(hasComments: true);

            // Act
            await target.WriteWorkItemAsync(workItemDetails);

            // Assert
            Assert.True(issuesMock.CreateCommentArgs.All(argSet => argSet.Name == ctorArgs.Repo && argSet.Owner == ctorArgs.RepoOwner));
            issuesMock.VerifyCommentCallCount(methodName: nameof(IIssueCommentsClient.Create), callCount: workItemDetails.Comments.Count());
            foreach (WorkItemComment comment in workItemDetails.Comments)
            {
                Assert.Contains(issuesMock.CreateCommentArgs, argSet => argSet.NewComment.Contains(comment.Message));
            }
        }

        #region Label Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateWorkItemAsync_OnDuplicateWorkItem_SetsDuplicateLabel(bool isIssueDuplicate)
        {
            // Arrange
            var issuesMock = new GitHubIssueMock();
            var ctorArgs = new CtorArgs { Issues = issuesMock.Issues };
            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);
            WorkItemDetails workItemDetails = CreateSampleWorkItemDetails(isDuplicate: isIssueDuplicate);

            // Act
            await target.WriteWorkItemAsync(workItemDetails);

            // Assert
            if (isIssueDuplicate)
            {
                Assert.Equal(2, issuesMock.UpdateIssueArgs.IssueUpdate.Labels.Count);
                Assert.Contains(GitHubLabels.Duplicate, issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            }
            else
            {
                Assert.Single(issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            }

            Assert.Contains(GitHubLabels.CodePlexMigrated, issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
        }

        [Fact]
        public async Task CreateWorkItemAsync_OnWorkItemWithReleaseComponentAndPriority_SetsTextLabels()
        {
            // Arrange
            var issuesMock = new GitHubIssueMock();
            var ctorArgs = new CtorArgs { Issues = issuesMock.Issues };
            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);
            WorkItemDetails workItemDetails = CreateSampleWorkItemDetails(hasSpecialProperties: true);

            // Act
            await target.WriteWorkItemAsync(workItemDetails);

            // Assert
            Assert.Equal(4, issuesMock.UpdateIssueArgs.IssueUpdate.Labels.Count);
            Assert.Contains(workItemDetails.WorkItem.PlannedForRelease, issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            Assert.Contains(workItemDetails.WorkItem.AffectedComponent.DisplayName, issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            Assert.Contains(issuesMock.UpdateIssueArgs.IssueUpdate.Labels, label => label.Contains(GitHubLabels.Impact) && label.Contains(workItemDetails.WorkItem.Priority.Name));
            Assert.Contains(GitHubLabels.CodePlexMigrated, issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
        }

        [Theory]
        [InlineData(CodePlexStrings.Feature, GitHubLabels.Enhancement)]
        [InlineData(CodePlexStrings.Issue, GitHubLabels.Bug)]
        [InlineData(CodePlexStrings.Unassigned, null)]
        [InlineData("Otherwise", "Otherwise")]
        public async Task CreateWorkItemAsync_OnWorkItemWithFeatureIssueAndTextTypes_SetsIssueTypeLabel(string codePlexWorkItemType, string gitHubLabel)
        {
            // Arrange
            var issuesMock = new GitHubIssueMock();
            var ctorArgs = new CtorArgs { Issues = issuesMock.Issues };
            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);
            WorkItemDetails workItemDetails = CreateSampleWorkItemDetails(type: codePlexWorkItemType);

            // Act
            await target.WriteWorkItemAsync(workItemDetails);

            // Assert
            if (gitHubLabel != null)
            {
                Assert.Equal(2, issuesMock.UpdateIssueArgs.IssueUpdate.Labels.Count);
                Assert.Contains(gitHubLabel, issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            }
            else
            {
                Assert.Single(issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            }

            Assert.Contains(GitHubLabels.CodePlexMigrated, issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
        }

        #endregion

        #endregion

        #region GetMigratedWorkItems Tests

        [Fact]
        public async Task GetMigratedWorkItems_IfNoMigratedWorkItems_ReturnsEmptyList()
        {
            // Arrange
            var searchMock = new GitHubSearchByLabelMock();
            GitHubRepoIssueReaderWriter target = CreateTarget(new CtorArgs { Search = searchMock.Search });

            // Act
            IReadOnlyList<MigratedWorkItem> migratedWorkItems = await target.GetMigratedWorkItemsAsync();

            Assert.Equal(0, migratedWorkItems.Count);
            searchMock.VerifySearchCallCount(callCount: 2);
        }

        [Theory]
        [InlineData(GitHubLabels.CodePlexMigrated, nameof(MigrationState.Migrated))]
        [InlineData(GitHubLabels.CodePlexMigrationInitiated, nameof(MigrationState.PartiallyMigrated))]
        public async Task GetMigratedWorkItems_IfOneWorkItemExists_ReturnsWorkItem(string issueLabel, string expectedMigrationState)
        {
            // Arrange
            var searchMock =
                new GitHubSearchByLabelMock()
                    .SetSearchResults(issueLabel, new[] { CreateSampleIssue(issueLabel) });

            GitHubRepoIssueReaderWriter target = CreateTarget(new CtorArgs { Search = searchMock.Search });

            // Act
            IReadOnlyList<MigratedWorkItem> migratedWorkItems = await target.GetMigratedWorkItemsAsync();

            // Assert            
            Assert.Single(migratedWorkItems);
            Assert.True(migratedWorkItems.All(item => item.MigrationState == (MigrationState)Enum.Parse(typeof(MigrationState), expectedMigrationState)));
            searchMock.VerifySearchCallCount(callCount: 2);
        }

        [Fact]
        public async Task GetMigratedWorkItems_IfMultipleWorkItemsExist_ReturnsWorkItems()
        {
            // Arrange
            int migratedIssueCount = 2;
            int partiallyMigratedIssueCount = 3;

            // 2 calls for migrated issues + 2 calls for partially migrated issues since Search mock paginates any 
            // search result set passed to it via SetSearchResults().
            int expectedSearchCallCount = 4;

            TestIssue[] migratedIssues = Enumerable.Range(0, migratedIssueCount).Select(i => CreateSampleIssue(GitHubLabels.CodePlexMigrated)).ToArray();
            TestIssue[] partiallyMigratedIssues = Enumerable.Range(0, partiallyMigratedIssueCount).Select(i => CreateSampleIssue(GitHubLabels.CodePlexMigrationInitiated)).ToArray();

            var searchMock =
                new GitHubSearchByLabelMock()
                    .SetSearchResults(GitHubLabels.CodePlexMigrated, migratedIssues)
                    .SetSearchResults(GitHubLabels.CodePlexMigrationInitiated, partiallyMigratedIssues);

            var ctorArgs = new CtorArgs { Search = searchMock.Search };
            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);

            // Act
            IReadOnlyList<MigratedWorkItem> migratedWorkItems = await target.GetMigratedWorkItemsAsync();

            // Assert            
            Assert.Equal(migratedIssueCount + partiallyMigratedIssueCount, migratedWorkItems.Count);

            foreach (MigratedWorkItem migratedWorkItem in migratedWorkItems)
            {
                switch (migratedWorkItem.MigrationState)
                {
                    case MigrationState.Migrated:
                        Assert.Contains(migratedIssues, issue => issue.WorkItemId == migratedWorkItem.CodePlexWorkItemId);
                        break;
                    case MigrationState.PartiallyMigrated:
                        Assert.Contains(partiallyMigratedIssues, issue => issue.WorkItemId == migratedWorkItem.CodePlexWorkItemId);
                        break;
                    default:
                        Assert.True(false, "Unexpected work item migration state");
                        break;
                }
            }

            Assert.Equal(IssueTypeQualifier.Issue, searchMock.SearchIssuesRequest.Type);
            Assert.Single(searchMock.SearchIssuesRequest.Repos);
            Assert.Equal(ctorArgs.FullRepoName, searchMock.SearchIssuesRequest.Repos[0]);
            searchMock.VerifySearchCallCount(callCount: expectedSearchCallCount);
        }

        [Theory]
        [InlineData(GitHubLabels.CodePlexMigrated, "invalid-Int32-value")]
        [InlineData(GitHubLabels.CodePlexMigrationInitiated, "invalid-Int32-value")]
        [InlineData(GitHubLabels.CodePlexMigrated, "4294967296")]
        [InlineData(GitHubLabels.CodePlexMigrationInitiated, "4294967296")]
        [InlineData(GitHubLabels.CodePlexMigrated, "")]
        [InlineData(GitHubLabels.CodePlexMigrationInitiated, "")]
        public async Task GetMigratedWorkItems_OnInvalidWorkItemId_Throws(string issueLabel, string workItemStringValue)
        {
            // Arrange
            var searchMock =
                new GitHubSearchByLabelMock()
                    .SetSearchResults(
                        issueLabel, new[] { CreateSampleIssue(issueLabel, body: string.Format(TextUtilities.CodePlexWorkItemFormat, workItemStringValue)) });

            // Act
            GitHubRepoIssueReaderWriter target = CreateTarget(new CtorArgs { Search = searchMock.Search });

            // Assert
            await Assert.ThrowsAsync<WorkItemIdentificationException>(() => target.GetMigratedWorkItemsAsync());
        }

        #endregion

        #region UpdateMigratedWorkItems Tests

        [Fact]
        public async Task UpdateWorkItemAsync_OnNullWorkItemDetailsOrWorkItem_Throws()
        {
            // Arrange
            GitHubRepoIssueReaderWriter target = CreateTarget(new CtorArgs { Search = new GitHubSearchInBodyMock(searchResults: null).Search });

            // Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.UpdateWorkItemAsync(workItemDetails: null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.UpdateWorkItemAsync(new WorkItemDetails { WorkItem = null }));
        }

        [Fact]
        public async Task UpdateWorkItemAsync_IfZeroWorkItemsExist_Throws()
        {
            // Arrange
            var searchMock = new GitHubSearchInBodyMock(searchResults: null);

            // Act
            GitHubRepoIssueReaderWriter target = CreateTarget(new CtorArgs { Search = searchMock.Search });

            // Assert
            await Assert.ThrowsAsync<WorkItemIdentificationException>(() => target.UpdateWorkItemAsync(CreateSampleWorkItemDetails()));
        }

        [Fact]
        public async Task UpdateWorkItemAsync_IfMultipleWorkItemsExist_Throws()
        {
            // Arrange
            var searchMock = new GitHubSearchInBodyMock(searchResults: new[] { CreateSampleIssue(), CreateSampleIssue() });

            // Act
            GitHubRepoIssueReaderWriter target = CreateTarget(new CtorArgs { Search = searchMock.Search });

            // Assert
            await Assert.ThrowsAsync<WorkItemIdentificationException>(() => target.UpdateWorkItemAsync(CreateSampleWorkItemDetails()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateWorkItemAsync_IfWorkItemExists_Succeeds(bool hasAttachments)
        {
            // Arrange
            Issue issue = CreateSampleIssue(label: "AnyLabel");
            WorkItemDetails workItemDetails = CreateSampleWorkItemDetails(hasAttachments: hasAttachments);

            var issuesMock = new GitHubIssueMock();
            var searchMock = new GitHubSearchInBodyMock(searchResults: new[] { issue });
            CtorArgs ctorArgs = new CtorArgs { Search = searchMock.Search, Issues = issuesMock.Issues };

            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);

            // Act
            await target.UpdateWorkItemAsync(workItemDetails);

            // Assert: Search
            Assert.Contains(workItemDetails.WorkItem.Id.ToString(), searchMock.SearchIssuesRequest.Term);
            Assert.Equal(IssueTypeQualifier.Issue, searchMock.SearchIssuesRequest.Type);
            Assert.Single(searchMock.SearchIssuesRequest.Repos);
            Assert.Equal(ctorArgs.FullRepoName, searchMock.SearchIssuesRequest.Repos[0]);
            Assert.Contains(searchMock.SearchIssuesRequest.In, q => q == IssueInQualifier.Body);
            searchMock.VerifySearchCallCount(callCount: 1);

            // Assert: Update -- Owner + Repo + Issue number
            Assert.Equal(ctorArgs.RepoOwner, issuesMock.UpdateIssueArgs.Owner);
            Assert.Equal(ctorArgs.Repo, issuesMock.UpdateIssueArgs.Name);
            Assert.Equal(issue.Number, issuesMock.UpdateIssueArgs.Number);

            // Assert: Update -- Title + Body
            Assert.Equal(workItemDetails.WorkItem.Summary, issuesMock.UpdateIssueArgs.IssueUpdate.Title);
            Assert.Contains(TextUtilities.GetFormattedWorkItemBody(workItemDetails.WorkItem, workItemDetails.FileAttachments), issuesMock.UpdateIssueArgs.IssueUpdate.Body);

            // Assert: Update -- Labels
            Assert.Single(issuesMock.UpdateIssueArgs.IssueUpdate.Labels);
            Assert.Contains(issuesMock.UpdateIssueArgs.IssueUpdate.Labels, label => label == GitHubLabels.CodePlexMigrated);

            // Assert: Update -- Attachments
            if (hasAttachments)
            {
                Assert.Contains(Resources.Attachments, issuesMock.UpdateIssueArgs.IssueUpdate.Body);
            }
            else
            {
                Assert.DoesNotContain(Resources.Attachments, issuesMock.UpdateIssueArgs.IssueUpdate.Body);
            }

            issuesMock.VerifyIssuesCallCount(methodName: nameof(IIssuesClient.Update), callCount: 2);
        }

        [Fact]
        public async Task UpdateWorkItemAsync_IfWorkItemHasComments_DropsAndRecreatesComments()
        {
            // Arrange
            int issueCommentCount = 10;
            Issue issue = CreateSampleIssue(label: "AnyLabel");
            IssueComment[] issueComments = Enumerable.Range(0, issueCommentCount).Select(i => CreateSampleComment(issue.Number)).ToArray();
            WorkItemDetails workItemDetails = CreateSampleWorkItemDetails(hasComments: true);

            var issuesMock = new GitHubIssueMock();
            issuesMock.SetCommentsForIssue(issue.Number, issueComments);

            var searchMock = new GitHubSearchInBodyMock(searchResults: new[] { issue });
            CtorArgs ctorArgs = new CtorArgs { Search = searchMock.Search, Issues = issuesMock.Issues };

            GitHubRepoIssueReaderWriter target = CreateTarget(ctorArgs);

            // Act
            await target.UpdateWorkItemAsync(workItemDetails);

            // Assert: Search
            searchMock.VerifySearchCallCount(callCount: 1);

            // Assert: Delete comments
            Assert.True(issuesMock.DeleteCommentArgs.All(args => args.Owner == ctorArgs.RepoOwner));
            Assert.True(issuesMock.DeleteCommentArgs.All(args => args.Name == ctorArgs.Repo));
            Assert.Equal(issueComments.Select(c => c.Id), issuesMock.DeleteCommentArgs.Select(args => args.Id));
            issuesMock.VerifyCommentCallCount(nameof(IIssueCommentsClient.Delete), callCount: issueCommentCount);

            // Assert: Create comments
            Assert.True(issuesMock.CreateCommentArgs.All(args => args.Owner == ctorArgs.RepoOwner));
            Assert.True(issuesMock.CreateCommentArgs.All(args => args.Name == ctorArgs.Repo));
            Assert.True(issuesMock.CreateCommentArgs.All(args => args.Number == issue.Number));

            string[] createdComments = issuesMock.CreateCommentArgs.Select(c => c.NewComment).ToArray();
            foreach (WorkItemComment comment in workItemDetails.Comments)
            {
                Assert.Contains(createdComments, c => c.Contains(comment.Message));
            }

            issuesMock.VerifyCommentCallCount(nameof(IIssueCommentsClient.Create), callCount: workItemDetails.Comments.Count());
        }

        #endregion

        #endregion

        #region Helpers

        private static WorkItemDetails CreateSampleWorkItemDetails(
            string type = null,
            bool isClosed = false,
            bool isDuplicate = false,
            bool hasSpecialProperties = false,
            bool hasComments = false,
            bool hasAttachments = false)
        {
            int r = RandomGenerator.Next(Int32.MaxValue);

            var result = new WorkItemDetails
            {
                WorkItem = new WorkItem
                {
                    Id = r,
                    Summary = "WorkItemSummary-" + r,
                    PlainDescription = "PlainDescription-" + r,
                }
            };

            if (type != null)
            {
                result.WorkItem.Type = new WorkItemType { Id = r, Name = type };
            }

            if (isClosed)
            {
                result.WorkItem.Status = new WorkItemStatus { Id = r, Name = CodePlexStrings.Closed };
            }

            if (isDuplicate)
            {
                result.WorkItem.ReasonClosed = new WorkItemReasonClosed { Name = isDuplicate ? CodePlexStrings.Duplicate : "Reason" };
            }

            if (hasSpecialProperties)
            {
                result.WorkItem.PlannedForRelease = "RTM";
                result.WorkItem.AffectedComponent = new WorkItemComponent { DisplayName = "ComponentDisplayName-" + r, Name = "ComponentName-" + r };
                result.WorkItem.Priority = new WorkItemPriority { Id = 0, Name = "PriorityName-" + r, Severity = 0 };
            }

            if (hasComments)
            {
                result.Comments = new[]
                {
                    new WorkItemComment
                    {
                        Id = r,
                        Message = "CommentMessage-" + r,
                        PostedBy = "AuthorName-" + r,
                        PostedDate = DateTimeOffset.UtcNow,
                        WorkItemId = result.WorkItem.Id,
                    },
                };
            }

            if (hasAttachments)
            {
                result.FileAttachments = new[]
                {
                    new WorkItemFileAttachment
                    {
                        FileId = r,
                        FileName = "FileName-" + r,
                        DownloadUrl = new Uri("http://file-url-" + r),
                        WorkItemId = result.WorkItem.Id,
                    },
                };
            }

            return result;
        }

        private static TestIssue CreateSampleIssue(string label = null, string body = null)
        {
            int id = RandomGenerator.Next(int.MaxValue);
            var workItem = new WorkItem { Id = RandomGenerator.Next(Int32.MaxValue) };

            var result = new TestIssue
            {
                Id = id,
                Number = id,
                Title = "Issue-" + id,
                Body = body ?? TextUtilities.GetFormattedWorkItemBody(workItem, attachments: null),
                WorkItemId = workItem.Id,
            };

            if (label != null)
            {
                result.Labels = new[] { new TestLabel { Name = label } };
            }

            return result;
        }

        private static IssueComment CreateSampleComment(int issueNumber)
        {
            int r = RandomGenerator.Next(int.MaxValue);

            return new TestIssueComment
            {
                Id = r,
                Body = "CommentBody-" + r,
            };
        }

        private static GitHubRepoIssueReaderWriter CreateTarget(CtorArgs ctorArgs = null)
        {
            CtorArgs args = ctorArgs ?? new CtorArgs();
            return new GitHubRepoIssueReaderWriter(args.RepoOwner, args.Repo, args.Issues, args.Search);
        }

        #endregion

        private class CtorArgs
        {
            private static readonly IIssuesClient IssuesMock = new Mock<IIssuesClient>().Object;
            private static readonly ISearchClient SearchMock = new Mock<ISearchClient>().Object;

            public string RepoOwner { get; set; }
            public string Repo { get; set; }
            public string FullRepoName => $"{RepoOwner}/{Repo}";
            public IIssuesClient Issues { get; set; }
            public ISearchClient Search { get; set; }

            public CtorArgs()
            {
                RepoOwner = "OwnerName";
                Repo = "RepoName";
                Search = SearchMock;
                Issues = IssuesMock;
            }
        }
    }
}
