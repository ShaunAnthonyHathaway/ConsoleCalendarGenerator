using System.Collections;
using System.Reflection;
using ConsoleCalendarGenerator.Tests.Support;

namespace ConsoleCalendarGenerator.Tests.Layout;

/// <summary>
/// A layout cannot be changed once it exists: not through its properties, and not by casting the collections it
/// hands out to something writable.
/// </summary>
[TestFixture]
public class LayoutImmutabilityTests
{
    private static readonly Type[] LayoutTypes = { typeof(MonthLayout), typeof(MonthLayoutWeek), typeof(MonthLayoutCell) };

    [Test]
    public void TheCollections_AreNotArrays()
    {
        MonthLayout layout = MonthLayout.Create(2023, 5, DayOfWeek.Monday);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(layout.Weeks, Is.Not.InstanceOf<Array>());
            Assert.That(layout.DaysOfWeek, Is.Not.InstanceOf<Array>());
            Assert.That(layout.Weeks[0].Cells, Is.Not.InstanceOf<Array>());
        }
    }

    [Test]
    public void TheWeeks_CannotBeChanged_ThroughAnyListInterface()
    {
        MonthLayout layout = MonthLayout.Create(2023, 5, DayOfWeek.Monday);
        string before = LayoutPicture.Describe(layout);
        MonthLayoutWeek another = MonthLayout.Create(2024, 2, DayOfWeek.Monday).Weeks[0];

        AssertRejectsEveryChange(layout.Weeks, another);

        Assert.That(LayoutPicture.Describe(layout), Is.EqualTo(before));
    }

    [Test]
    public void TheCellsOfAWeek_CannotBeChanged_ThroughAnyListInterface()
    {
        MonthLayout layout = MonthLayout.Create(2023, 5, DayOfWeek.Monday);
        string before = LayoutPicture.Describe(layout);
        MonthLayoutCell another = MonthLayout.Create(2024, 2, DayOfWeek.Monday).Weeks[1].Cells[0];

        foreach (MonthLayoutWeek week in layout.Weeks)
        {
            AssertRejectsEveryChange(week.Cells, another);
        }

        Assert.That(LayoutPicture.Describe(layout), Is.EqualTo(before));
    }

    [Test]
    public void TheDaysOfWeek_CannotBeChanged_ThroughAnyListInterface()
    {
        MonthLayout layout = MonthLayout.Create(2023, 5, DayOfWeek.Monday);
        string before = LayoutPicture.Describe(layout);

        AssertRejectsEveryChange(layout.DaysOfWeek, DayOfWeek.Friday);

        Assert.That(LayoutPicture.Describe(layout), Is.EqualTo(before));
    }

    /// <summary>
    /// What a caller does with one layout has no effect on another, including one created for the same
    /// arguments.
    /// </summary>
    [Test]
    public void ALayout_IsNotAffectedByOtherLayouts()
    {
        MonthLayout layout = MonthLayout.Create(2023, 5, DayOfWeek.Monday);
        string before = LayoutPicture.Describe(layout);

        foreach (DayOfWeek firstDayOfWeek in ReferenceCalendar.AllDaysOfWeek)
        {
            for (int month = 1; month <= 12; month++)
            {
                _ = LayoutPicture.Describe(MonthLayout.Create(2023, month, firstDayOfWeek));
            }
        }

        Assert.That(LayoutPicture.Describe(layout), Is.EqualTo(before));
    }

    /// <summary>
    /// The public surface offers nothing to change a layout with: no property setters, no fields, no public
    /// constructors, and no way to derive from the types.
    /// </summary>
    [Test]
    public void TheLayoutTypes_AreSealed_WithReadOnlyPropertiesAndNoPublicConstructorsOrFields()
    {
        foreach (Type type in LayoutTypes)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(type.IsSealed, Is.True, type.Name + " is sealed");
                Assert.That(type.GetConstructors(), Is.Empty, type.Name + " has no public constructor");
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static), Is.Empty, type.Name + " has no public field");
                Assert.That(type.GetProperties().Where(property => property.CanWrite).Select(property => property.Name), Is.Empty,
                    type.Name + " has no settable property");
                Assert.That(type.GetProperties(), Is.Not.Empty);
            }
        }
    }

    /// <summary>
    /// Tries every way of changing the collection that the generic and the non-generic list interfaces offer.
    /// Whichever of them the collection implements must say it is read-only and refuse each change.
    /// </summary>
    private static void AssertRejectsEveryChange<T>(IReadOnlyList<T> collection, T item)
    {
        T[] before = collection.ToArray();

        if (collection is ICollection<T> generic)
        {
            Assert.That(generic.IsReadOnly, Is.True);
            Assert.That(() => generic.Add(item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => generic.Remove(before[0]), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => generic.Clear(), Throws.TypeOf<NotSupportedException>());
        }
        if (collection is IList<T> genericList)
        {
            Assert.That(() => genericList[0] = item, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => genericList.Insert(0, item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => genericList.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
        }
        if (collection is IList list)
        {
            Assert.That(list.IsReadOnly, Is.True);
            Assert.That(list.IsFixedSize, Is.True);
            Assert.That(() => list[0] = item, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => list.Add(item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => list.Insert(0, item), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => list.Remove(before[0]), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => list.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => list.Clear(), Throws.TypeOf<NotSupportedException>());
        }

        Assert.That(collection, Is.EqualTo(before), "the collection still holds what it held");
    }
}
