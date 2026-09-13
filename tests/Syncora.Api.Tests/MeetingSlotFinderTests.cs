using Syncora.Services.MeetingEngine;
using Xunit;

namespace Syncora.Api.Tests;

/// <summary>
/// Unit-тесты алгоритма поиска общего свободного времени (ТЗ §20.1, §20.3).
/// Чистая логика без EF Core — детерминированный алгоритм MeetingSlotFinder.
/// </summary>
public class MeetingSlotFinderTests
{
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly Guid UserC = Guid.NewGuid();
    private static readonly Guid UserD = Guid.NewGuid();
    private static readonly Guid UserE = Guid.NewGuid();
    private static readonly Guid UserF = Guid.NewGuid();
    private static readonly Guid UserG = Guid.NewGuid();
    private static readonly Guid UserH = Guid.NewGuid();
    private static readonly Guid UserI = Guid.NewGuid();
    private static readonly Guid UserJ = Guid.NewGuid();

    // Пн–Пт 09:00–18:00 (ТЗ §14.2)
    private static readonly WorkingDay[] DefaultWorkingDays =
    {
        new(1, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true),
        new(2, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true),
        new(3, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true),
        new(4, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true),
        new(5, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true),
        new(6, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), false),
        new(7, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), false),
    };

    private static IReadOnlyDictionary<Guid, IReadOnlyList<WorkingDay>> MakeWorkingDays(
        params Guid[] userIds)
    {
        var dict = new Dictionary<Guid, IReadOnlyList<WorkingDay>>();
        foreach (var id in userIds)
            dict[id] = DefaultWorkingDays;
        return dict;
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0)
        => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    // ─────────────────────────────────────────────────────────────
    // Критические примеры из ТЗ §20.3
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CriticalExample1_A_10_14_B_12_16_Duration2h_Returns_12_14()
    {
        // A свободен 10:00–14:00, B свободен 12:00–16:00, длительность 2 часа
        // Ожидается: 12:00–14:00

        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            // A занят 9:00–10:00 и 14:00–18:00
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 10)),
            new(Utc(2026, 9, 11, 14), Utc(2026, 9, 11, 18)),
            // B занят 9:00–12:00 и 16:00–18:00
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 12)),
            new(Utc(2026, 9, 11, 16), Utc(2026, 9, 11, 18)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 120);

        Assert.Single(slots);
        Assert.Equal(Utc(2026, 9, 11, 12), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 14), slots[0].End);
    }

    [Fact]
    public void CriticalExample2_A_10_12_B_11_12_Duration2h_ReturnsEmpty()
    {
        // A свободен 10:00–12:00, B свободен 11:00–12:00, длительность 2 часа
        // Пересечение всего 1 час (11:00–12:00) — меньше 2 часов, слот не возвращается

        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            // A занят 9:00–10:00 и 12:00–18:00
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 10)),
            new(Utc(2026, 9, 11, 12), Utc(2026, 9, 11, 18)),
            // B занят 9:00–11:00 и 12:00–18:00
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 11)),
            new(Utc(2026, 9, 11, 12), Utc(2026, 9, 11, 18)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 120);

        Assert.Empty(slots);
    }

    // ─────────────────────────────────────────────────────────────
    // §20.1: Поиск для 2 / 4 / 10 участников
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void TwoParticipants_BothFree_ReturnsCommonWindow()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            new(Utc(2026, 9, 11, 12), Utc(2026, 9, 11, 13)), // обед
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 60);

        Assert.NotEmpty(slots);
        // 09:00–12:00 (3ч) и 13:00–18:00 (5ч) — оба ≥ 1 часа
        Assert.Equal(2, slots.Count);
        Assert.Equal(Utc(2026, 9, 11, 9), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 12), slots[0].End);
        Assert.Equal(Utc(2026, 9, 11, 13), slots[1].Start);
        Assert.Equal(Utc(2026, 9, 11, 18), slots[1].End);
    }

    [Fact]
    public void FourParticipants_WithOverlappingBusy_ReturnsIntersection()
    {
        var workingDays = MakeWorkingDays(UserA, UserB, UserC, UserD);
        var busy = new List<BusyInterval>
        {
            // A занят 10:00–11:00
            new(Utc(2026, 9, 11, 10), Utc(2026, 9, 11, 11)),
            // B занят 14:00–15:00
            new(Utc(2026, 9, 11, 14), Utc(2026, 9, 11, 15)),
            // C занят 09:00–09:30
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 9, 30)),
            // D свободен весь день
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 30);

        // Свободные окна: 09:30–10:00, 11:00–14:00, 15:00–18:00
        Assert.Equal(3, slots.Count);
        Assert.Equal(Utc(2026, 9, 11, 9, 30), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 10), slots[0].End);
        Assert.Equal(Utc(2026, 9, 11, 11), slots[1].Start);
        Assert.Equal(Utc(2026, 9, 11, 14), slots[1].End);
        Assert.Equal(Utc(2026, 9, 11, 15), slots[2].Start);
        Assert.Equal(Utc(2026, 9, 11, 18), slots[2].End);
    }

    [Fact]
    public void TenParticipants_ReturnsValidSlots()
    {
        var allUsers = new[] { UserA, UserB, UserC, UserD, UserE, UserF, UserG, UserH, UserI, UserJ };
        var workingDays = MakeWorkingDays(allUsers);

        // Каждый занят в разное время — общее окно 09:00–10:00 и 16:00–18:00
        var busy = new List<BusyInterval>();
        for (int i = 0; i < allUsers.Length; i++)
        {
            var hour = 10 + (i % 6); // 10:00–16:00
            busy.Add(new BusyInterval(
                Utc(2026, 9, 11, hour),
                Utc(2026, 9, 11, hour + 1)));
        }

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 60);

        Assert.NotEmpty(slots);
        Assert.Contains(slots, s => s.Start == Utc(2026, 9, 11, 9) && s.End == Utc(2026, 9, 11, 10));
        Assert.Contains(slots, s => s.Start == Utc(2026, 9, 11, 16) && s.End == Utc(2026, 9, 11, 18));
    }

    // ─────────────────────────────────────────────────────────────
    // §20.1: Пересекающиеся события
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void OverlappingBusyIntervals_MergedIntoOne()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            // Пересекающиеся: 10:00–12:00 и 11:00–13:00 → объединение 10:00–13:00
            new(Utc(2026, 9, 11, 10), Utc(2026, 9, 11, 12)),
            new(Utc(2026, 9, 11, 11), Utc(2026, 9, 11, 13)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 60);

        Assert.Equal(2, slots.Count);
        Assert.Equal(Utc(2026, 9, 11, 9), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 10), slots[0].End);
        Assert.Equal(Utc(2026, 9, 11, 13), slots[1].Start);
        Assert.Equal(Utc(2026, 9, 11, 18), slots[1].End);
    }

    // ─────────────────────────────────────────────────────────────
    // §20.1: Полностью занятый день
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FullyBusyDay_ReturnsEmpty()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            // Занято всё рабочее время
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 60);

        Assert.Empty(slots);
    }

    // ─────────────────────────────────────────────────────────────
    // §20.1: Недостаточная продолжительность свободного окна
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FreeWindowShorterThanDuration_ReturnsEmpty()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            // Окно 10:00–10:30 (30 мин), а нужно 60 мин
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 10)),
            new(Utc(2026, 9, 11, 10, 30), Utc(2026, 9, 11, 18)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 60);

        Assert.Empty(slots);
    }

    // ─────────────────────────────────────────────────────────────
    // §20.1: Учёт рабочего времени (§10.1)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void OutsideWorkingHours_NotAvailable()
    {
        // Рабочее время 09:00–18:00, но ищем 07:00–22:00
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>();

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 7), Utc(2026, 9, 11, 22),
            durationMinutes: 60);

        // Слоты должны быть только внутри 09:00–18:00
        Assert.Single(slots);
        Assert.Equal(Utc(2026, 9, 11, 9), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 18), slots[0].End);
    }

    [Fact]
    public void WeekendDay_NotAvailable()
    {
        // Суббота (6) — нерабочий день
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>();

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 12, 9), Utc(2026, 9, 12, 18), // суббота
            durationMinutes: 60);

        Assert.Empty(slots);
    }

    // ─────────────────────────────────────────────────────────────
    // §20.1: Пограничные значения времени
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Boundary_ExactlyAtWorkingStart_ReturnsSlot()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            // Занято с 09:00, но свободно 10:00–18:00
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 10)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 480); // 8 часов

        // 10:00–18:00 = 8 часов — ровно хватает
        Assert.Single(slots);
        Assert.Equal(Utc(2026, 9, 11, 10), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 18), slots[0].End);
    }

    [Fact]
    public void Boundary_ExactlyAtWorkingEnd_ReturnsSlot()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            new(Utc(2026, 9, 11, 17), Utc(2026, 9, 11, 18)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 60);

        // 09:00–17:00 = 8 часов — свободно
        Assert.Single(slots);
        Assert.Equal(Utc(2026, 9, 11, 9), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 17), slots[0].End);
    }

    [Fact]
    public void Duration_ExactlyEqualsWindow_ReturnsSlot()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            new(Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 12)),
            new(Utc(2026, 9, 11, 14), Utc(2026, 9, 11, 18)),
        };

        var slots = MeetingSlotFinder.FindCommonSlots(
            workingDays, busy,
            Utc(2026, 9, 11, 9), Utc(2026, 9, 11, 18),
            durationMinutes: 120); // 2 часа

        // 12:00–14:00 = ровно 2 часа — подходит
        Assert.Single(slots);
        Assert.Equal(Utc(2026, 9, 11, 12), slots[0].Start);
        Assert.Equal(Utc(2026, 9, 11, 14), slots[0].End);
    }

    // ─────────────────────────────────────────────────────────────
    // IsSlotAvailable (повторная проверка, ТЗ §10.2)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsSlotAvailable_FreeSlot_ReturnsTrue()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            new(Utc(2026, 9, 11, 12), Utc(2026, 9, 11, 13)),
        };

        Assert.True(MeetingSlotFinder.IsSlotAvailable(
            workingDays, busy,
            Utc(2026, 9, 11, 10), Utc(2026, 9, 11, 12)));
    }

    [Fact]
    public void IsSlotAvailable_ConflictingSlot_ReturnsFalse()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>
        {
            new(Utc(2026, 9, 11, 10), Utc(2026, 9, 11, 12)),
        };

        Assert.False(MeetingSlotFinder.IsSlotAvailable(
            workingDays, busy,
            Utc(2026, 9, 11, 11), Utc(2026, 9, 11, 13)));
    }

    [Fact]
    public void IsSlotAvailable_OutsideWorkingHours_ReturnsFalse()
    {
        var workingDays = MakeWorkingDays(UserA, UserB);
        var busy = new List<BusyInterval>();

        Assert.False(MeetingSlotFinder.IsSlotAvailable(
            workingDays, busy,
            Utc(2026, 9, 11, 7), Utc(2026, 9, 11, 9)));
    }
}
