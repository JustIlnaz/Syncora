using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Syncora.Models.Enums;

namespace Syncora.Data.Converters
{
    public static class CalendarEnumMapper
    {
        public static string AccessLevelToString(AccessLevel a) => a switch
        {
            AccessLevel.Full => "full",
            AccessLevel.Edit => "edit",
            AccessLevel.View => "view",
            AccessLevel.FreeBusy => "free-busy",
            _ => "view"
        };

        public static AccessLevel StringToAccessLevel(string? s) => (s ?? string.Empty).ToLower() switch
        {
            "full" => AccessLevel.Full,
            "edit" => AccessLevel.Edit,
            "view" => AccessLevel.View,
            "free-busy" => AccessLevel.FreeBusy,
            _ => AccessLevel.View
        };

        public static string RoleToString(Syncora.Models.Enums.CalendarRole r) => r switch
        {
            Syncora.Models.Enums.CalendarRole.Owner => "owner",
            Syncora.Models.Enums.CalendarRole.Member => "member",
            _ => "member"
        };

        public static Syncora.Models.Enums.CalendarRole StringToRole(string? s) => (s ?? string.Empty).ToLower() switch
        {
            "owner" => Syncora.Models.Enums.CalendarRole.Owner,
            "member" => Syncora.Models.Enums.CalendarRole.Member,
            _ => Syncora.Models.Enums.CalendarRole.Member
        };

        public static bool TryParseRole(string? s, out Syncora.Models.Enums.CalendarRole role)
        {
            switch ((s ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "owner":
                    role = Syncora.Models.Enums.CalendarRole.Owner;
                    return true;
                case "member":
                    role = Syncora.Models.Enums.CalendarRole.Member;
                    return true;
                default:
                    role = Syncora.Models.Enums.CalendarRole.Member;
                    return false;
            }
        }

        public static bool TryParseAccessLevel(string? s, out AccessLevel accessLevel)
        {
            switch ((s ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "full":
                    accessLevel = AccessLevel.Full;
                    return true;
                case "edit":
                    accessLevel = AccessLevel.Edit;
                    return true;
                case "view":
                    accessLevel = AccessLevel.View;
                    return true;
                case "free-busy":
                    accessLevel = AccessLevel.FreeBusy;
                    return true;
                default:
                    accessLevel = AccessLevel.View;
                    return false;
            }
        }

        public static string CalendarTypeToString(Syncora.Models.Enums.CalendarType t) => t switch
        {
            Syncora.Models.Enums.CalendarType.Personal => "personal",
            Syncora.Models.Enums.CalendarType.Work => "work",
            Syncora.Models.Enums.CalendarType.Group => "group",
            _ => "personal"
        };

        public static Syncora.Models.Enums.CalendarType StringToCalendarType(string? s) => (s ?? string.Empty).ToLower() switch
        {
            "personal" => Syncora.Models.Enums.CalendarType.Personal,
            "work" => Syncora.Models.Enums.CalendarType.Work,
            "group" => Syncora.Models.Enums.CalendarType.Group,
            _ => Syncora.Models.Enums.CalendarType.Personal
        };

        public static bool TryParseCalendarType(string? s, out Syncora.Models.Enums.CalendarType calendarType)
        {
            switch ((s ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "personal":
                    calendarType = Syncora.Models.Enums.CalendarType.Personal;
                    return true;
                case "work":
                    calendarType = Syncora.Models.Enums.CalendarType.Work;
                    return true;
                case "group":
                    calendarType = Syncora.Models.Enums.CalendarType.Group;
                    return true;
                default:
                    calendarType = Syncora.Models.Enums.CalendarType.Personal;
                    return false;
            }
        }

        public static ValueConverter<Syncora.Models.Enums.AccessLevel, string> AccessLevelConverter =>
            new ValueConverter<Syncora.Models.Enums.AccessLevel, string>(
                v => AccessLevelToString(v),
                v => StringToAccessLevel(v));

        public static ValueConverter<Syncora.Models.Enums.CalendarRole, string> RoleConverter =>
            new ValueConverter<Syncora.Models.Enums.CalendarRole, string>(
                v => RoleToString(v),
                v => StringToRole(v));

        public static ValueConverter<Syncora.Models.Enums.CalendarType, string> CalendarTypeConverter =>
            new ValueConverter<Syncora.Models.Enums.CalendarType, string>(
                v => CalendarTypeToString(v),
                v => StringToCalendarType(v));
    }
}
