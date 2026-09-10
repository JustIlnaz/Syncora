using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Timezone { get; set; }

        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<UserWorkingHours> WorkingHours { get; set; } = new List<UserWorkingHours>();
        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public ICollection<Calendar> OwnedCalendars { get; set; } = new List<Calendar>();
        public ICollection<CalendarMember> CalendarMemberships { get; set; } = new List<CalendarMember>();
        public ICollection<UserCalendarSettings> CalendarSettings { get; set; } = new List<UserCalendarSettings>();
        public ICollection<UserAccessSettings> AccessSettings { get; set; } = new List<UserAccessSettings>();
        public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
        public ICollection<Meeting> CreatedMeetings { get; set; } = new List<Meeting>();
        public ICollection<MeetingParticipant> MeetingParticipations { get; set; } = new List<MeetingParticipant>();
        public ICollection<ShoppingList> OwnedShoppingLists { get; set; } = new List<ShoppingList>();
        public ICollection<ShoppingListMember> ShoppingListMemberships { get; set; } = new List<ShoppingListMember>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
