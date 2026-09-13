using System;

namespace Syncora.Services
{
    /// <summary>
    /// Выбранный интервал встречи стал занят после поиска (ТЗ §10.2).
    /// API возвращает 409 Conflict.
    /// </summary>
    public class MeetingSlotConflictException : InvalidOperationException
    {
        public MeetingSlotConflictException(string message) : base(message)
        {
        }
    }
}
