using System.Net;
using System.Net.Http.Json;
using Syncora.DTO.Calendar;
using Syncora.Models.Enums;
using Xunit;

namespace Syncora.IntegrationTests;

public class CalendarAccessTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly Guid _testUserId = Guid.NewGuid();

    public CalendarAccessTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCalendar_DefaultType_Personal()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_testUserId, "user@example.com", "Test User");
        var request = new CreateCalendarRequest
        {
            Name = "My Calendar",
            Color = "#A78BFA"
            // Type не указан - должен быть Personal по умолчанию
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calendars", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(result);
        Assert.Equal("personal", result.Type); // Должен быть "personal" по умолчанию
    }

    [Fact]
    public async Task CreateCalendar_ExplicitWorkType_Success()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_testUserId, "user@example.com", "Test User");
        var request = new CreateCalendarRequest
        {
            Name = "Work Calendar",
            Color = "#34D399",
            Type = "work"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calendars", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(result);
        Assert.Equal("work", result.Type);
    }

    [Fact]
    public async Task CreateCalendar_InvalidType_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_testUserId, "user@example.com", "Test User");
        var request = new CreateCalendarRequest
        {
            Name = "Invalid Calendar",
            Color = "#A78BFA",
            Type = "invalid_type"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calendars", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_Success()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_testUserId, "owner@example.com", "Owner");
        
        // Создаём календарь
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Group Calendar",
            Color = "#34D399",
            Type = "group"
        };
        var calendarResponse = await client.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Act - добавляем участника
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "member",
            AccessLevel = "edit"
        };
        var memberResponse = await client.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, memberResponse.StatusCode);
    }

    [Fact]
    public async Task AddMember_InvalidRole_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_testUserId, "owner@example.com", "Owner");
        
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Group Calendar",
            Color = "#34D399",
            Type = "group"
        };
        var calendarResponse = await client.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Act
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "invalid_role",
            AccessLevel = "edit"
        };
        var memberResponse = await client.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, memberResponse.StatusCode);
    }

    [Fact]
    public async Task AddMember_InvalidAccessLevel_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_testUserId, "owner@example.com", "Owner");
        
        var calendarRequest = new CreateCalendarRequest
        {
            Name = "Group Calendar",
            Color = "#34D399",
            Type = "group"
        };
        var calendarResponse = await client.PostAsJsonAsync("/api/calendars", calendarRequest);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<CalendarDto>();
        Assert.NotNull(calendar);

        // Act
        var memberRequest = new AddCalendarMemberRequest
        {
            Email = "member@example.com",
            Role = "member",
            AccessLevel = "invalid_access"
        };
        var memberResponse = await client.PostAsJsonAsync($"/api/calendars/{calendar.Id}/members", memberRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, memberResponse.StatusCode);
    }
}