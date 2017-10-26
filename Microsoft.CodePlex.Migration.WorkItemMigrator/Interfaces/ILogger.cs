namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal interface ILogger
    {
        void LogMessage(LogLevel level, string format, params object[] args);
    }
}
