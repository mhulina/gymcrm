namespace GymCRM.SchedulingAPI.Constants;

public static class ValidationMessages
{
    // Booking validation messages
    public const string BookingOverlapsExisting = "This booking overlaps with an existing booking.";
    public const string BookingOutsideAvailability = "Trainer is not available during the requested time.";
    public const string BookingInPast = "Cannot create bookings in the past.";
    public const string BookingTooFarInFuture = "Cannot create bookings more than 90 days in advance.";
    public const string BookingTooShort = "Booking must be at least 30 minutes long.";
    public const string BookingTooLong = "Booking cannot exceed 90 minutes.";
    public const string TrainerNotFound = "Trainer not found.";
    public const string MemberNotFound = "Member not found.";
    public const string BookingNotFound = "Booking not found.";
    public const string UnauthorizedCancellation = "You are not authorized to cancel this booking.";
    public const string CancellationTooLate = "Bookings cannot be cancelled less than 24 hours in advance.";
    public const string BookingIntervals = "Bookings need to be made in 30 minute intervals.";
    public const string BookingOnHoliday = "Booking dates can not be on holidays.";
    public const string InvalidBooking = "Invalid booking.";
    
    // Availability validation messages
    public const string AvailabilityOverlapsExisting = "This availability overlaps with an existing availability.";
    public const string AvailabilityInPast = "Cannot create availability in the past.";
    public const string AvailabilityHasBookings = "Cannot delete availability that has existing bookings.";
}