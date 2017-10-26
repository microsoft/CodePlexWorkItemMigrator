using System;
using System.Runtime.Serialization;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    [Serializable]
    internal class WorkItemIdentificationException : Exception
    {
        public WorkItemIdentificationException()
        {
        }

        public WorkItemIdentificationException(string message) : base(message)
        {
        }

        public WorkItemIdentificationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WorkItemIdentificationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}