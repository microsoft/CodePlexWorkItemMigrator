using System.Linq;
using log4net;
using log4net.Appender;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class Logger : ILogger
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(Logger));

        public Logger(string filePath)
        {
            ArgValidate.IsNotNullNotEmptyNotWhiteSpace(filePath, nameof(filePath));

            RollingFileAppender rollingFileAppender = 
                (RollingFileAppender)LogManager
                    .GetRepository()
                    .GetAppenders()
                    .SingleOrDefault(appender => appender is RollingFileAppender);

            if (rollingFileAppender != null)
            {
                rollingFileAppender.File = filePath;
                rollingFileAppender.ActivateOptions();
            }
        }

        #region ILogger

        public void LogMessage(LogLevel level, string format, params object[] args)
        {
            string message = format;
            if (args != null && args.Length > 0)
            {
                message = string.Format(format, args);
            }

            switch (level)
            {
                case LogLevel.Info:
                    Log.Info(message);
                    break;
                case LogLevel.Warning:
                    Log.Warn(message);
                    break;
                case LogLevel.Error:
                    Log.Error(message);
                    break;
                case LogLevel.Trace:
                    Log.Debug(message);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
