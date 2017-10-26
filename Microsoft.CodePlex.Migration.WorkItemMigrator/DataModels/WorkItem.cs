using System;
using System.Collections.Generic;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class WorkItemDetails
    {
        public WorkItem WorkItem { get; set; }
        public IEnumerable<WorkItemFileAttachment> FileAttachments { get; set; }
        public IEnumerable<WorkItemComment> Comments { get; set; }
        public bool CanDeleteWorkItem { get; set; }
        public bool CanDeleteComments { get; set; }
    }

    internal class WorkItem
    {
        public WorkItemComponent AffectedComponent { get; set; }
        public string AssignedTo { get; set; }
        public string ClosedBy { get; set; }
        public string ClosedComment { get; set; }
        public DateTimeOffset? ClosedDate { get; set; }
        public int CommentCount { get; set; }
        public string Custom { get; set; }
        public string Description { get; set; }
        public string PlainDescription { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTimeOffset LastUpdatedDate { get; set; }
        public string PlannedForRelease { get; set; }
        public bool ReleaseVisibleToPublic { get; set; }
        public WorkItemPriority Priority { get; set; }
        public string ProjectName { get; set; }
        public string ReportedBy { get; set; }
        public DateTimeOffset ReportedDate { get; set; }
        public bool CanContactReportedByUser { get; set; }
        public WorkItemStatus Status { get; set; }
        public WorkItemReasonClosed ReasonClosed { get; set; }
        public string Summary { get; set; }
        public WorkItemType Type { get; set; }
        public int VoteCount { get; set; }
        public int Id { get; set; }
        public string HtmlDescription { get; set; }
    }

    internal class WorkItemComponent
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

    internal class WorkItemPriority
    {
        public string Name { get; set; }
        public int Severity { get; set; }
        public int Id { get; set; }
    }

    internal class WorkItemStatus
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    internal class WorkItemReasonClosed
    {
        public string Name { get; set; }
    }

    internal class WorkItemType
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    internal class WorkItemFileAttachment
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public Uri DownloadUrl { get; set; }
        public int WorkItemId { get; set; }
    }

    internal class WorkItemComment
    {
        public string Message { get; set; }
        public string PostedBy { get; set; }
        public DateTimeOffset PostedDate { get; set; }
        public int WorkItemId { get; set; }
        public int Id { get; set; }
    }
}
