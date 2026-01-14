namespace GymCRM.Shared.Utilities;

public static class DateTimeHelpers
{
    public static DateTime EnsureUtc(DateTime dateTime) 
        => dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    
    public static DateTimeOffset ToUtcOffset(this DateTime dateTime, TimeSpan offset)
        => new DateTimeOffset(EnsureUtc(dateTime)).ToOffset(offset);

    public static bool Between(this DateTime dateTime, DateTime start, DateTime end, bool includeTime = true)
    {
        if (!includeTime)
        {
            var resultWithoutIncludedTime = dateTime.Date >= start.Date && dateTime.Date <= end.Date;
            return resultWithoutIncludedTime;
        }
        
        var resultWithIncludedTime = dateTime >= start && dateTime <= end;
        return resultWithIncludedTime;
    }
}