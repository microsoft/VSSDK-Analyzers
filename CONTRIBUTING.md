# Contributing

This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct
FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com)
with any additional questions or comments.

## Best practices

* Use Windows PowerShell or [PowerShell Core][pwsh] (including on Linux/OSX) to run .ps1 scripts.
  Some scripts set environment variables to help you, but they are only retained if you use PowerShell as your shell.

## Prerequisites

The only prerequisite for building, testing, and deploying from this repository
is the [.NET SDK](https://get.dot.net/).
You should install the version specified in `global.json` or a later version within
the same major.minor.Bxx "hundreds" band.
For example if 2.2.300 is specified, you may install 2.2.300, 2.2.301, or 2.2.310
while the 2.2.400 version would not be considered compatible by .NET SDK.
See [.NET Core Versioning](https://learn.microsoft.com/dotnet/core/versions/) for more information.

All dependencies can be installed by running the `init.ps1` script at the root of the repository
using Windows PowerShell or [PowerShell Core][pwsh] (on any OS).

This repository can be built on Windows, Linux, and OSX.

## Package restore

The easiest way to restore packages may be to run `init.ps1` which automatically authenticates
to the feeds that packages for this repo come from, if any.
`dotnet restore` or `nuget restore` also work but may require extra steps to authenticate to any applicable feeds.

## Building

Building, testing, and packing this repository can be done by using the standard dotnet CLI commands (e.g. `dotnet build`, `dotnet test`, `dotnet pack`, etc.).

[pwsh]: https://docs.microsoft.com/powershell/scripting/install/installing-powershell?view=powershell-6

## Bug reports

If you have a bug report, please file an issue.
If you can send a pull request with a repro of the bug in the form of a unit test, please do submit that PR
and link to it from the Issue you file.

## Pull Requests

We love to get pull requests. If you have a bug fix to offer or a new analyzer, please send us a pull request.

Every new feature or bug fix should be accompanied by unit tests to cover your change.

## Tutorial and API documentation

API and hand-written docs are found under the `docfx/` directory. and are built by [docfx](https://dotnet.github.io/docfx/).

You can make changes and host the site locally to preview them by switching to that directory and running the `dotnet docfx --serve` command.
After making a change, you can rebuild the docs site while the localhost server is running by running `dotnet docfx` again from a separate terminal.

The `.github/workflows/docs.yml` GitHub Actions workflow publishes the content of these docs to github.io if the workflow itself and [GitHub Pages is enabled for your repository](https://docs.github.com/en/pages/quickstart).

## Updating dependencies

This repo uses Renovate to keep dependencies current.
Configuration is in the `.github/renovate.json` file.
[Learn more about configuring Renovate](https://docs.renovatebot.com/configuration-options/).

When changing the renovate.json file, follow [these validation steps](https://docs.renovatebot.com/config-validation/).

If Renovate is not creating pull requests when you expect it to, check that the [Renovate GitHub App](https://github.com/apps/renovate) is configured for your account or repo.

## Merging latest from Library.Template

### Maintaining your repo based on this template

The best way to keep your repo in sync with Library.Template's evolving features and best practices is to periodically merge the template into your repo:
`
```ps1
git fetch
git checkout origin/main
.\tools\MergeFrom-Template.ps1
# resolve any conflicts, then commit the merge commit.
git push origin -u HEAD
```
