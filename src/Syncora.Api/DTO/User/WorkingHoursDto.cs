namespace Syncora.DTO.User
{
    public class WorkingHoursDto
    {
        public Guid Id { get; set; }
        public short DayOfWeek { get; set; }       
        public string DayName { get; set; } = "";  
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsWorkingDay { get; set; }
    }
}
