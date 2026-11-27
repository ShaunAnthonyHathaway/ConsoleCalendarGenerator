# Security

## Supported versions

Security fixes are made for the latest version published on NuGet.

Version 2.0.0 is the current version of this repository. Until it is published on NuGet, the latest published
version is 1.0.0. A problem found in the code of this repository is worth reporting either way.

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

This repository does not have a private channel for vulnerability reports at present.

Please [open an issue](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/issues) that says you
have found a security problem, without the details that would let someone exploit it. The maintainer will
reply there to arrange how to share the details. Issues are public, so do not include anything in one that
should not be.

Reports are handled on a best-effort basis by a single maintainer. There is no guaranteed response time.
