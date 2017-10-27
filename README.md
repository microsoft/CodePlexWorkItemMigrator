# CodePlex to GitHub Work Item Migrator
A command line tool to migrate CodePlex work items to GitHub issues.

## How to use
```
Microsoft.CodePlex.Migration.WorkItemMigrator.exe -p CodePlexProject -o GitHubRepoOwner -r GitHubRepo -t GitHubPersonalAccessToken
```

- `CodePlexProject` is the subdomain of your CodePlex project (`subdomain`.codeplex.com)
- `GitHubRepoOwner` is the user or organization that owns your migrated repo; `GitHubRepo` is your migrated repo. For instance, the [NuGet/Home](https://github.com/nuget/home) repository would use `nuget` as the owner and `home` as the repo.
- `GitHubPersonalAccessToken` is a token you create [here](https://github.com/settings/tokens); make sure to select the `repo` scope.

## Installing
Download and extract the [zip](https://github.com/Microsoft/CodePlexWorkItemMigrator/files/1420246/Microsoft.CodePlex.Migration.WorkItemMigrator.1.0.0.zip), then run the executable using a command window.

## Notes
+ Migration progress will be slow (avg. 1 item/min) in order to avoid triggering GitHub APIs abuse mechanisms. _Please, refer to the following URL for more information on GitHub abuse rate limits: https://developer.github.com/v3/guides/best-practices-for-integrators/#dealing-with-abuse-rate-limits_

## License
Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT](https://opensource.org/licenses/MIT) license

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Acknowledgements
This tool was created in collaboration with GitHub as part of the [CodePlex archive process](https://aka.ms/codeplex-announcement).
