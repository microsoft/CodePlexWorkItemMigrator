using Octokit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal class TestIssueComment : IssueComment
    {
        public new int Id
        {
            get { return base.Id; }
            set { base.Id = value; }
        }

        public new string Body
        {
            get { return base.Body; }
            set { base.Body = value; }
        }
    }
}
