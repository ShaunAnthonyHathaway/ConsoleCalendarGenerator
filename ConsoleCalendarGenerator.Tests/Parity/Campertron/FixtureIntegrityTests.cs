using System.Globalization;
using System.Text.RegularExpressions;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Parity.Campertron;

/// <summary>
/// The fixtures themselves: that they say where they come from, are well formed and consistent, and cover what
/// they are meant to cover. None of these tests uses the package.
/// </summary>
[TestFixture]
public class FixtureIntegrityTests
{
    private static Manifest Manifest => ParityFixtures.Manifest;

    private static IReadOnlyList<FixtureCase> Cases => ParityFixtures.Cases;

    // ---- provenance ----

    [Test]
    public void TheManifest_SaysExactlyWhereTheFixturesComeFrom()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Manifest.SchemaVersion, Is.EqualTo(1));
            Assert.That(Manifest.Source.Repository, Is.EqualTo("https://github.com/ShaunAnthonyHathaway/Campertron"));
            Assert.That(Manifest.Source.Commit, Does.Match("^[0-9a-f]{40}$"), "a full commit, not a branch or a short hash");
            Assert.That(Manifest.Source.Commit, Is.EqualTo("3ded294c37642243359bcee2563d7227295a7f40"));
            Assert.That(Manifest.Source.File, Is.EqualTo("CampertronLibrary/function/Base/Calendar.cs"));
            Assert.That(Manifest.Source.Symbol, Does.StartWith("CampertronLibrary.function.Base.Calendar.Lines(").Using(StringComparison.Ordinal));
            Assert.That(Manifest.Source.Returns, Is.Not.Empty);
        }
    }

    [Test]
    public void TheManifest_RecordsWhatTheSourceFixes_AndWhatItsCallersPass()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Manifest.FixedByTheSource.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(Manifest.FixedByTheSource.WeekdayRow, Is.EqualTo(" Mo  Tu  We  Th  Fr  Sa  Su"));
            Assert.That(Manifest.FixedByTheSource.CellWidth, Is.EqualTo(3));
            Assert.That(Manifest.FixedByTheSource.ColumnGap, Is.EqualTo(1));
            Assert.That(Manifest.FixedByTheSource.Marker, Is.EqualTo("*"));
            Assert.That(Manifest.FixedByTheSource.TitleFormat, Is.EqualTo("Y"));
            Assert.That(Manifest.FixedByTheSource.TitleSuffix, Is.EqualTo(" (* = open)"));
            Assert.That(Manifest.FixedByTheSource.TitleAlignment, Is.EqualTo("Left"));
            Assert.That(Manifest.LiveCallSites.File, Is.EqualTo("CampertronLibrary/function/Base/ConsoleResults.cs"));
            Assert.That(Manifest.LiveCallSites.Indent, Is.EqualTo("  "));
            Assert.That(Manifest.LiveCallSites.MarkColor, Is.EqualTo(ConsoleColor.Cyan));
            Assert.That(Manifest.LiveCallSites.MaximumWidth, Is.EqualTo(80));
        }
    }

    [Test]
    public void TheManifest_ListsExactlyTheFixtureFilesThatExist()
    {
        Assert.That(ParityFixtures.EmbeddedFiles().Where(file => file != "manifest.json"),
            Is.EquivalentTo(Manifest.Files), "every file is listed and every listed file exists");
        Assert.That(Manifest.Files, Is.Unique);
    }

    // ---- form ----

    [Test]
    public void EveryFile_HasTheSchemaVersion_ADescription_AndCases()
    {
        foreach (string file in Manifest.Files)
        {
            FixtureFile fixtures = ParityFixtures.ReadFile(file);

            Assert.That(fixtures.SchemaVersion, Is.EqualTo(1), file);
            Assert.That(fixtures.About, Is.Not.Empty, file);
            Assert.That(fixtures.Cases, Is.Not.Empty, file);
        }
    }

    /// <summary>The files are read strictly, so a misspelled or leftover field cannot hide in them.</summary>
    [Test]
    public void AFieldTheSchemaDoesNotKnow_IsAnError()
    {
        const string WithAnExtraField = "{ \"schemaVersion\": 1, \"about\": \"x\", \"cases\": [], \"recordedOn\": \"today\" }";
        var strict = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };

        Assert.That(() => System.Text.Json.JsonSerializer.Deserialize<FixtureFile>(WithAnExtraField, strict), Throws.InstanceOf<System.Text.Json.JsonException>());
    }

    [Test]
    public void TheFiles_AreAsciiOnly_WithNoPathsAndNoTimesOfRecording()
    {
        foreach (string file in ParityFixtures.EmbeddedFiles())
        {
            string text = ParityFixtures.RawText(file);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(text.All(character => character < 128), Is.True, file + " has a character outside ASCII; write it as \\uXXXX");
                Assert.That(Regex.IsMatch(text, @"/home/|/Users/|/tmp/|[A-Za-z]:\\\\|\\\\\\\\"), Is.False, file + " contains a path of some machine");
                Assert.That(Regex.IsMatch(text, @"\d{2}:\d{2}:\d{2}(Z|[+-]\d{2}:\d{2})"), Is.False, file + " contains a time stamp with a zone");
                Assert.That(Regex.IsMatch(text, @"(?i)recorded|generated|timestamp|password|token|secret|@[a-z0-9-]+\.[a-z]{2,}"), Is.False,
                    file + " contains something that is not fixture data");
                Assert.That(text, Ordinal.DoesNotContain(TerminalOutput.EscapeCharacter), file + " contains an escape character");
                Assert.That(text, Ordinal.DoesNotContain("\\" + "u001b"), file + " contains a written-out escape character");
            }
        }
    }

    [Test]
    public void EveryCase_HasAnIdThatIsUniqueInItsFile_AndSaysWhatItCovers()
    {
        foreach (IGrouping<string, FixtureCase> file in Cases.GroupBy(fixture => fixture.File))
        {
            Assert.That(file.Select(fixture => fixture.Id), Is.Unique, file.Key);
        }
        foreach (FixtureCase fixture in Cases)
        {
            Assert.That(fixture.Id, Does.Match("^[a-z0-9]+(-[a-z0-9]+)*$"), fixture.ToString());
            Assert.That(fixture.Covers, Is.Not.Empty, fixture.ToString());
            Assert.That(fixture.Title, Is.AnyOf(FixtureCase.ExactTitle, FixtureCase.IntentionallyDifferentTitle), fixture.ToString());
        }
    }

    [Test]
    public void EveryInput_IsAValidMonth_AKnownCulture_AndWellFormedDates()
    {
        foreach (FixtureCase fixture in Cases)
        {
            FixtureInput input = fixture.Input;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(input.Year, Is.InRange(1, 9999), fixture.ToString());
                Assert.That(input.Month, Is.InRange(1, 12), fixture.ToString());
                Assert.That(input.Culture, Does.Match("^([a-z]{2,3}(-[A-Za-z0-9]{2,8})*)?$"), fixture + ": a culture name, or empty for the invariant culture");
                Assert.That(Enum.IsDefined(input.MarkColor), Is.True, fixture.ToString());
                Assert.That(input.Indent, Does.Not.Contain("\n").And.Not.Contain("\r"), fixture.ToString());
                foreach (FixtureDate date in input.Dates)
                {
                    Assert.That(DateTime.TryParseExact(date.Date, FixtureDate.Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _), Is.True,
                        fixture + ": " + date.Date);
                    Assert.That(Enum.IsDefined(date.Kind), Is.True, fixture.ToString());
                }
                Assert.That(input.DateTimes().Select(date => date.Kind), Is.EqualTo(input.Dates.Select(date => date.Kind)), fixture + ": the kind survives");
            }
        }
    }

    // ---- consistency of what was recorded ----

    [Test]
    public void TheSpansOfEveryLine_JoinToItsText()
    {
        foreach (FixtureCase fixture in Cases)
        {
            foreach (FixtureLine line in fixture.Lines)
            {
                Assert.That(line.Spans, Is.Not.Empty, fixture.ToString());
                Assert.That(line.Spans.Select(span => span.Count), Is.All.EqualTo(2), fixture + ": a span is a [text, color] pair");
                Assert.That(string.Concat(line.ColoredSpans().Select(span => span.Text)), Is.EqualTo(line.Text), fixture.ToString());
            }
        }
    }

    [Test]
    public void EveryCase_IsATitle_TheWeekdayRow_AndFourToSixWeeks_AllStartingWithTheIndent()
    {
        foreach (FixtureCase fixture in Cases)
        {
            string indent = fixture.Input.Indent;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.Lines.Count, Is.InRange(6, 8), fixture.ToString());
                Assert.That(fixture.Lines[0].Text, Does.StartWith(indent).And.EndWith(Manifest.FixedByTheSource.TitleSuffix).Using(StringComparison.Ordinal), fixture.ToString());
                Assert.That(fixture.Lines[1].Text, Is.EqualTo(indent + Manifest.FixedByTheSource.WeekdayRow), fixture.ToString());
                Assert.That(fixture.Lines.Select(line => line.Text), Is.All.StartsWith(indent).Using(StringComparison.Ordinal), fixture.ToString());
                Assert.That(fixture.Lines.Select(line => line.Text), Has.None.EndsWith(" ").Using(StringComparison.Ordinal), fixture + ": no trailing spaces");
            }
        }
    }

    /// <summary>
    /// What the recorded renderer colors: exactly the cells of the dates that are in the month, in the mark
    /// color, each with the marker before its number. Checked from the fixture alone.
    /// </summary>
    [Test]
    public void TheColoredSpansOfEveryCase_AreExactlyItsMarkedDays()
    {
        foreach (FixtureCase fixture in Cases)
        {
            int[] expectedDays = fixture.Input.DateTimes()
                .Where(date => date.Year == fixture.Input.Year && date.Month == fixture.Input.Month)
                .Select(date => date.Day).Distinct().OrderBy(day => day).ToArray();

            (string Text, ConsoleColor? Color)[] colored = fixture.Lines.SelectMany(line => line.ColoredSpans()).Where(span => span.Color.HasValue).ToArray();

            Assert.That(colored.Select(span => span.Color), Is.All.EqualTo(fixture.Input.MarkColor), fixture.ToString());
            Assert.That(colored.Select(span => span.Text.Length), Is.All.EqualTo(Manifest.FixedByTheSource.CellWidth), fixture + ": the whole cell is colored");
            Assert.That(colored.Select(span => span.Text.Trim()), Is.EqualTo(expectedDays.Select(day => Manifest.FixedByTheSource.Marker + day.ToString(CultureInfo.InvariantCulture))),
                fixture.ToString());
        }
    }

    // ---- coverage ----

    [Test]
    public void TheCases_CoverEveryWeekdayForThe1st_EveryMonthLength_AndFourFiveAndSixWeeks()
    {
        var firstWeekdays = Cases.Select(fixture => new DateTime(fixture.Input.Year, fixture.Input.Month, 1).DayOfWeek).Distinct();
        var lengths = Cases.Select(fixture => DateTime.DaysInMonth(fixture.Input.Year, fixture.Input.Month)).Distinct();
        var weeks = Cases.Select(fixture => fixture.Lines.Count - 2).Distinct();

        Assert.That(firstWeekdays, Is.EquivalentTo(Enum.GetValues<DayOfWeek>()));
        Assert.That(lengths, Is.EquivalentTo(new[] { 28, 29, 30, 31 }));
        Assert.That(weeks, Is.EquivalentTo(new[] { 4, 5, 6 }));
    }

    [Test]
    public void TheCases_CoverTheIndentsCulturesColorsAndDatePatternsTheyAreMeantTo()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Cases.Select(fixture => fixture.Input.Indent).Distinct(), Is.SupersetOf(new[] { string.Empty, " ", "  ", "> ", "\t" }));
            Assert.That(Cases.Select(fixture => fixture.Input.Culture).Distinct(), Is.SupersetOf(new[] { string.Empty, "en-US", "de-DE", "fr-FR", "th-TH", "fa-IR" }));
            Assert.That(Cases.Select(fixture => fixture.Input.MarkColor).Distinct().Count(), Is.GreaterThanOrEqualTo(3));
            Assert.That(Cases.Any(fixture => fixture.Input.Dates.Count == 0), Is.True, "no dates");
            Assert.That(Cases.Any(fixture => fixture.Input.Dates.Count == DateTime.DaysInMonth(fixture.Input.Year, fixture.Input.Month)
                && fixture.Input.DateTimes().Select(date => date.Day).Distinct().Count() == fixture.Input.Dates.Count), Is.True, "every day");
            Assert.That(Cases.Any(fixture => fixture.Input.DateTimes().GroupBy(date => date).Any(group => group.Count() > 1)), Is.True, "a date more than once");
            Assert.That(Cases.Any(fixture => fixture.Input.DateTimes().Any(date => date.TimeOfDay != TimeSpan.Zero)), Is.True, "a time of day");
            Assert.That(Cases.SelectMany(fixture => fixture.Input.Dates).Select(date => date.Kind).Distinct(), Is.EquivalentTo(Enum.GetValues<DateTimeKind>()));
            Assert.That(Cases.Any(fixture => fixture.Input.DateTimes().Any(date => date.Year == fixture.Input.Year && date.Month != fixture.Input.Month)), Is.True, "another month");
            Assert.That(Cases.Any(fixture => fixture.Input.DateTimes().Any(date => date.Year != fixture.Input.Year && date.Month == fixture.Input.Month)), Is.True, "another year");
            Assert.That(Cases.Any(fixture => !fixture.Input.DateTimes().SequenceEqual(fixture.Input.DateTimes().OrderBy(date => date))), Is.True, "unordered dates");
            Assert.That(Cases.Count(fixture => fixture.Title == FixtureCase.IntentionallyDifferentTitle), Is.EqualTo(2), "the two cultures whose calendar is not Gregorian");
        }
    }

    /// <summary>The calls of the live call sites: two spaces of indent and the color they use.</summary>
    [Test]
    public void TheCases_IncludeTheIndentAndTheColorOfTheLiveCallSites()
    {
        Assert.That(Cases.Count(fixture => fixture.Input.Indent == Manifest.LiveCallSites.Indent && fixture.Input.MarkColor == Manifest.LiveCallSites.MarkColor),
            Is.GreaterThan(20));
    }
}
