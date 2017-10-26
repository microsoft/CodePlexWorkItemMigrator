using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Octokit;
using Xunit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal class GitHubIssueMock
    {
        private static readonly Random RandomGenerator = new Random(Guid.NewGuid().GetHashCode());
        private static readonly Task CompletedTask = Task.FromResult(true);

        private readonly Mock<IIssuesClient> issues;
        private readonly Mock<IIssueCommentsClient> comment;
        private readonly List<CreateCommentArguments> createCommentArgs;
        private readonly List<DeleteCommentArguments> deleteCommentArgs;
        private Dictionary<int, IssueComment[]> issueComments;

        public IIssuesClient Issues => issues.Object;
        public IIssueCommentsClient Comment => comment.Object;

        public CreateArguments CreateIssueArgs { get; private set; }
        public UpdateArguments UpdateIssueArgs { get; private set; }
        public IReadOnlyList<CreateCommentArguments> CreateCommentArgs => createCommentArgs;
        public IReadOnlyList<DeleteCommentArguments> DeleteCommentArgs => deleteCommentArgs;
        public GetAllCommentsForIssueArguments GetAllCommentsForIssueArgs { get; private set; }

        public GitHubIssueMock()
        {
            issues = new Mock<IIssuesClient>(MockBehavior.Strict);

            issues
                .Setup(issues => issues.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewIssue>()))
                .ReturnsAsync((Func<string, string, NewIssue, Issue>)CreateIssue)
                .Callback<string, string, NewIssue>(
                    (owner, name, newIssue) =>
                    {
                        CreateIssueArgs = new CreateArguments(owner, name, newIssue);
                    });

            issues
                .Setup(issues => issues.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IssueUpdate>()))
                .ReturnsAsync((Func<string, string, int, IssueUpdate, Issue>)UpdateIssue)
                .Callback<string, string, int, IssueUpdate>(
                    (owner, name, number, issueUpdate) =>
                    {
                        UpdateIssueArgs = new UpdateArguments(owner, name, number, issueUpdate);
                    });

            comment = new Mock<IIssueCommentsClient>(MockBehavior.Strict);

            comment
                .Setup(comment => comment.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((Func<string, string, int, string, IssueComment>)CreateComment)
                .Callback<string, string, int, string>(
                    (owner, name, number, newComment) =>
                    {
                        createCommentArgs.Add(new CreateCommentArguments(owner, name, number, newComment));
                    });

            comment
                .Setup(comment => comment.Delete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(CompletedTask)
                .Callback<string, string, int>(
                    (owner, name, number) =>
                    {
                        deleteCommentArgs.Add(new DeleteCommentArguments(owner, name, number));
                    });

            comment
                .Setup(comment => comment.GetAllForIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((Func<string, string, int, IReadOnlyList<IssueComment>>)GetAllCommentsForIssue)
                .Callback<string, string, int>(
                    (owner, name, number) =>
                    {
                        GetAllCommentsForIssueArgs = new GetAllCommentsForIssueArguments(owner, name, number);
                    });

            issues.Setup(issues => issues.Comment).Returns(Comment);

            createCommentArgs = new List<CreateCommentArguments>();
            deleteCommentArgs = new List<DeleteCommentArguments>();
            issueComments = new Dictionary<int, IssueComment[]>();
        }

        public void SetCommentsForIssue(int number, IEnumerable<IssueComment> comments) => issueComments[number] = comments.ToArray();

        #region VerifyCallCount Methods

        public void VerifyIssuesCallCount(string methodName, int callCount)
        {
            if (methodName == nameof(IIssuesClient.Create))
            {
                issues.Verify(
                    issues => issues.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewIssue>()), Times.Exactly(callCount));
            }
            else if (methodName == nameof(IIssuesClient.Update))
            {
                issues.Verify(
                    issues => issues.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IssueUpdate>()), Times.Exactly(callCount));
            }
            else
            {
                Assert.True(false, "Unknown method name");
            }
        }

        public void VerifyCommentCallCount(string methodName, int callCount)
        {
            if (methodName == nameof(IIssueCommentsClient.Create))
            {
                comment.Verify(
                    comment => comment.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(callCount));
            }
            else if (methodName == nameof(IIssueCommentsClient.Delete))
            {
                comment.Verify(
                    comment => comment.Delete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(callCount));
            }
            else if (methodName == nameof(IIssueCommentsClient.GetAllForIssue))
            {
                comment.Verify(
                    comment => comment.GetAllForIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(callCount));
            }
            else
            {
                Assert.True(false, "Unknown method name");
            }
        }

        #endregion

        #region API Methods

        private static Issue CreateIssue(string owner, string name, NewIssue newIssue)
        {
            return new TestIssue
            {
                Id = RandomGenerator.Next(Int32.MaxValue),
                Title = newIssue.Title,
                Body = newIssue.Body,
                Labels = newIssue.Labels.Select(label => new TestLabel { Name = label }).ToArray(),
            };
        }

        private static Issue UpdateIssue(string owner, string name, int number, IssueUpdate issueUpdate)
        {
            return new TestIssue
            {
                Id = RandomGenerator.Next(Int32.MaxValue),
                Number = number,
                Title = issueUpdate.Title,
                Body = issueUpdate.Body,
                Labels = issueUpdate.Labels.Select(label => new TestLabel { Name = label }).ToArray(),
            };
        }

        private static IssueComment CreateComment(string owner, string name, int number, string newComment)
        {
            return new TestIssueComment
            {
                Id = RandomGenerator.Next(Int32.MaxValue),
                Body = newComment,
            };
        }

        private IReadOnlyList<IssueComment> GetAllCommentsForIssue(string owner, string name, int number)
        {
            if (!issueComments.TryGetValue(number, out IssueComment[] result))
            {
                return new IssueComment[0];
            }

            return result;
        }

        #endregion

        #region Argument Classes

        public class CreateArguments
        {
            public string Owner { get; }
            public string Name { get; }
            public NewIssue NewIssue { get; }

            public CreateArguments(string owner, string name, NewIssue newIssue)
            {
                Owner = owner;
                Name = name;
                NewIssue = newIssue;
            }
        }

        public class UpdateArguments
        {
            public string Owner { get; }
            public string Name { get; }
            public int Number { get; }
            public IssueUpdate IssueUpdate { get; }

            public UpdateArguments(string owner, string name, int number, IssueUpdate issueUpdate)
            {
                Owner = owner;
                Name = name;
                Number = number;
                IssueUpdate = issueUpdate;
            }
        }

        public class CreateCommentArguments
        {
            public string Owner { get; }
            public string Name { get; }
            public int Number { get; }
            public string NewComment { get; }

            public CreateCommentArguments(string owner, string name, int number, string newComment)
            {
                Owner = owner;
                Name = name;
                Number = number;
                NewComment = newComment;
            }
        }

        public class GetAllCommentsForIssueArguments
        {
            public string Owner;
            public string Name;
            public int Number;

            public GetAllCommentsForIssueArguments(string owner, string name, int number)
            {
                Owner = owner;
                Name = name;
                Number = number;
            }
        }

        public class DeleteCommentArguments
        {
            public string Owner;
            public string Name;
            public int Id;

            public DeleteCommentArguments(string owner, string name, int id)
            {
                Owner = owner;
                Name = name;
                Id = id;
            }
        }

        #endregion
    }
}
