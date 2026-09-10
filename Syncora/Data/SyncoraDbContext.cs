using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data
{
    public class SyncoraDbContext : DbContext
    {
        public SyncoraDbContext(DbContextOptions<SyncoraDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserWorkingHours> UserWorkingHours { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Calendar> Calendars { get; set; }
        public DbSet<CalendarMember> CalendarMembers { get; set; }
        public DbSet<UserCalendarSettings> UserCalendarSettings { get; set; }
        public DbSet<UserAccessSettings> UserAccessSettings { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Recurrence> Recurrences { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ShoppingListMember> ShoppingListMembers { get; set; }
        public DbSet<ShoppingItem> ShoppingItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SyncoraDbContext).Assembly);
        }
    }
}
