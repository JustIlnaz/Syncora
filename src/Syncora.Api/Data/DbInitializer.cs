using Microsoft.EntityFrameworkCore;
using Syncora.Helpers;
using Syncora.Models;

namespace Syncora.Data
{
    public class DbInitializer
    {
        public static async Task InitializeAsync(SyncoraDbContext context)
        {
            if (await context.Users.AnyAsync())
                return;

            var anna = new User
            {
                Id = Guid.NewGuid(),
                Name = "Анна Петрова",
                Email = "anna@syncora.com",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Timezone = "Europe/Moscow",
                AvatarUrl = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var ivan = new User
            {
                Id = Guid.NewGuid(),
                Name = "Иван Соколов",
                Email = "ivan@syncora.com",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Timezone = "Europe/Moscow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(anna, ivan);
            await context.SaveChangesAsync();

            var workingHours = new List<UserWorkingHours>();
            foreach (var user in new[] { anna, ivan })
            {
                for (short day = 1; day <= 5; day++) 
                {
                    workingHours.Add(new UserWorkingHours
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        DayOfWeek = day,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(18, 0, 0),
                        IsWorkingDay = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                for (short day = 6; day <= 7; day++)
                {
                    workingHours.Add(new UserWorkingHours
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        DayOfWeek = day,
                        StartTime = new TimeSpan(0, 0, 0),
                        EndTime = new TimeSpan(0, 0, 0),
                        IsWorkingDay = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            context.UserWorkingHours.AddRange(workingHours);
            await context.SaveChangesAsync();

            var personalCalendar = new Calendar
            {
                Id = Guid.NewGuid(),
                OwnerId = anna.Id,
                Name = "Личный",
                Color = "#A78BFA",
                Type = "personal",
                Description = "Личные дела и заметки",
                Timezone = "Europe/Moscow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var workCalendar = new Calendar
            {
                Id = Guid.NewGuid(),
                OwnerId = anna.Id,
                Name = "Рабочий",
                Color = "#60A5FA",
                Type = "work",
                Description = "Рабочие встречи и задачи",
                Timezone = "Europe/Moscow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var groupCalendar = new Calendar
            {
                Id = Guid.NewGuid(),
                OwnerId = anna.Id,
                Name = "Групповой",
                Color = "#34D399",
                Type = "group",
                Description = "Общий календарь для встреч с друзьями",
                Timezone = "Europe/Moscow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Calendars.AddRange(personalCalendar, workCalendar, groupCalendar);
            await context.SaveChangesAsync();

            context.CalendarMembers.AddRange(
                new CalendarMember
                {
                    Id = Guid.NewGuid(),
                    CalendarId = personalCalendar.Id,
                    UserId = anna.Id,
                    Role = "owner",
                    AccessLevel = "full",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CalendarMember
                {
                    Id = Guid.NewGuid(),
                    CalendarId = workCalendar.Id,
                    UserId = anna.Id,
                    Role = "owner",
                    AccessLevel = "full",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CalendarMember
                {
                    Id = Guid.NewGuid(),
                    CalendarId = workCalendar.Id,
                    UserId = ivan.Id,
                    Role = "member",
                    AccessLevel = "edit",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CalendarMember
                {
                    Id = Guid.NewGuid(),
                    CalendarId = groupCalendar.Id,
                    UserId = anna.Id,
                    Role = "owner",
                    AccessLevel = "full",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CalendarMember
                {
                    Id = Guid.NewGuid(),
                    CalendarId = groupCalendar.Id,
                    UserId = ivan.Id,
                    Role = "member",
                    AccessLevel = "edit",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;

            context.Events.AddRange(
                new Event
                {
                    Id = Guid.NewGuid(),
                    CalendarId = personalCalendar.Id,
                    CreatorId = anna.Id,
                    Title = "Забрать посылку",
                    Description = "Пункт выдачи на Ленина 10",
                    StartAt = today.AddDays(1).AddHours(18),
                    EndAt = today.AddDays(1).AddHours(19),
                    Location = "Ленина 10",
                    IsAllDay = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    CalendarId = workCalendar.Id,
                    CreatorId = anna.Id,
                    Title = "Созвон с командой",
                    Description = "Обсуждение спринта",
                    StartAt = today.AddDays(1).AddHours(11),
                    EndAt = today.AddDays(1).AddHours(12),
                    Location = "Zoom",
                    IsAllDay = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    CalendarId = groupCalendar.Id,
                    CreatorId = ivan.Id,
                    Title = "Спорт",
                    Description = "Тренировка",
                    StartAt = today.AddDays(2).AddHours(19),
                    EndAt = today.AddDays(2).AddHours(20).AddMinutes(30),
                    Location = "Фитнес-клуб",
                    IsAllDay = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    CalendarId = groupCalendar.Id,
                    CreatorId = anna.Id,
                    Title = "Встреча с друзьями",
                    Description = "Ужин в кафе",
                    StartAt = today.AddDays(3).AddHours(19),
                    EndAt = today.AddDays(3).AddHours(21),
                    Location = "Кафе 'Пушкин'",
                    IsAllDay = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    CalendarId = personalCalendar.Id,
                    CreatorId = anna.Id,
                    Title = "Всё для дома",
                    Description = "Закупка бытовых товаров",
                    StartAt = today.AddDays(4).AddHours(10),
                    EndAt = today.AddDays(4).AddHours(12),
                    Location = "ТЦ Метрополис",
                    IsAllDay = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var shoppingList = new ShoppingList
            {
                Id = Guid.NewGuid(),
                OwnerId = anna.Id,
                Name = "Продукты",
                IsShared = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.ShoppingLists.Add(shoppingList);
            await context.SaveChangesAsync();

            context.ShoppingListMembers.AddRange(
                new ShoppingListMember
                {
                    Id = Guid.NewGuid(),
                    ShoppingListId = shoppingList.Id,
                    UserId = anna.Id,
                    Role = "owner",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ShoppingListMember
                {
                    Id = Guid.NewGuid(),
                    ShoppingListId = shoppingList.Id,
                    UserId = ivan.Id,
                    Role = "member",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            context.ShoppingItems.AddRange(
                new ShoppingItem
                {
                    Id = Guid.NewGuid(),
                    ShoppingListId = shoppingList.Id,
                    Name = "Молоко",
                    Quantity = 2,
                    Unit = "л",
                    Category = "Молочное",
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ShoppingItem
                {
                    Id = Guid.NewGuid(),
                    ShoppingListId = shoppingList.Id,
                    Name = "Хлеб",
                    Quantity = 1,
                    Unit = "шт",
                    Category = "Хлебобулочное",
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ShoppingItem
                {
                    Id = Guid.NewGuid(),
                    ShoppingListId = shoppingList.Id,
                    Name = "Яйца",
                    Quantity = 10,
                    Unit = "шт",
                    Category = "Молочное",
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ShoppingItem
                {
                    Id = Guid.NewGuid(),
                    ShoppingListId = shoppingList.Id,
                    Name = "Овощи (огурцы, помидоры)",
                    Quantity = 1,
                    Unit = "кг",
                    Category = "Овощи",
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            context.Notifications.AddRange(
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = ivan.Id,
                    Type = "calendar_invite",
                    Title = "Приглашение в календарь",
                    Message = "Анна Петрова добавила вас в календарь 'Рабочий'",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = ivan.Id,
                    Type = "shopping_list_invite",
                    Title = "Новый список покупок",
                    Message = "Вас добавили в список 'Продукты'",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
