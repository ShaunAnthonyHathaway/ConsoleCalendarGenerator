using ConsoleCalendarGenerator.Tests.Support;

// The library writes to the process-wide console and reads the process-wide culture, so no two tests may run
// at the same time.
[assembly: NonParallelizable]
[assembly: GlobalStateGuard]
