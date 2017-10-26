using Octokit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal class TestLabel : Label
    {
        public new string Name
        {
            get { return base.Name; }
            set { base.Name = value; }
        }
    }
}
