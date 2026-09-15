using System.Net;
using System.Net.Http.Json;
using Syncora.DTO.Calendar;
using Syncora.DTO.Event;
using Xunit;

namespace Syncora.IntegrationTests;

public class AccessLevelTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();

    public AccessLevelTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ViewAccess_CannotCreateEvent_Returns403()
    {
        // Arrange
        var ownerClient = _factory.CreateAuthenticatedClient(_ownerId, "owner@example.com", "Owner");
        var memberClient = _factory.CreateAuthenticatedClient(_memberId, "member@example.com", "Member");

        // Создаём календарь
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Test Calendar",
            Color = "#A78BFA",
            Type = "group"
        };
        var calendarResponse = await ownerClient.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Добавляем участника с доступом view
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "member",
            AccessLevel = "view"
        };
        await ownerClient.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Act - пытаемся создать событие с доступом view
        var eventRequest = new CreateEventRequest
        {
            Title = "Test Event",
            CalendarId = calendar.Id,
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2)
        };
        var eventResponse = await memberClient.PostAsJsonAsync("/api/events", eventRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, eventResponse.StatusCode);
    }

    [Fact]
    public async Task FreeBusyAccess_CannotCreateEvent_Returns403()
    {
        // Arrange
        var ownerClient = _factory.CreateAuthenticatedClient(_ownerId, "owner@example.com", "Owner");
        var memberClient = _factory.CreateAuthenticatedClient(_memberId, "member@example.com", "Member");

        // Создаём календарь
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Test Calendar",
            Color = "#A78BFA",
            Type = "group"
        };
        var calendarResponse = await ownerClient.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Добавляем участника с доступом free-busy
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "member",
            AccessLevel = "free-busy"
        };
        await ownerClient.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Act - пытаемся создать событие с доступом free-busy
        var eventRequest = new CreateEventRequest
        {
            Title = "Test Event",
            CalendarId = calendar.Id,
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2)
        };
        var eventResponse = await memberClient.PostAsJsonAsync("/api/events", eventRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, eventResponse.StatusCode);
    }

    [Fact]
    public async Task EditAccess_CanCreateEvent_Success()
    {
        // Arrange
        var ownerClient = _factory.CreateAuthenticatedClient(_ownerId, "owner@example.com", "Owner");
        var memberClient = _factory.CreateAuthenticatedClient(_memberId, "member@example.com", "Member");

        // Создаём календарь
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Test Calendar",
            Color = "#A78BFA",
            Type = "group"
        };
        var calendarResponse = await ownerClient.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Добавляем участника с доступом edit
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "member",
            AccessLevel = "edit"
        };
        await ownerClient.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Act - создаём событие с доступом edit
        var eventRequest = new CreateEventRequest
        {
            Title = "Test Event",
            CalendarId = calendar.Id,
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2)
        };
        var eventResponse = await memberClient.PostAsJsonAsync("/api/events", eventRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
    }

    [Fact]
    public async Task FreeBusyAccess_MaskedEventDetails()
    {
        // Arrange
        var ownerClient = _factory.CreateAuthenticatedClient(_ownerId, "owner@example.com", "Owner");
        var memberClient = _factory.CreateAuthenticatedClient(_memberId, "member@example.com", "Member");

        // Создаём календарь
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Test Calendar",
            Color = "#A78BFA",
            Type = "group"
        };
        var calendarResponse = await ownerClient.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Владелец создаёт событие
        var eventRequest = new CreateEventRequest
        {
            Title = "Secret Meeting",
            Description = "Very important secret details",
            CalendarId = calendar.Id,
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2)
        };
        await ownerClient.PostAsJsonAsync("/api/events", eventRequest);

        // Добавляем участника с доступом free-busy
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "member",
            AccessLevel = "free-busy"
        };
        await ownerClient.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Act - получаем события
        var eventsResponse = await memberClient.GetAsync($"/api/events?start={DateTime.UtcNow.AddHours(-1):o}&end={DateTime.UtcNow.AddHours(3):o}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);

        var events = await eventsResponse.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.NotNull(events);
        var maskedEvent = events.FirstOrDefault(e => e.CalendarId == calendar.Id);
        Assert.NotNull(maskedEvent);

        // Проверяем маскирование деталей
        Assert.Equal("Занят", maskedEvent.Title);
        Assert.Null(maskedEvent.Description);
    }

    [Fact]
    public async Task ViewAccess_FullEventDetails()
    {
        // Arrange
        var ownerClient = _factory.CreateAuthenticatedClient(_ownerId, "owner@example.com", "Owner");
        var memberClient = _factory.CreateAuthenticatedClient(_memberId, "member@example.com", "Member");

        // Создаём календарь
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Test Calendar",
            Color = "#A78BFA",
            Type = "group"
        };
        var calendarResponse = await ownerClient.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Владелец создаёт событие
        var eventRequest = new CreateEventRequest
        {
            Title = "Public Meeting",
            Description = "Public meeting details",
            CalendarId = calendar.Id,
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2)
        };
        await ownerClient.PostAsJsonAsync("/api/events", eventRequest);

        // Добавляем участника с доступом view
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "member",
            AccessLevel = "view"
        };
        await ownerClient.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Act - получаем события
        var eventsResponse = await memberClient.GetAsync($"/api/events?start={DateTime.UtcNow.AddHours(-1):o}&end={DateTime.UtcNow.AddHours(3):o}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);

        var events = await eventsResponse.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.NotNull(events);
        var publicEvent = events.FirstOrDefault(e => e.CalendarId == calendar.Id);
        Assert.NotNull(publicEvent);

        // Проверяем полные детали
        Assert.Equal("Public Meeting", publicEvent.Title);
        Assert.Equal("Public meeting details", publicEvent.Description);
    }

    [Fact]
    public async Task NonMember_CannotAccessCalendar_Returns403()
    {
        // Arrange
        var ownerClient = _factory.CreateAuthenticatedClient(_ownerId, "owner@example.com", "Owner");
        var strangerClient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), "stranger@example.com", "Stranger");

        // Создаём календарь
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Private Calendar",
            Color = "#A78BFA",
            Type = "group"
        };
        var calendarResponse = await ownerClient.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Act - посторонний пытается получить календарь
        var response = await strangerClient.GetAsync($"/api/calendars/{calendar.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
