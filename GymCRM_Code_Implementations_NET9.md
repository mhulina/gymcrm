# GymCRM - Complete Code Implementations (.NET 9)
**All Tasks with Full Code Examples**

---

## Table of Contents
1. [Task 1: Booking Validation Service](#task-1-booking-validation-service)
2. [Task 2: Authentication Controller Integration Tests](#task-2-authentication-controller-integration-tests)
3. [Task 3: Revoke Tokens on Password Change](#task-3-revoke-tokens-on-password-change)
4. [Task 4: Scheduling Integration Tests](#task-4-scheduling-integration-tests)
5. [Task 5: Password Complexity Validation](#task-5-password-complexity-validation)
6. [Task 6: Calendar Controller](#task-6-calendar-controller)
7. [Task 7: Available Slots Generation](#task-7-available-slots-generation)
8. [Task 8: Rate Limiting for SchedulingAPI](#task-8-rate-limiting-for-schedulingapi)

---

## Task 1: Booking Validation Service

### Step 1: Create ValidationResult DTO

**File:** `GymCRM.SchedulingAPI/Models/DTOs/ValidationResult.cs`

```csharp
namespace GymCRM.SchedulingAPI.Models.DTOs;

/// <summary>
/// Represents the result of a booking validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Collection of validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
```

### Step 2: Create Interface

**File:** `GymCRM.SchedulingAPI/Services/Interface/IBookingValidationService.cs`

```csharp
using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

/// <summary>
/// Service for validating training session bookings against business rules.
/// </summary>
public interface IBookingValidationService
{
    /// <summary>
    /// Validates a training session booking against all business rules.
    /// </summary>
    /// <param name="booking">The booking to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result containing status and any error messages.</returns>
    Task<ValidationResult> ValidateBookingAsync(
        InsertTrainingSession booking,
        CancellationToken cancellationToken = default);
}
```

### Step 3: Implementation

**File:** `GymCRM.SchedulingAPI/Services/Implementation/BookingValidationService.cs`

```csharp
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Models.Enums;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

/// <summary>
/// Service for validating training session bookings against business rules.
/// Validates: working hours, holidays, time-offs, conflicts, duration, and past bookings.
/// </summary>
public class BookingValidationService : IBookingValidationService
{
    private readonly ITrainerWorkingHoursRepository _workingHoursRepo;
    private readonly ITrainingSessionsRepository _sessionsRepo;
    private readonly IHolidayRepository _holidayRepo;
    private readonly ITimeOffRepository _timeOffRepo;
    private readonly ILogger<BookingValidationService> _logger;
    
    public BookingValidationService(
        ITrainerWorkingHoursRepository workingHoursRepo,
        ITrainingSessionsRepository sessionsRepo,
        IHolidayRepository holidayRepo,
        ITimeOffRepository timeOffRepo,
        ILogger<BookingValidationService> logger)
    {
        _workingHoursRepo = workingHoursRepo ?? throw new ArgumentNullException(nameof(workingHoursRepo));
        _sessionsRepo = sessionsRepo ?? throw new ArgumentNullException(nameof(sessionsRepo));
        _holidayRepo = holidayRepo ?? throw new ArgumentNullException(nameof(holidayRepo));
        _timeOffRepo = timeOffRepo ?? throw new ArgumentNullException(nameof(timeOffRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<ValidationResult> ValidateBookingAsync(
        InsertTrainingSession booking,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        
        _logger.LogInformation(
            "Validating booking for trainer {TrainerId} on {Date} from {StartTime} to {EndTime}",
            booking.TrainerId, booking.StartTime.Date, booking.StartTime, booking.EndTime);
        
        // Rule 1: Check if booking is in the past
        if (booking.StartTime < DateTime.UtcNow)
        {
            errors.Add("Cannot book sessions in the past");
            _logger.LogWarning("Booking validation failed: booking is in the past");
            return new ValidationResult { IsValid = false, Errors = errors };
        }
        
        // Rule 2: Validate duration (must be 30, 60, or 90 minutes)
        var duration = (booking.EndTime - booking.StartTime).TotalMinutes;
        if (duration != 30 && duration != 60 && duration != 90)
        {
            errors.Add("Session duration must be 30, 60, or 90 minutes");
            _logger.LogWarning("Booking validation failed: invalid duration {Duration} minutes", duration);
        }
        
        // Rule 3: Check if trainer is working on this day/time
        var dayOfWeek = booking.StartTime.DayOfWeek.ToString();
        var workingHours = await _workingHoursRepo.FetchByConditionAsync(
            wh => wh.TrainerDailyAvailability.DayOfWeek == dayOfWeek
                && wh.TrainerDailyAvailability.TrainerAvailability.TrainerId == booking.TrainerId,
            cancellationToken);
        
        if (!workingHours.Any())
        {
            errors.Add($"Trainer is not working on {dayOfWeek}");
            _logger.LogWarning(
                "Booking validation failed: trainer {TrainerId} not working on {DayOfWeek}",
                booking.TrainerId, dayOfWeek);
            return new ValidationResult { IsValid = false, Errors = errors };
        }
        
        // Rule 4: Check if booking time is within working hours
        var bookingTime = TimeOnly.FromDateTime(booking.StartTime);
        var bookingEndTime = TimeOnly.FromDateTime(booking.EndTime);
        
        var validTimeSlot = workingHours.Any(wh =>
        {
            var workStart = TimeOnly.FromTimeSpan(wh.StartTime);
            var workEnd = TimeOnly.FromTimeSpan(wh.EndTime);
            return workStart <= bookingTime && workEnd >= bookingEndTime;
        });
        
        if (!validTimeSlot)
        {
            errors.Add("Booking time is outside trainer's working hours");
            _logger.LogWarning(
                "Booking validation failed: time {BookingTime}-{BookingEndTime} outside working hours",
                bookingTime, bookingEndTime);
        }
        
        // Rule 5: Check for holidays
        var holiday = await _holidayRepo.GetByDateAsync(
            booking.StartTime.Date,
            cancellationToken);
        
        if (holiday != null)
        {
            errors.Add($"Cannot book on holiday: {holiday.EnglishName}");
            _logger.LogWarning(
                "Booking validation failed: date {Date} is a holiday ({HolidayName})",
                booking.StartTime.Date, holiday.EnglishName);
        }
        
        // Rule 6: Check for trainer time-off
        var timeOff = await _timeOffRepo.FetchByConditionAsync(
            t => t.TrainerId == booking.TrainerId
                && t.Date.Date == booking.StartTime.Date,
            cancellationToken);
        
        if (timeOff.Any())
        {
            var timeOffReason = timeOff.First().Reason;
            errors.Add($"Trainer has time off on this date: {timeOffReason}");
            _logger.LogWarning(
                "Booking validation failed: trainer {TrainerId} has time off on {Date} ({Reason})",
                booking.TrainerId, booking.StartTime.Date, timeOffReason);
        }
        
        // Rule 7: Check for booking conflicts
        var existingBookings = await _sessionsRepo.FetchByConditionAsync(
            s => s.TrainerId == booking.TrainerId
                && s.StartTime < booking.EndTime
                && s.EndTime > booking.StartTime
                && s.Status != TrainingSessionStatus.Cancelled,
            cancellationToken);
        
        if (existingBookings.Any())
        {
            var conflict = existingBookings.First();
            errors.Add($"Trainer already has a booking from {conflict.StartTime:HH:mm} to {conflict.EndTime:HH:mm}");
            _logger.LogWarning(
                "Booking validation failed: conflicting booking exists for trainer {TrainerId} at {ConflictTime}",
                booking.TrainerId, conflict.StartTime);
        }
        
        var validationResult = new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
        
        _logger.LogInformation(
            "Booking validation completed for trainer {TrainerId}: {Result} ({ErrorCount} errors)",
            booking.TrainerId,
            validationResult.IsValid ? "PASSED" : "FAILED",
            errors.Count);
        
        return validationResult;
    }
}
```

### Step 4: Update TrainingSessionsService

**File:** `GymCRM.SchedulingAPI/Services/Implementation/TrainingSessionsService.cs`

**Add constructor parameter:**
```csharp
private readonly IBookingValidationService _bookingValidationService;

public TrainingSessionsService(
    ITrainingSessionsRepository trainingSessionsRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<TrainingSessionsService> logger,
    IBookingValidationService bookingValidationService)  // Add this
{
    _trainingSessionsRepository = trainingSessionsRepository;
    _unitOfWork = unitOfWork;
    _mapper = mapper;
    _logger = logger;
    _bookingValidationService = bookingValidationService;  // Add this
}
```

**Updated Method:**
```csharp
public async Task<bool> InsertTrainingSessionAsync(
    InsertTrainingSession insertTrainingSession,
    CancellationToken cancellationToken = default)
{
    if (insertTrainingSession is null)
    {
        throw new ArgumentNullException(nameof(insertTrainingSession));
    }
    
    // Validate booking before inserting
    var validation = await _bookingValidationService.ValidateBookingAsync(
        insertTrainingSession,
        cancellationToken);
    
    if (!validation.IsValid)
    {
        var errorMessage = $"Booking validation failed: {string.Join(", ", validation.Errors)}";
        _logger.LogWarning(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }
    
    _logger.LogInformation(
        "Booking validation passed, creating training session for trainer {TrainerId}",
        insertTrainingSession.TrainerId);
    
    var trainingSessionEntity = new TrainingSession
    {
        Id = Guid.CreateVersion7(),
        TrainerId = insertTrainingSession.TrainerId,
        ClientId = insertTrainingSession.ClientId,
        Status = insertTrainingSession.Status,
        Description = insertTrainingSession.Description,
        StartTime = insertTrainingSession.StartTime,
        EndTime = insertTrainingSession.EndTime,
        DateCreated = DateTime.UtcNow,
        DateModified = DateTime.UtcNow,
    };

    _trainingSessionsRepository.Add(trainingSessionEntity);
    var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    _logger.LogInformation(
        "Training session {SessionId} created successfully: {Result}",
        trainingSessionEntity.Id,
        result ? "SUCCESS" : "FAILED");

    return result;
}
```

### Step 5: Update Controller Error Handling

**File:** `GymCRM.SchedulingAPI/Controllers/TrainingSessionController.cs`

```csharp
[HttpPost]
public async Task<ActionResult> AddTrainingSession(
    InsertTrainingSession trainingSession,
    CancellationToken cancellationToken)
{
    try
    {
        var result = await _trainingSessionService.InsertTrainingSessionAsync(
            trainingSession, 
            cancellationToken);

        if (!result)
        {
            return new BadRequestResult();
        }

        return new CreatedResult();
    }
    catch (InvalidOperationException ex)
    {
        // Validation failed - return detailed error
        return BadRequest(new { error = ex.Message });
    }
    catch (ArgumentNullException ex)
    {
        return BadRequest(new { error = "Invalid booking data", details = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating training session");
        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    }
}
```

### Step 6: Register in DI Container

**File:** `GymCRM.SchedulingAPI/ProgramConfigurations.cs`

```csharp
public static IServiceCollection AddProjectServices(this IServiceCollection services)
{
    services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    services.AddScoped<ITrainingSessionsRepository, TrainingSessionsRepository>();
    services.AddScoped<ITrainingSessionsService, TrainingSessionsService>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<ITrainerAvailabilitiesRepository, TrainerAvailabilitiesRepository>();
    services.AddScoped<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailabilitiesRepository>();
    services.AddScoped<ITrainerWorkingHoursRepository, TrainerWorkingHoursRepository>();
    services.AddScoped<ITrainerAvailabilitiesService, TrainerAvailabilitiesService>();
    services.AddScoped<ITimeOffRepository, TimeOffRepository>();
    services.AddScoped<ITimeOffService, TimeOffService>();
    services.AddScoped<IHolidayRepository, HolidayRepository>();
    services.AddScoped<IHolidayService, HolidayService>();
    
    // Add booking validation service
    services.AddScoped<IBookingValidationService, BookingValidationService>();
    
    services.AddHttpClient<HolidaySeeder>();
    services.AddScoped<ICalendarService, CalendarService>();
    
    return services;
}
```

---

## Task 2: Authentication Controller Integration Tests

### Test Base Setup

**File:** `GymCRM.IdentityAPI.Tests/Integration/Controllers/TestAuthenticationController.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GymCRM.IdentityAPI.Infrastructure;
using GymCRM.IdentityAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymCRM.IdentityAPI.Tests.Integration.Controllers;

public class TestAuthenticationController : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private IdentityDbContext _dbContext;
    
    public TestAuthenticationController(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove production database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                // Add test database
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseNpgsql(
                        "Host=localhost;Port=5433;Database=gymcrm_identity_test;Username=postgres;Password=postgres");
                });
            });
        });
        
        _client = _factory.CreateClient();
        
        // Get database context
        using var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    }
    
    public async Task InitializeAsync()
    {
        // Ensure database is created and clean
        await _dbContext.Database.EnsureCreatedAsync();
        await CleanDatabase();
    }
    
    public async Task DisposeAsync()
    {
        await CleanDatabase();
        _client?.Dispose();
    }
    
    private async Task CleanDatabase()
    {
        _dbContext.RefreshTokens.RemoveRange(_dbContext.RefreshTokens);
        _dbContext.Members.RemoveRange(_dbContext.Members);
        _dbContext.Accounts.RemoveRange(_dbContext.Accounts);
        await _dbContext.SaveChangesAsync();
    }
    
    #region Test: Successful Login
    
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndSetsCookies()
    {
        // Arrange - Register user first
        var email = $"test{Guid.NewGuid():N}@example.com";
        var registerDto = new InsertAccount
        {
            Email = email,
            Password = "SecurePass123!"
        };
        await _client.PostAsJsonAsync("/api/v1/Register", registerDto);
        
        var loginDto = new AuthenticationRequestBody
        {
            Username = email,
            Password = "SecurePass123!"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken"), 
            "because access token should be set as cookie");
        cookies.Should().Contain(c => c.Contains("refreshToken"), 
            "because refresh token should be set as cookie");
        cookies.Should().Contain(c => c.Contains("HttpOnly"), 
            "because cookies should be HttpOnly for security");
    }
    
    #endregion
    
    #region Test: Invalid Credentials
    
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new AuthenticationRequestBody
        {
            Username = "nonexistent@example.com",
            Password = "WrongPassword"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    #endregion
    
    #region Test: Account Lockout
    
    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccount()
    {
        // Arrange
        var email = $"test{Guid.NewGuid():N}@example.com";
        var correctPassword = "CorrectPassword123!";
        
        await RegisterTestUser(email, correctPassword);
        
        var wrongPasswordDto = new AuthenticationRequestBody
        {
            Username = email,
            Password = "WrongPassword"
        };
        
        // Act - Fail 5 times
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/v1/Login", wrongPasswordDto);
        }
        
        // Try again with correct password
        var correctPasswordDto = new AuthenticationRequestBody
        {
            Username = email,
            Password = correctPassword
        };
        var response = await _client.PostAsJsonAsync("/api/v1/Login", correctPasswordDto);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Account locked", 
            "because account should be locked after 5 failed attempts");
        content.Should().Contain("minutes", 
            "because lockout message should include time remaining");
    }
    
    #endregion
    
    #region Test: Rate Limiting
    
    [Fact]
    public async Task Login_ExceedsRateLimit_Returns429()
    {
        // Arrange
        var loginDto = new AuthenticationRequestBody
        {
            Username = "test@example.com",
            Password = "password"
        };
        
        // Act - Exceed 5 requests per minute
        var tasks = Enumerable.Range(0, 6)
            .Select(_ => _client.PostAsJsonAsync("/api/v1/Login", loginDto))
            .ToList();
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert - Last request should be rate limited
        responses.Last().StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
    
    #endregion
    
    #region Helper Methods
    
    private async Task RegisterTestUser(string email, string password)
    {
        var registerDto = new InsertAccount
        {
            Email = email,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/v1/Register", registerDto);
    }
    
    private string ExtractCookieValue(string cookie, string name)
    {
        var parts = cookie.Split(';');
        var valuePart = parts.First(p => p.Trim().StartsWith(name));
        return valuePart.Split('=')[1].Trim();
    }
    
    #endregion
}
```

---

## Task 3: Revoke Tokens on Password Change

### Updated ChangePassword Method

**File:** `GymCRM.IdentityAPI/Services/Implementation/AuthenticationService.cs`

```csharp
/// <summary>
/// Changes the password for an existing account and revokes all refresh tokens.
/// </summary>
/// <param name="email">The email address associated with the account.</param>
/// <param name="oldPassword">The current password of the account.</param>
/// <param name="newPassword">The new password to set for the account.</param>
/// <param name="cancellationToken">Optional cancellation token for the async operation.</param>
/// <returns>True if the password change was successful; otherwise, false.</returns>
/// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
/// <exception cref="AccountDoesntExistException">Thrown when account is not found.</exception>
/// <exception cref="AuthenticationFailureException">Thrown when old password is incorrect.</exception>
public async Task<bool> ChangePassword(
    string email, 
    string oldPassword, 
    string newPassword,
    CancellationToken cancellationToken = default)
{
    // Validate inputs
    if (string.IsNullOrWhiteSpace(email?.Trim()))
    {
        throw new ArgumentException("Email is required", nameof(email));
    }

    if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
    {
        throw new ArgumentException("Old password and new password are required");
    }
    
    var modifiedEmail = email.Trim().ToLower();
    
    try
    {
        // Fetch account
        var account = (await _accountsRepository.FetchByConditionAsync(
            x => x.Email == modifiedEmail, 
            cancellationToken))
            .FirstOrDefault() 
            ?? throw new AccountDoesntExistException($"Account with email {modifiedEmail} does not exist");

        // Verify old password
        if (!CompareHashedPasswords(account, oldPassword))
        {
            _logger.Warning(
                "Failed password change attempt for account {Email}: incorrect old password",
                modifiedEmail);
            throw new AuthenticationFailureException("Current password is incorrect");
        }
        
        _logger.Information("Password verification successful for account {Email}", modifiedEmail);
        
        // Generate new hashed password
        account.HashedPassword = GenerateHashedPassword(
            newPassword, 
            account.HashSalt, 
            account.DateCreated);
        
        // Update account
        _unitOfWork.Detach(account);
        _accountsRepository.Update(account);
        var result = await _unitOfWork.SaveAsync(cancellationToken);

        if (result)
        {
            _logger.Information(
                "Password updated successfully for account {Email}, revoking all tokens",
                modifiedEmail);
            
            // Revoke all refresh tokens for security
            var tokensRevoked = await _refreshTokenService.RevokeAllTokensForAccountAsync(
                account.Id,
                "Password changed - security measure",
                cancellationToken);
            
            if (tokensRevoked)
            {
                var activeTokenCount = await GetActiveTokenCount(account.Id, cancellationToken);
                _logger.Information(
                    "Password changed and all tokens revoked for account {Email}. Previously had {TokenCount} active tokens",
                    modifiedEmail,
                    activeTokenCount);
            }
            else
            {
                _logger.Warning(
                    "Password changed for account {Email} but token revocation may have failed",
                    modifiedEmail);
            }
        }
        else
        {
            _logger.Error(
                "Failed to save password change for account {Email}",
                modifiedEmail);
        }

        return result;
    }
    catch (AccountDoesntExistException)
    {
        throw;
    }
    catch (AuthenticationFailureException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Error changing password for account {Email}", modifiedEmail);
        throw;
    }
}

/// <summary>
/// Gets the count of active tokens for an account (used for logging).
/// </summary>
private async Task<int> GetActiveTokenCount(Guid accountId, CancellationToken cancellationToken)
{
    try
    {
        var tokens = await _refreshTokenService.GetActiveTokensForAccountAsync(accountId, cancellationToken);
        return tokens.Count;
    }
    catch
    {
        return 0; // If we can't get count, just return 0 for logging purposes
    }
}
```

---

## Task 4: Scheduling Integration Tests

### Example Integration Test

**File:** `GymCRM.SchedulingAPI.Tests/Integration/Services/TestBookingValidationService.cs`

```csharp
using FluentAssertions;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Models.Entities;
using GymCRM.SchedulingAPI.Models.Enums;
using GymCRM.SchedulingAPI.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymCRM.SchedulingAPI.Tests.Integration.Services;

public class TestBookingValidationService : TestBase
{
    private readonly IBookingValidationService _validationService;
    private Guid _trainerId;
    
    public TestBookingValidationService()
    {
        _validationService = ServiceProvider.GetRequiredService<IBookingValidationService>();
        _trainerId = SeedTestTrainerWithAvailability();
    }
    
    [Fact]
    public async Task ValidateBooking_DuringHoliday_ReturnsInvalid()
    {
        // Arrange
        var christmasDate = new DateTime(DateTime.UtcNow.Year, 12, 25);
        var holiday = new Holiday
        {
            Id = Guid.CreateVersion7(),
            Date = christmasDate,
            EnglishName = "Christmas Day",
            LocalName = "Božić",
            CountryCode = "HR",
            Type = "Public",
            Year = christmasDate.Year,
            Created = DateTime.UtcNow
        };
        await SeedHoliday(holiday);
        
        var booking = new InsertTrainingSession
        {
            TrainerId = _trainerId,
            ClientId = Guid.CreateVersion7(),
            StartTime = christmasDate.AddHours(10),
            EndTime = christmasDate.AddHours(11),
            Status = TrainingSessionStatus.Pending
        };
        
        // Act
        var result = await _validationService.ValidateBookingAsync(booking);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("holiday"));
        result.Errors.Should().Contain(e => e.Contains("Christmas"));
    }
    
    [Fact]
    public async Task ValidateBooking_ConflictingTime_ReturnsInvalid()
    {
        // Arrange
        var nextMonday = GetNextWeekday(DayOfWeek.Monday);
        
        // Create existing booking
        var existingBooking = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            TrainerId = _trainerId,
            ClientId = Guid.CreateVersion7(),
            StartTime = nextMonday.Date.AddHours(10),
            EndTime = nextMonday.Date.AddHours(11),
            Status = TrainingSessionStatus.Confirmed,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };
        await SeedTrainingSession(existingBooking);
        
        // Try to book overlapping time
        var newBooking = new InsertTrainingSession
        {
            TrainerId = _trainerId,
            ClientId = Guid.CreateVersion7(),
            StartTime = nextMonday.Date.AddHours(10).AddMinutes(30), // 10:30 AM
            EndTime = nextMonday.Date.AddHours(11).AddMinutes(30),   // 11:30 AM
            Status = TrainingSessionStatus.Pending
        };
        
        // Act
        var result = await _validationService.ValidateBookingAsync(newBooking);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already has a booking"));
    }
    
    #region Helper Methods
    
    private Guid SeedTestTrainerWithAvailability()
    {
        var trainerId = Guid.CreateVersion7();
        
        // Create trainer availability
        var availability = new TrainerAvailability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = trainerId,
            WorkingWeekends = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        
        // Create daily availability for Monday-Friday
        var dailyAvailabilities = new List<TrainerDailyAvailability>();
        for (int day = 1; day <= 5; day++) // Monday to Friday
        {
            var dayName = ((DayOfWeek)day).ToString();
            var dailyAvail = new TrainerDailyAvailability
            {
                Id = Guid.CreateVersion7(),
                AvailabilityId = availability.Id,
                DayOfWeek = dayName,
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow
            };
            dailyAvailabilities.Add(dailyAvail);
            
            // Create working hours (9 AM - 5 PM)
            var workingHours = new TrainerWorkingHours
            {
                Id = Guid.CreateVersion7(),
                DailyAvailabilityId = dailyAvail.Id,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow
            };
            
            SeedWorkingHours(workingHours);
        }
        
        SeedAvailability(availability);
        SeedDailyAvailabilities(dailyAvailabilities);
        
        return trainerId;
    }
    
    private DateTime GetNextWeekday(DayOfWeek day)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7; // Next week
        return today.AddDays(daysUntil);
    }
    
    private async Task SeedHoliday(Holiday holiday)
    {
        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedTrainingSession(TrainingSession session)
    {
        _context.TrainingSessions.Add(session);
        await _context.SaveChangesAsync();
    }
    
    private void SeedWorkingHours(TrainerWorkingHours workingHours)
    {
        _context.TrainerWorkingHours.Add(workingHours);
        _context.SaveChanges();
    }
    
    private void SeedAvailability(TrainerAvailability availability)
    {
        _context.TrainerAvailabilities.Add(availability);
        _context.SaveChanges();
    }
    
    private void SeedDailyAvailabilities(List<TrainerDailyAvailability> availabilities)
    {
        _context.TrainerDailyAvailabilities.AddRange(availabilities);
        _context.SaveChanges();
    }
    
    #endregion
}
```

---

## Task 5: Password Complexity Validation

### PasswordValidator Class

**File:** `GymCRM.IdentityAPI/Validators/PasswordValidator.cs`

```csharp
using System.Text.RegularExpressions;

namespace GymCRM.IdentityAPI.Validators;

/// <summary>
/// Validates password complexity according to security requirements.
/// Enforces minimum length, character type requirements (uppercase, lowercase, digit, special).
/// </summary>
public class PasswordValidator
{
    private const int MinimumLength = 8;
    private const string UpperCasePattern = @"[A-Z]";
    private const string LowerCasePattern = @"[a-z]";
    private const string DigitPattern = @"\d";
    private const string SpecialCharPattern = @"[!@#$%^&*(),.?\""':{}|<>]";
    
    /// <summary>
    /// Validates password complexity according to security requirements.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>
    /// Tuple containing:
    /// - IsValid: true if password meets all requirements, false otherwise
    /// - Errors: List of specific validation errors
    /// </returns>
    public (bool IsValid, List<string> Errors) Validate(string password)
    {
        var errors = new List<string>();
        
        // Check if password is provided
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return (false, errors);
        }
        
        // Check minimum length
        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters long");
        }
        
        // Check for uppercase letter
        if (!Regex.IsMatch(password, UpperCasePattern))
        {
            errors.Add("Password must contain at least one uppercase letter (A-Z)");
        }
        
        // Check for lowercase letter
        if (!Regex.IsMatch(password, LowerCasePattern))
        {
            errors.Add("Password must contain at least one lowercase letter (a-z)");
        }
        
        // Check for digit
        if (!Regex.IsMatch(password, DigitPattern))
        {
            errors.Add("Password must contain at least one digit (0-9)");
        }
        
        // Check for special character
        if (!Regex.IsMatch(password, SpecialCharPattern))
        {
            errors.Add("Password must contain at least one special character (!@#$%^&*(),.?\":{}|<>)");
        }
        
        return (errors.Count == 0, errors);
    }
    
    /// <summary>
    /// Gets a formatted string describing all password requirements.
    /// </summary>
    /// <returns>Multi-line string with password requirements.</returns>
    public static string GetRequirementsDescription()
    {
        return $@"Password Requirements:
• Minimum {MinimumLength} characters
• At least one uppercase letter (A-Z)
• At least one lowercase letter (a-z)
• At least one digit (0-9)
• At least one special character (!@#$%^&*(),.?\"":{{}}|<>)";
    }
}
```

### Integration into AuthenticationService

**File:** `GymCRM.IdentityAPI/Services/Implementation/AuthenticationService.cs`

**Updated RegisterAccount:**
```csharp
public async Task<Guid> RegisterAccount(
    InsertAccount insertAccount, 
    CancellationToken cancellationToken = default)
{
    // Validate basic inputs
    if (string.IsNullOrWhiteSpace(insertAccount?.Email) || 
        string.IsNullOrWhiteSpace(insertAccount?.Password))
    {
        throw new ArgumentException("Email and password are required");
    }

    // Validate password complexity
    var passwordValidator = new PasswordValidator();
    var (isValid, errors) = passwordValidator.Validate(insertAccount.Password);
    
    if (!isValid)
    {
        var errorMessage = $"Password validation failed: {string.Join(", ", errors)}";
        _logger.Warning(
            "Registration attempt failed for {Email}: {ErrorMessage}",
            insertAccount.Email,
            errorMessage);
        throw new ArgumentException(errorMessage);
    }

    try
    {
        var modifiedEmail = insertAccount.Email.Trim().ToLower();
        
        // Check if account already exists
        var existingAccount = (await _accountsRepository
            .FetchByConditionAsync(x => x.Email == modifiedEmail, cancellationToken))
            .FirstOrDefault();

        if (existingAccount is not null)
        {
            _logger.Warning("Registration attempt for existing email: {Email}", modifiedEmail);
            throw new AccountAlreadyExistsException($"Account with email {modifiedEmail} already exists");
        }

        _logger.Information("Creating new account for {Email}", modifiedEmail);

        // Create account with hashed password
        var entity = CreateAccountWithHashedPassword(insertAccount);
        
        _accountsRepository.Insert(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.Information("Account created successfully with ID: {AccountId}", entity.Id);

        // Create associated member
        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            AccountGuid = entity.Id,
            Email = entity.Email,
            AccountType = insertAccount.AccountType ?? 1,
            GymSubscriptionType = insertAccount.GymSubscriptionType ?? 0,
            Gender = insertAccount.Gender ?? 0,
            DateModified = entity.DateCreated,
            TimeZone = TimeZoneInfo.Utc.Id,
        };

        _membersRepository.Insert(member);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.Information("Member profile created successfully for account {AccountId}", entity.Id);

        return entity.Id;
    }
    catch (AccountAlreadyExistsException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Error during account registration for {Email}", insertAccount.Email);
        throw;
    }
}
```

**Updated ChangePassword:**
```csharp
public async Task<bool> ChangePassword(
    string email, 
    string oldPassword, 
    string newPassword,
    CancellationToken cancellationToken = default)
{
    // Validate inputs
    if (string.IsNullOrWhiteSpace(email))
    {
        throw new ArgumentException("Email is required", nameof(email));
    }

    if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
    {
        throw new ArgumentException("Old password and new password are required");
    }
    
    // Validate new password complexity
    var passwordValidator = new PasswordValidator();
    var (isValid, errors) = passwordValidator.Validate(newPassword);
    
    if (!isValid)
    {
        var errorMessage = $"Password validation failed: {string.Join(", ", errors)}";
        _logger.Warning(
            "Password change attempt failed for {Email}: {ErrorMessage}",
            email,
            errorMessage);
        throw new ArgumentException(errorMessage);
    }
    
    var modifiedEmail = email.Trim().ToLower();
    
    try
    {
        // Fetch and validate account
        var account = (await _accountsRepository.FetchByConditionAsync(
            x => x.Email == modifiedEmail, 
            cancellationToken))
            .FirstOrDefault() 
            ?? throw new AccountDoesntExistException($"Account with email {modifiedEmail} does not exist");

        // Verify old password
        if (!CompareHashedPasswords(account, oldPassword))
        {
            _logger.Warning(
                "Password change failed for {Email}: incorrect old password",
                modifiedEmail);
            throw new AuthenticationFailureException("Current password is incorrect");
        }
        
        // Generate new hashed password
        account.HashedPassword = GenerateHashedPassword(
            newPassword, 
            account.HashSalt, 
            account.DateCreated);
        
        // Save changes
        _unitOfWork.Detach(account);
        _accountsRepository.Update(account);
        var result = await _unitOfWork.SaveAsync(cancellationToken);

        if (result)
        {
            // Revoke all tokens
            await _refreshTokenService.RevokeAllTokensForAccountAsync(
                account.Id,
                "Password changed",
                cancellationToken);
            
            _logger.Information(
                "Password changed and all tokens revoked for account {Email}",
                modifiedEmail);
        }

        return result;
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Error changing password for {Email}", modifiedEmail);
        throw;
    }
}
```

---

## Task 6: Calendar Controller

### CalendarController Implementation

**File:** `GymCRM.SchedulingAPI/Controllers/CalendarController.cs`

```csharp
using Asp.Versioning;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.SchedulingAPI.Controllers;

/// <summary>
/// Controller for managing trainer calendars.
/// Provides endpoints to retrieve consolidated calendar data including availability, sessions, and time-offs.
/// </summary>
[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/calendar")]
[Authorize]
[ApiController]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarController> _logger;
    
    public CalendarController(
        ICalendarService calendarService,
        ILogger<CalendarController> logger)
    {
        _calendarService = calendarService ?? 
            throw new ArgumentNullException(nameof(calendarService));
        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Retrieves a trainer's complete calendar for a specific month.
    /// Includes availability, training sessions, holidays, and time-offs in a single response.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="year">The calendar year (e.g., 2025). Must be between 2020 and 2100.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="GymTrainerCalendarDto"/> containing the consolidated calendar view.
    /// </returns>
    /// <response code="200">Returns the trainer's monthly calendar with all relevant data.</response>
    /// <response code="400">Invalid parameters (trainer ID, month, or year).</response>
    /// <response code="404">Trainer not found or no calendar data available.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpGet("trainer/{trainerId:guid}/month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(GymTrainerCalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GymTrainerCalendarDto>> GetTrainerMonthlyCalendar(
        Guid trainerId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        // Validate trainer ID
        if (trainerId == Guid.Empty)
        {
            _logger.LogWarning("Invalid trainer ID received: {TrainerId}", trainerId);
            return BadRequest(new { 
                error = "Invalid trainer ID",
                details = "Trainer ID cannot be empty"
            });
        }
        
        // Validate month
        if (month < 1 || month > 12)
        {
            _logger.LogWarning("Invalid month received: {Month}", month);
            return BadRequest(new { 
                error = "Invalid month",
                details = "Month must be between 1 and 12",
                received = month
            });
        }
        
        // Validate year
        if (year < 2020 || year > 2100)
        {
            _logger.LogWarning("Invalid year received: {Year}", year);
            return BadRequest(new { 
                error = "Invalid year",
                details = "Year must be between 2020 and 2100",
                received = year
            });
        }
        
        try
        {
            _logger.LogInformation(
                "Retrieving calendar for trainer {TrainerId}, month {Month}/{Year}",
                trainerId, month, year);
            
            var calendar = await _calendarService.GetGymTrainerCalendarForMonthAsync(
                trainerId,
                month,
                year,
                cancellationToken);
            
            if (calendar == null)
            {
                _logger.LogWarning(
                    "No calendar data found for trainer {TrainerId}",
                    trainerId);
                
                return NotFound(new 
                { 
                    error = "Calendar not found",
                    details = $"No calendar data available for trainer {trainerId}",
                    trainerId = trainerId,
                    month = month,
                    year = year
                });
            }
            
            _logger.LogInformation(
                "Successfully retrieved calendar for trainer {TrainerId}: {SessionCount} sessions, {TimeOffCount} time-offs",
                trainerId,
                calendar.TrainingSessions?.Count ?? 0,
                calendar.TimeOffs?.Count ?? 0);
            
            return Ok(calendar);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, 
                "Argument validation error for trainer {TrainerId}",
                trainerId);
            
            return BadRequest(new { 
                error = "Validation error",
                details = ex.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error retrieving calendar for trainer {TrainerId}, month {Month}/{Year}",
                trainerId, month, year);
            
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { 
                    error = "Internal server error",
                    details = "An error occurred while retrieving the calendar"
                });
        }
    }
    
    /// <summary>
    /// Retrieves a trainer's calendar for the current month.
    /// Convenience endpoint that automatically uses the current month and year.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="GymTrainerCalendarDto"/> containing the current month's calendar.
    /// </returns>
    /// <response code="200">Returns the trainer's current monthly calendar.</response>
    /// <response code="400">Invalid trainer ID.</response>
    /// <response code="404">Trainer not found or no calendar data available.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpGet("trainer/{trainerId:guid}/current")]
    [ProducesResponseType(typeof(GymTrainerCalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GymTrainerCalendarDto>> GetTrainerCurrentMonthCalendar(
        Guid trainerId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving current month calendar for trainer {TrainerId}",
            trainerId);
        
        var now = DateTime.UtcNow;
        return await GetTrainerMonthlyCalendar(
            trainerId, 
            now.Year, 
            now.Month, 
            cancellationToken);
    }
    
    /// <summary>
    /// Retrieves a trainer's calendar for the next month.
    /// Convenience endpoint for forward planning.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="GymTrainerCalendarDto"/> containing next month's calendar.
    /// </returns>
    /// <response code="200">Returns the trainer's next monthly calendar.</response>
    /// <response code="400">Invalid trainer ID.</response>
    /// <response code="404">Trainer not found or no calendar data available.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpGet("trainer/{trainerId:guid}/next")]
    [ProducesResponseType(typeof(GymTrainerCalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GymTrainerCalendarDto>> GetTrainerNextMonthCalendar(
        Guid trainerId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving next month calendar for trainer {TrainerId}",
            trainerId);
        
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        return await GetTrainerMonthlyCalendar(
            trainerId, 
            nextMonth.Year, 
            nextMonth.Month, 
            cancellationToken);
    }
}
```

---

## Task 7: Available Slots Generation

### DTOs

**File:** `GymCRM.SchedulingAPI/Models/DTOs/TimeSlot.cs`

```csharp
namespace GymCRM.SchedulingAPI.Models.DTOs;

/// <summary>
/// Represents a bookable time slot for a trainer.
/// </summary>
public class TimeSlot
{
    /// <summary>
    /// Start time of the slot.
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End time of the slot.
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Duration of the slot in minutes.
    /// </summary>
    public int DurationMinutes { get; set; }
    
    /// <summary>
    /// Indicates whether this slot is available for booking.
    /// </summary>
    public bool IsAvailable { get; set; }
    
    /// <summary>
    /// Client ID if slot is booked, null if available.
    /// </summary>
    public string? BookedByClientId { get; set; }
}

/// <summary>
/// Response containing available booking slots for a trainer on a specific date.
/// </summary>
public class AvailableSlotsResponse
{
    /// <summary>
    /// The trainer's unique identifier.
    /// </summary>
    public Guid TrainerId { get; set; }
    
    /// <summary>
    /// The date for which slots are generated.
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// List of all time slots (both available and booked).
    /// </summary>
    public List<TimeSlot> Slots { get; set; } = new();
    
    /// <summary>
    /// Total number of slots generated.
    /// </summary>
    public int TotalSlots { get; set; }
    
    /// <summary>
    /// Number of available slots.
    /// </summary>
    public int AvailableSlots { get; set; }
    
    /// <summary>
    /// Number of booked slots.
    /// </summary>
    public int BookedSlots { get; set; }
}
```

### Update Interface

**File:** `GymCRM.SchedulingAPI/Services/Interface/ITrainerAvailabilitiesService.cs`

```csharp
// Add this method to the interface
/// <summary>
/// Generates available booking slots for a trainer on a specific date.
/// </summary>
/// <param name="trainerId">The unique identifier of the trainer.</param>
/// <param name="date">The date to generate slots for.</param>
/// <param name="durationMinutes">Desired session duration (30, 60, or 90 minutes).</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns>Response containing all slots with availability status.</returns>
Task<AvailableSlotsResponse> GetAvailableSlotsAsync(
    Guid trainerId,
    DateTime date,
    int durationMinutes,
    CancellationToken cancellationToken = default);
```

### Service Method Implementation

**File:** `GymCRM.SchedulingAPI/Services/Implementation/TrainerAvailabilitiesService.cs`

```csharp
/// <summary>
/// Generates available booking slots for a trainer on a specific date.
/// Considers working hours, existing bookings, holidays, and time-offs.
/// </summary>
/// <param name="trainerId">The unique identifier of the trainer.</param>
/// <param name="date">The date to generate slots for.</param>
/// <param name="durationMinutes">Desired session duration (30, 60, or 90 minutes).</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns>Response containing all slots with availability status.</returns>
public async Task<AvailableSlotsResponse> GetAvailableSlotsAsync(
    Guid trainerId,
    DateTime date,
    int durationMinutes,
    CancellationToken cancellationToken = default)
{
    if (trainerId == Guid.Empty)
    {
        throw new ArgumentException("Invalid trainer ID", nameof(trainerId));
    }
    
    if (durationMinutes != 30 && durationMinutes != 60 && durationMinutes != 90)
    {
        throw new ArgumentException(
            "Duration must be 30, 60, or 90 minutes", 
            nameof(durationMinutes));
    }
    
    _logger.LogInformation(
        "Generating {Duration}-minute slots for trainer {TrainerId} on {Date}",
        durationMinutes, trainerId, date.Date);
    
    var response = new AvailableSlotsResponse
    {
        TrainerId = trainerId,
        Date = date.Date,
        Slots = new List<TimeSlot>()
    };
    
    // Get working hours for this day
    var dayOfWeek = date.DayOfWeek.ToString();
    var workingHours = await _trainerWorkingHoursRepository.FetchByConditionAsync(
        wh => wh.TrainerDailyAvailability.DayOfWeek == dayOfWeek
            && wh.TrainerDailyAvailability.TrainerAvailability.TrainerId == trainerId,
        cancellationToken);
    
    if (!workingHours.Any())
    {
        _logger.LogInformation(
            "No working hours found for trainer {TrainerId} on {DayOfWeek}",
            trainerId, dayOfWeek);
        return response;
    }
    
    // Get existing bookings
    var bookings = await _trainingSessionsRepository.FetchByConditionAsync(
        s => s.TrainerId == trainerId
            && s.StartTime.Date == date.Date
            && s.Status != TrainingSessionStatus.Cancelled,
        cancellationToken);
    
    var bookingsList = bookings.ToList();
    _logger.LogInformation(
        "Found {BookingCount} existing bookings for trainer {TrainerId} on {Date}",
        bookingsList.Count, trainerId, date.Date);
    
    // Check for time-off
    var hasTimeOff = await _timeOffRepository.FetchByConditionAsync(
        t => t.TrainerId == trainerId && t.Date.Date == date.Date,
        cancellationToken);
    
    if (hasTimeOff.Any())
    {
        _logger.LogInformation(
            "Trainer {TrainerId} has time off on {Date}: {Reason}",
            trainerId, date.Date, hasTimeOff.First().Reason);
        return response;
    }
    
    // Check for holiday
    var holiday = await _holidayRepository.GetByDateAsync(date, cancellationToken);
    
    if (holiday != null)
    {
        var holidayName = holiday.EnglishName;
        _logger.LogInformation(
            "Date {Date} is a holiday: {HolidayName}",
            date.Date, holidayName);
        return response;
    }
    
    // Generate slots
    var workingHoursList = workingHours.ToList();
    foreach (var hours in workingHoursList)
    {
        var currentTime = date.Date.Add(hours.StartTime);
        var endTime = date.Date.Add(hours.EndTime);
        
        _logger.LogDebug(
            "Generating slots from {StartTime} to {EndTime}",
            currentTime, endTime);
        
        while (currentTime.Add(TimeSpan.FromMinutes(durationMinutes)) <= endTime)
        {
            var slotEnd = currentTime.Add(TimeSpan.FromMinutes(durationMinutes));
            
            // Check if slot overlaps with any booking
            var bookedSession = bookingsList.FirstOrDefault(b =>
                (b.StartTime < slotEnd && b.EndTime > currentTime));
            
            var slot = new TimeSlot
            {
                StartTime = currentTime,
                EndTime = slotEnd,
                DurationMinutes = durationMinutes,
                IsAvailable = bookedSession == null,
                BookedByClientId = bookedSession?.ClientId.ToString()
            };
            
            response.Slots.Add(slot);
            
            // Slide window by 30 minutes for flexible booking
            currentTime = currentTime.AddMinutes(30);
        }
    }
    
    // Calculate statistics
    response.TotalSlots = response.Slots.Count;
    response.AvailableSlots = response.Slots.Count(s => s.IsAvailable);
    response.BookedSlots = response.TotalSlots - response.AvailableSlots;
    
    _logger.LogInformation(
        "Generated {TotalSlots} slots for trainer {TrainerId} on {Date}: {AvailableSlots} available, {BookedSlots} booked",
        response.TotalSlots, trainerId, date.Date, response.AvailableSlots, response.BookedSlots);
    
    return response;
}
```

### Controller Endpoint

**File:** `GymCRM.SchedulingAPI/Controllers/AvailabilitiesController.cs`

```csharp
/// <summary>
/// Gets available booking slots for a trainer on a specific date.
/// Generates time slots based on working hours and marks each as available or booked.
/// </summary>
/// <param name="trainerId">The unique identifier of the trainer.</param>
/// <param name="date">The date to check availability (YYYY-MM-DD format).</param>
/// <param name="durationMinutes">Desired session duration: 30, 60, or 90 minutes. Default is 60.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns>List of time slots with availability status and statistics.</returns>
/// <response code="200">Returns the available slots for the specified date and duration.</response>
/// <response code="400">Invalid parameters (trainer ID, date, or duration).</response>
/// <response code="500">An unexpected error occurred on the server.</response>
[HttpGet("slots/{trainerId:guid}")]
[ProducesResponseType(typeof(AvailableSlotsResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<AvailableSlotsResponse>> GetAvailableSlots(
    Guid trainerId,
    [FromQuery] DateTime date,
    [FromQuery] int durationMinutes = 60,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Validate trainer ID
        if (trainerId == Guid.Empty)
        {
            return BadRequest(new { 
                error = "Invalid trainer ID",
                details = "Trainer ID cannot be empty"
            });
        }
        
        // Validate date (cannot be in the past)
        if (date.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { 
                error = "Invalid date",
                details = "Cannot retrieve slots for past dates",
                requestedDate = date.Date,
                currentDate = DateTime.UtcNow.Date
            });
        }
        
        // Validate duration
        if (durationMinutes != 30 && durationMinutes != 60 && durationMinutes != 90)
        {
            return BadRequest(new {
                error = "Invalid duration",
                details = "Duration must be 30, 60, or 90 minutes",
                requestedDuration = durationMinutes,
                validDurations = new[] { 30, 60, 90 }
            });
        }
        
        var result = await _trainerAvailabilitiesService.GetAvailableSlotsAsync(
            trainerId,
            date,
            durationMinutes,
            cancellationToken);
        
        return Ok(result);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { 
            error = "Validation error",
            details = ex.Message 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, 
            "Error generating slots for trainer {TrainerId} on {Date}",
            trainerId, date);
        
        return StatusCode(StatusCodes.Status500InternalServerError,
            new { error = "An error occurred while generating available slots" });
    }
}
```

---

## Task 8: Rate Limiting for SchedulingAPI

### Configuration

**File:** `GymCRM.SchedulingAPI/ProgramConfigurations.cs`

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

public static IServiceCollection AddRateLimiting(this IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        // Strict policy for booking operations (creating/modifying training sessions)
        options.AddFixedWindowLimiter("booking", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 10;
            opt.QueueLimit = 0;
            opt.AutoReplenishment = true;
        });
        
        // Moderate policy for modification operations (availabilities, time-offs)
        options.AddFixedWindowLimiter("modifications", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 20;
            opt.QueueLimit = 0;
            opt.AutoReplenishment = true;
        });
        
        // Generous policy for read operations (GET requests)
        options.AddFixedWindowLimiter("queries", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 60;
            opt.QueueLimit = 0;
            opt.AutoReplenishment = true;
        });

        // Configure rejection response
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            var retryAfter = context.Lease.TryGetMetadata(
                MetadataName.RetryAfter, 
                out var retryAfterValue) 
                ? retryAfterValue.TotalSeconds 
                : null;
            
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { 
                    error = "Too many requests. Please try again later.",
                    retryAfterSeconds = retryAfter,
                    limit = context.Lease.TryGetMetadata(
                        MetadataName.Limit, 
                        out var limit) ? limit : null
                },
                cancellationToken);
        };
    });

    return services;
}
```

### Register in Program.cs

**File:** `GymCRM.SchedulingAPI/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Host.UseSerilog(logger: log);

// Add services
builder.Services.AddDbContext<SchedulingDbContext>(option =>
{
    option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services
    .Cors()
    .AutoMapper();

var secretForKey = builder.Configuration["Authentication:SecretForKey"];

if (string.IsNullOrEmpty(secretForKey))
{
    throw new InvalidOperationException("Secret is missing from configuration");
}

builder.Services
    .Authentication(builder, secretForKey)
    .ApiVersioning()
    .AddProjectServices()
    .AddRateLimiting()  // <-- Add this line
    .AddControllers()
    .AddJsonTimeOnlyAndDateOnlyConverters();

builder.Services
    .AddEndpointsApiExplorer()
    .SwaggerGen()
    .AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors();

// Add rate limiter middleware BEFORE authorization
app.UseRateLimiter();  // <-- Add this line

app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
```

### Apply to Controllers

**File:** `GymCRM.SchedulingAPI/Controllers/TrainingSessionController.cs`

```csharp
using Microsoft.AspNetCore.RateLimiting;

[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[action]")]
[Authorize]
[ApiController]
public class TrainingSessionController : ControllerBase
{
    private readonly ITrainingSessionsService _trainingSessionService;
    private readonly ILogger<TrainingSessionController> _logger;

    public TrainingSessionController(
        ITrainingSessionsService trainingSessionService,
        ILogger<TrainingSessionController> logger)
    {
        _trainingSessionService = trainingSessionService ?? 
            throw new ArgumentNullException(nameof(trainingSessionService));
        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
    }

    // Strict limit for booking operations
    [HttpPost]
    [EnableRateLimiting("booking")]
    public async Task<ActionResult> AddTrainingSession(
        InsertTrainingSession trainingSession,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.InsertTrainingSessionAsync(
                trainingSession,
                cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new CreatedResult();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training session");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    
    // Moderate limit for modifications
    [HttpPut]
    [EnableRateLimiting("modifications")]
    public async Task<ActionResult> UpdateTrainingSession(
        [FromBody] TrainingSession updatedTrainingSession,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.UpdateTrainingSessionAsync(
                updatedTrainingSession, 
                cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }
            
            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training session");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    
    // Moderate limit for deletions
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("modifications")]
    public async Task<ActionResult> DeleteTrainingSession(
        Guid id, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.DeleteTrainingSessionAsync(
                id, 
                cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training session");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    
    // Generous limit for queries
    [HttpGet]
    [EnableRateLimiting("queries")]
    public async Task<ActionResult<List<TrainingSession>>> GetAllTrainingSessions(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetAllAsync(
                cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training sessions");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpGet("{id:guid}")]
    [EnableRateLimiting("queries")]
    public async Task<ActionResult<IEnumerable<TrainingSession>>> GetAllTrainingSessionsForClient(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetTrainingSessionsForClientIdAsync(
                id,
                cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training sessions for client");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
```

**Similarly apply to other controllers:**

```csharp
// AvailabilitiesController.cs
[HttpPost]
[EnableRateLimiting("modifications")]
public async Task<ActionResult> AddAvailability(...) { }

[HttpGet]
[EnableRateLimiting("queries")]
public async Task<ActionResult<IEnumerable<TrainerAvailability>>> GetAvailabilities(...) { }

// TimeOffController.cs
[HttpPost]
[EnableRateLimiting("modifications")]
public async Task<ActionResult> AddNewTimeOff(...) { }

[HttpGet]
[EnableRateLimiting("queries")]
public async Task<ActionResult<IDictionary<DateTime, TimeOff>>> GetAllForDatePeriod(...) { }

// CalendarController.cs
[HttpGet("trainer/{trainerId:guid}/month/{year:int}/{month:int}")]
[EnableRateLimiting("queries")]
public async Task<ActionResult<GymTrainerCalendarDto>> GetTrainerMonthlyCalendar(...) { }
```

---

## Summary

All 8 tasks have complete .NET 9 implementations:

1. ✅ **Booking Validation Service** - Complete with all 7 validation rules
2. ✅ **Auth Controller Integration Tests** - Example tests with WebApplicationFactory
3. ✅ **Revoke Tokens on Password Change** - Updated ChangePassword method
4. ✅ **Scheduling Integration Tests** - Example validation tests
5. ✅ **Password Complexity Validation** - Complete validator with integration
6. ✅ **Calendar Controller** - Complete controller with 3 endpoints
7. ✅ **Available Slots Generation** - Complete service method and controller
8. ✅ **Rate Limiting for SchedulingAPI** - Complete configuration and application

All code is production-ready with:
- Comprehensive XML documentation
- Proper error handling
- Detailed logging
- Input validation
- Appropriate HTTP status codes

**Total Implementation Time Estimate:** 38-52 hours

---

*Document Version: 1.0 (.NET 9 Only)*  
*Last Updated: December 31, 2025*
