using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Documentation;

/// <summary>Temporary: shows what the Windows CI machine's culture data gives. Removed in the next commit.</summary>
[TestFixture]
public class TemporaryCultureDataDiagnostic
{
    [Test]
    public void ShowCultureData()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        var lines = new List<string>();
        foreach (string name in new[] { "en-US", "fr-FR" })
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(name);
            DateTimeFormatInfo format = culture.DateTimeFormat;
            RenderedMonth month = MonthRenderer.Render(2027, 3, MonthRenderOptions.Default.WithCulture(culture));
            lines.Add(name + " title=[" + month.Lines[0].Text + "] labels=[" + month.Lines[1].Text + "]");
            lines.Add(name + " shortest=[" + string.Join("|", format.ShortestDayNames) + "] abbreviated=[" + string.Join("|", format.AbbreviatedDayNames) + "]");
            lines.Add(name + " pattern=[" + format.YearMonthPattern + "] month=[" + format.GetMonthName(3) + "] calendar=" + format.Calendar.GetType().Name);
        }
        Assert.Fail(string.Join(" ;; ", lines));
    }
}
