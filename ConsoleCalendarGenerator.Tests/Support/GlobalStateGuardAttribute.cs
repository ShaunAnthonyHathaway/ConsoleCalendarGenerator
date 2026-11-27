using System.Globalization;
using NUnit.Framework.Interfaces;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// Fails any test that leaves process-wide state changed: the console writers, the current cultures or, where
/// they can be read back, the console colors. Applied to the whole assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class GlobalStateGuardAttribute : Attribute, ITestAction
{
    private TextWriter? _out;
    private TextWriter? _error;
    private CultureInfo? _culture;
    private CultureInfo? _uiCulture;

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        _out = Console.Out;
        _error = Console.Error;
        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;
    }

    public void AfterTest(ITest test)
    {
        try
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Console.Out, Is.SameAs(_out), "The test left Console.Out replaced.");
                Assert.That(Console.Error, Is.SameAs(_error), "The test left Console.Error replaced.");
                Assert.That(CultureInfo.CurrentCulture, Is.SameAs(_culture), "The test left CurrentCulture changed.");
                Assert.That(CultureInfo.CurrentUICulture, Is.SameAs(_uiCulture), "The test left CurrentUICulture changed.");
                if (!OperatingSystem.IsWindows())
                {
                    Assert.That(Console.ForegroundColor, Is.EqualTo(ConsoleState.Unset), "The test left a foreground color set.");
                    Assert.That(Console.BackgroundColor, Is.EqualTo(ConsoleState.Unset), "The test left a background color set.");
                }
            }
        }
        finally
        {
            // Put everything back so that one leaking test does not fail the tests after it.
            Console.SetOut(_out!);
            Console.SetError(_error!);
            CultureInfo.CurrentCulture = _culture!;
            CultureInfo.CurrentUICulture = _uiCulture!;
            Console.ResetColor();
        }
    }
}
