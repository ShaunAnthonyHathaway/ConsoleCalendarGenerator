# Security

## Supported versions

| Version | Availability | Supported |
|---|---|---|
| 2.x | GitHub Releases | Yes |
| 1.0.0 | NuGet | Yes, until a 2.x version is published on NuGet |

A fix is released as a new version. Published versions are never changed.

## What the library does

ConsoleCalendarGenerator formats calendars as text and writes to the console or to a `TextWriter` that the
caller supplies. It does not use the network, the file system or other processes, and it has no package
dependencies. A security problem is most likely to be a way to make the library fail, hang or use excessive
memory with input an application could receive from outside.

Text that an application passes in, such as weekday labels, an indent, a title suffix or a marker, is written
as it is. The library refuses a line break in it and does not remove or escape anything else, control
characters included. An application that takes such text from outside should check it before it reaches a
terminal.

## Reporting a problem

Please report a security problem privately, through GitHub's private vulnerability reporting for this
repository:

- [Report a vulnerability](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/security/advisories/new), or
- on the repository's page on GitHub, open the **Security** tab and choose **Report a vulnerability**.

You need to be signed in to GitHub. GitHub shows the report to you and to the maintainer of this repository,
not to the public.

Please do not put the details of a vulnerability in a public issue, a discussion or a pull request.

Reports are handled on a best-effort basis by a single maintainer. There is no guaranteed response time.
