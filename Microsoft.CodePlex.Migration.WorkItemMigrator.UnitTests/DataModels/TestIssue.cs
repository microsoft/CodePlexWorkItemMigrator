using System.Collections.Generic;
using Octokit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal class TestIssue : Issue
    {
        public new int Id
        {
            get { return base.Id; }
            set { base.Id = value; }
        }

        public new int Number
        {
            get { return base.Number; }
            set { base.Number = value; }
        }

        public new string Title
        {
            get { return base.Title; }
            set { base.Title = value; }
        }

        public new string Body
        {
            get { return base.Body; }
            set { base.Body = value; }
        }

        public new IReadOnlyList<Label> Labels
        {
            get { return base.Labels; }
            set { base.Labels = value; }
        }

        public int WorkItemId { get; set; }
    }
}
