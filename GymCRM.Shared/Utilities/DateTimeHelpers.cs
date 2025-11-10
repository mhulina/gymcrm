namespace GymCRM.Shared.Utilities;

public static class DateTimeHelpers
{
    public static DateTime EnsureUtc(DateTime dateTime) 
        => dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    
    public static DateTimeOffset ToUtcOffset(this DateTime dateTime, TimeSpan offset)
        => new DateTimeOffset(EnsureUtc(dateTime)).ToOffset(offset);
}