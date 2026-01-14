using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface ITimeOffService
{
    /// <summary>
    /// Retrieves all time-off records from the repository.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An enumerable collection of <see cref="TimeOff"/> objects.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<IEnumerable<TimeOff>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all time-off records for the specified trainer.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An enumerable collection of <see cref="TimeOff"/> objects for the given trainer.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="trainerId"/> is an empty GUID.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<IEnumerable<TimeOff>> GetAllForTrainerIdAsync(Guid trainerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all time-off records for the specified trainer in the specified month.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="month">Month for which the search is being done</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An enumerable collection of <see cref="TimeOff"/> objects for the given trainer.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="trainerId"/> is an empty GUID.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<IEnumerable<TimeOff>> GetAllForTrainerIdInMonthAsync(
        Guid trainerId,
        int month,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all time-off records within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A dictionary where the key is a <see cref="DateTime"/> representing the date,
    /// and the value is a list of <see cref="TimeOff"/> records for that date.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="endDate"/> is earlier than <paramref name="startDate"/>  
    /// or if either date is <see cref="DateTime.MinValue"/> or <see cref="DateTime.MaxValue"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<IDictionary<DateTime, List<TimeOff>>> GetAllForDatePeriodAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds a new time-off record.
    /// </summary>
    /// <param name="insertTimeOff">The DTO containing the new time-off details.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the time-off was successfully added; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="insertTimeOff"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<bool> AddTimeOffAsync(InsertTimeOff insertTimeOff, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes an existing time-off record by its unique identifier.
    /// </summary>
    /// <param name="timeOffId">The unique identifier of the time-off to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the time-off was successfully deleted; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="timeOffId"/> is an empty GUID.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<bool> DeleteTimeOffAsync(Guid timeOffId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates an existing time-off record.
    /// </summary>
    /// <param name="updatedTimeOff">The updated <see cref="TimeOff"/> object.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the time-off was successfully updated; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="updatedTimeOff"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<bool> UpdateTimeOffAsync (TimeOff updatedTimeOff, CancellationToken cancellationToken = default);
}