using System;
using System.Collections.Generic;
using System.Linq;

namespace Syncora.Services.MeetingEngine
{
    /// <summary>
    /// Одна строка рабочего расписания участника: день недели (1-7, ISO),
    /// границы рабочего времени суток и признак рабочего дня.
    /// </summary>
    public sealed record WorkingDay(short DayOfWeek, TimeSpan Start, TimeSpan End, bool IsWorkingDay);

    /// <summary>
    /// Занятый интервал участника (событие календаря или подтверждённая встреча), UTC.
    /// </summary>
    public sealed record BusyInterval(DateTime Start, DateTime End);

    /// <summary>
    /// Чистый детерминированный алгоритм поиска общего свободного времени (ТЗ §10).
    /// Не зависит от EF Core и покрывается unit-тестами как обычный C#.
    ///
    /// Шаги (§10.1-10.10):
    ///  1) на каждый день периода вычисляется общее рабочее окно всех участников
    ///     (правило §10.1: время вне рабочего графика недоступно, даже если событий нет);
    ///  2) из общего окна вычитается объединение занятых интервалов;
    ///  3) остаются интервалы, продолжительность которых не меньше требуемой (§10.8);
    ///  4) результаты сортируются по ближайшей дате и времени (§10.9)
    ///     и возвращаются не более MaxSlots вариантов (§10.10).
    /// </summary>
    public static class MeetingSlotFinder
    {
        public const int DefaultMaxSlots = 5;

        /// <summary>
        /// Найти свободные интервалы, подходящие всем участникам.
        /// Возвращает окна (не короче durationMinutes), отсортированные по началу.
        /// </summary>
        public static List<(DateTime Start, DateTime End)> FindCommonSlots(
            IReadOnlyDictionary<Guid, IReadOnlyList<WorkingDay>> workingDays,
            IReadOnlyList<BusyInterval> busy,
            DateTime from,
            DateTime to,
            int durationMinutes,
            int maxSlots = DefaultMaxSlots)
        {
            var result = new List<(DateTime Start, DateTime End)>();

            if (durationMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationMinutes));
            if (to <= from || workingDays.Count == 0)
                return result;

            var duration = TimeSpan.FromMinutes(durationMinutes);
            var date = from.Date;
            var lastDate = to.Date;

            while (date <= lastDate)
            {
                var window = CommonWorkingWindow(workingDays, date);
                if (window is not null)
                {
                    var (start, end) = window.Value;
                    if (start < from) start = from;
                    if (end > to) end = to;

                    if (start < end)
                    {
                        foreach (var free in SubtractBusy(start, end, busy))
                        {
                            if (free.End - free.Start >= duration)
                                result.Add(free);
                        }
                    }
                }

                date = date.AddDays(1);
            }

            return result
                .OrderBy(s => s.Start)
                .Take(maxSlots)
                .ToList();
        }

        /// <summary>
        /// Повторная проверка доступности интервала для всех участников (ТЗ §10.2).
        /// Используется перед окончательным созданием встречи.
        /// </summary>
        public static bool IsSlotAvailable(
            IReadOnlyDictionary<Guid, IReadOnlyList<WorkingDay>> workingDays,
            IReadOnlyList<BusyInterval> busy,
            DateTime slotStart,
            DateTime slotEnd)
        {
            if (slotEnd <= slotStart || workingDays.Count == 0)
                return false;

            // Интервал не должен пересекаться ни с одной занятостью
            if (busy.Any(b => b.Start < slotEnd && b.End > slotStart))
                return false;

            // И должен целиком попадать в рабочее время каждого участника (§10.1)
            foreach (var days in workingDays.Values)
            {
                if (!WithinWorkingHours(days, slotStart, slotEnd))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Общее рабочее окно всех участников на дату:
        /// начало — максимум стартов, конец — минимум окончаний.
        /// Если хоть у одного участника день нерабочий — день недоступен целиком.
        /// </summary>
        private static (DateTime start, DateTime end)? CommonWorkingWindow(
            IReadOnlyDictionary<Guid, IReadOnlyList<WorkingDay>> workingDays,
            DateTime date)
        {
            var start = TimeSpan.MinValue;
            var end = TimeSpan.MaxValue;

            foreach (var days in workingDays.Values)
            {
                var day = days.FirstOrDefault(d => d.DayOfWeek == ToIsoDayOfWeek(date));
                if (day is null || !day.IsWorkingDay)
                    return null;

                if (day.Start > start) start = day.Start;
                if (day.End < end) end = day.End;
            }

            if (start >= end)
                return null;

            return (date.Add(start), date.Add(end));
        }

        /// <summary>
        /// Слот целиком внутри рабочего времени участника (с учётом перехода через полночь).
        /// </summary>
        private static bool WithinWorkingHours(
            IReadOnlyList<WorkingDay> days,
            DateTime slotStart,
            DateTime slotEnd)
        {
            var date = slotStart.Date;
            var lastDate = slotEnd.Date;

            while (date <= lastDate)
            {
                var day = days.FirstOrDefault(d => d.DayOfWeek == ToIsoDayOfWeek(date));
                if (day is null || !day.IsWorkingDay)
                    return false;

                if (slotStart < date.Add(day.Start) || slotEnd > date.Add(day.End))
                    return false;

                date = date.AddDays(1);
            }

            return true;
        }

        /// <summary>
        /// Вычитание занятых интервалов из свободного окна.
        /// </summary>
        private static IEnumerable<(DateTime Start, DateTime End)> SubtractBusy(
            DateTime freeStart,
            DateTime freeEnd,
            IReadOnlyList<BusyInterval> busy)
        {
            var relevant = busy
                .Where(b => b.Start < freeEnd && b.End > freeStart)
                .OrderBy(b => b.Start)
                .ToList();

            var cursor = freeStart;
            foreach (var b in relevant)
            {
                if (b.Start > cursor)
                    yield return (cursor, b.Start);

                if (b.End > cursor)
                    cursor = b.End;

                if (cursor >= freeEnd)
                    yield break;
            }

            if (cursor < freeEnd)
                yield return (cursor, freeEnd);
        }

        private static short ToIsoDayOfWeek(DateTime date) =>
            date.DayOfWeek == DayOfWeek.Sunday ? (short)7 : (short)date.DayOfWeek;
    }
}
