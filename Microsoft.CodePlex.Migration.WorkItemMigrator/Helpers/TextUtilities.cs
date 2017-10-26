using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal static class TextUtilities
    {
        private static readonly StringComparer StringComparer = StringComparer.OrdinalIgnoreCase;

        internal static readonly string CodePlexWorkItemFormat = GetPropertyFormat(Resources.CodePlexWorkItemId);
        private static readonly string CodePlexWorkItemIdPattern = string.Format(CodePlexWorkItemFormat, @"(?<ID>\d+)");
        private static readonly Regex CodePlexWorkItemIdRegex = new Regex(CodePlexWorkItemIdPattern, RegexOptions.Compiled);

        /// <summary>
        /// Builds a list of strings where each string should be added as a comment to the GitHub issue.
        /// </summary>
        public static IEnumerable<string> GetFormattedComments(WorkItemDetails workItemDetails)
        {
            ArgValidate.IsNotNull(workItemDetails, nameof(workItemDetails));

            var stringBuilder = new StringBuilder();

            // Format and return all work item comments.
            foreach (WorkItemComment comment in workItemDetails.Comments.EmptyIfNull())
            {
                string postedBy = comment.PostedBy;
                if (string.IsNullOrEmpty(postedBy))
                {
                    postedBy = CodePlexStrings.UnknownUser;
                }

                string commentHeading = string.Format(Resources.CommentPostedByPersonXOnDateY, postedBy, comment.PostedDate.ToString("d"));

                stringBuilder
                    .AppendLine(commentHeading)
                    .AppendLine(comment.Message)
                    .AppendLine();

                yield return stringBuilder.ToString();
                stringBuilder.Clear();
            }

            // Format closing comment if any is specified.
            string closingComment = GetFormattedClosingComment(workItemDetails);
            if (!string.IsNullOrEmpty(closingComment))
            {
                yield return closingComment;
            }
        }

        /// <summary>
        /// Returns the CodePlex work item ID from the body of a GitHub issue.
        /// </summary>
        public static int GetCodePlexWorkItemId(string issueBody)
        {
            ArgValidate.IsNotNull(issueBody, nameof(issueBody));

            Match match = CodePlexWorkItemIdRegex.Match(issueBody);
            if (match.Success)
            {
                string idString = match.Groups["ID"].Value;

                try
                {
                    return Convert.ToInt32(idString);
                }
                catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                {
                    throw new WorkItemIdentificationException(string.Format(Resources.InvalidCodePlexWorkItemId, idString), ex);
                }
            }

            throw new WorkItemIdentificationException(Resources.CodePlexWorkItemIdNotFoundInGitHubIssueBody);
        }

        /// <summary>
        /// Builds a formatted string out of a CodePlex work item ID to be written in its corresponding GitHub issue.
        /// </summary>
        public static string GetFormattedWorkItemId(int workItemId) => string.Format(CodePlexWorkItemFormat, workItemId);

        /// <summary>
        /// Builds the string to write as the main body of a GitHub issue.
        /// </summary>
        public static string GetFormattedWorkItemBody(WorkItem workItem, IEnumerable<WorkItemFileAttachment> attachments)
        {
            ArgValidate.IsNotNull(workItem, nameof(workItem));

            var stringBuilder =
                new StringBuilder()
                    .AppendLine(workItem.PlainDescription);

            string attachmentLinks = GetFormattedAttachmentHyperlinks(attachments);
            if (!string.IsNullOrEmpty(attachmentLinks))
            {
                stringBuilder
                    .AppendLine()
                    .Append(attachmentLinks);
            }

            return stringBuilder
                .AppendLine()
                .Append(GetFormattedCodePlexWorkItemDetails(workItem))
                .ToString();
        }

        private static string GetFormattedAttachmentHyperlinks(IEnumerable<WorkItemFileAttachment> attachments)
        {
            bool hasAttachments = attachments?.Any() ?? false;
            if (hasAttachments)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(MarkdownFormatter.H4(Resources.Attachments));

                foreach (WorkItemFileAttachment attachment in attachments)
                {
                    stringBuilder.AppendLine(MarkdownFormatter.HyperLink(attachment.FileName, attachment.DownloadUrl));
                }

                return stringBuilder.ToString();
            }

            return string.Empty;
        }

        private static string GetFormattedClosingComment(WorkItemDetails workItemDetails)
        {
            string closingComment = workItemDetails.WorkItem.ClosedComment;
            string closedBy = workItemDetails.WorkItem.ClosedBy;
            string reasonClosed = workItemDetails.WorkItem.ReasonClosed?.Name;

            // TODO: Produce a different comment if closedBy != null.
            if (!string.IsNullOrEmpty(closingComment) &&
                !string.IsNullOrEmpty(closedBy) &&
                !string.IsNullOrEmpty(reasonClosed))
            {
                string closingCommentTitle = string.Format(Resources.IssueClosedByXWithComment, MarkdownFormatter.Italic(closedBy));

                var stringBuilder =
                    new StringBuilder()
                        .AppendLine(MarkdownFormatter.Bold(closingCommentTitle))
                        .AppendLine(closingComment);

                if (!string.IsNullOrEmpty(reasonClosed) && !StringComparer.Equals(reasonClosed, CodePlexStrings.Unassigned))
                {
                    stringBuilder
                        .AppendLine()
                        .AppendLine(MarkdownFormatter.Bold(Resources.ReasonClosed))
                        .AppendLine(reasonClosed);
                }

                return stringBuilder.ToString();
            }

            return string.Empty;
        }

        private static string GetFormattedCodePlexWorkItemDetails(WorkItem workItem)
        {
            var stringBuilder = new StringBuilder()
                    .AppendLine(MarkdownFormatter.H4(Resources.MigratedCodePlexWorkItemDetails))
                    .AppendLine(GetFormattedWorkItemId(workItem.Id));

            if (!string.IsNullOrEmpty(workItem.AssignedTo))
            {
                stringBuilder.AppendLine(string.Format(GetPropertyFormat(Resources.AssignedTo), workItem.AssignedTo));
            }

            return stringBuilder
                    .AppendLine(string.Format(GetPropertyFormat(Resources.VoteCount), workItem.VoteCount))
                    .ToString();
        }

        private static string GetPropertyFormat(string name) => $"{name}: '{{0}}'";
    }
}
