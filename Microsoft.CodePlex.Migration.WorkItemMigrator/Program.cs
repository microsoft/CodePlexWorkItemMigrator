using System;
using Octokit;
using Octokit.Internal;
using PowerArgs;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ProgramArguments programArguments = Args.Parse<ProgramArguments>(args);

                if (programArguments != null)
                {
                    using (var httpClient = new HttpClientImpl())
                    {
                        WorkItemMigrator
                            .MigrateAsync(
                                source: new CodePlexWorkItemReader(programArguments.CodePlexProject, programArguments.MigrateClosedItems, httpClient),
                                destination: CreateDestination(programArguments),
                                settings: CreateMigrationSettings(programArguments),
                                logger: new Logger(programArguments.LogFilePath))
                            .Wait();
                    }
                }
            }
            catch (Exception)
            {
                Logger.Log.Error(Resources.UnrecoverableError);
                Environment.Exit(-1);
            }
        }

        private static IWorkItemDestination CreateGitHubIssueReaderWriter(ProgramArguments programArguments)
        {
            const string ProductName = "CodePlexWorkItemMigrator";  // The name that identifies this utility to GitHub.

            //
            // It is important to limit the rate of requests we issue to GitHub APIs in order to avoid triggering their
            // abuse rate limits, especially that most of our requests are requests that "create content which triggers
            // notification, such as issues, comments and pull requests". Refer to the following URL for more info:
            // https://developer.github.com/v3/guides/best-practices-for-integrators/#dealing-with-abuse-rate-limits
            //
            var gitHubConnection = new Connection(
                    new ProductHeaderValue(ProductName),
                    new RateLimitingHttpClientAdapter(
                        new HttpClientAdapter(HttpMessageHandlerFactory.CreateDefault),
                        timeInterval: TimeSpan.FromMinutes(1),      // Allow maxRequestsPerTimeInterval every 1 minute.
                        maxRequestsPerTimeInterval: 5));            // Max HTTP requests we can make per timeInterval.

            gitHubConnection.Credentials = new Credentials(programArguments.GitHubPersonalAccessToken);

            var gitHubClient = new GitHubClient(gitHubConnection);
            return new GitHubRepoIssueReaderWriter(programArguments.GitHubRepoOwner, programArguments.GitHubRepo, gitHubClient.Issue, gitHubClient.Search);
        }

        private static MigrationSettings CreateMigrationSettings(ProgramArguments importerArgs)
        {
            MigrationSettings settings = MigrationSettings.DefaultSettings;
            settings.WorkItemsToSkip = importerArgs.WorkItemsToSkip;
            settings.MaxItemsToMigrate = importerArgs.MaxItemsToMigrate;
            return settings;
        }

        private static IWorkItemDestination CreateDestination(ProgramArguments importerArgs) =>
            importerArgs.OnlyWriteMigrationToConsole ? new ConsoleWorkItemWriter() : CreateGitHubIssueReaderWriter(importerArgs);
    }
}
