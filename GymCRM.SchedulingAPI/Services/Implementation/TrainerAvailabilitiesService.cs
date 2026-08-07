using AutoMapper;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;
using ILogger = Serilog.ILogger;
using TrainerDailyAvailability = GymCRM.SchedulingAPI.Models.Entities.TrainerDailyAvailability;
using TrainerWorkingHours = GymCRM.SchedulingAPI.Models.Entities.TrainerWorkingHours;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class TrainerAvailabilitiesService : ITrainerAvailabilitiesService
{
    private readonly ITrainerWorkingHoursRepository _trainerWorkingHoursRepository;
    private readonly ITrainerAvailabilitiesRepository _trainerAvailabilitiesRepository;
    private readonly ITrainerDailyAvailabilitiesRepository _trainerDailyAvailabilitiesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;

    public TrainerAvailabilitiesService(
        ITrainerWorkingHoursRepository trainerWorkingHoursRepository,
        ITrainerAvailabilitiesRepository trainerAvailabilitiesRepository,
        ITrainerDailyAvailabilitiesRepository trainerDailyAvailabilitiesRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger)
    {
        _trainerWorkingHoursRepository = trainerWorkingHoursRepository;
        _trainerAvailabilitiesRepository = trainerAvailabilitiesRepository;
        _trainerDailyAvailabilitiesRepository = trainerDailyAvailabilitiesRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<IEnumerable<TrainerAvailability>> GetAvailabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _trainerAvailabilitiesRepository.FetchAllAsync(cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<List<TrainerAvailability>>(result);

        return await PopulateDailyAvailabilities(mappedResult, cancellationToken);
    }

    public async Task<IEnumerable<TrainerAvailability>> GetAvailabilitiesForTrainerIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _trainerAvailabilitiesRepository.FetchByConditionAsync(
            x => x.TrainerId == id,
            cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<List<TrainerAvailability>>(result);

        return await PopulateDailyAvailabilities(mappedResult, cancellationToken);
    }

    /// <summary>
    /// Fills in <see cref="TrainerAvailability.DailyAvailabilities"/> (and each day's
    /// <see cref="Models.DTOs.TrainerDailyAvailability.WorkingHours"/>) for the given
    /// availabilities. The entity <see cref="Models.Entities.TrainerAvailability"/> has no
    /// navigation property for its days, so AutoMapper's direct entity-to-DTO mapping
    /// always leaves these nested collections null - this backfills them with two batched
    /// queries (no N+1) instead.
    /// </summary>
    private async Task<List<TrainerAvailability>> PopulateDailyAvailabilities(
        List<TrainerAvailability> availabilities,
        CancellationToken cancellationToken)
    {
        if (availabilities.Count == 0)
        {
            return availabilities;
        }

        var availabilityIds = availabilities.Select(a => a.Id).ToList();

        var dailyAvailabilityEntities = (await _trainerDailyAvailabilitiesRepository
            .FetchByConditionAsync(x => availabilityIds.Contains(x.AvailabilityId), cancellationToken))
            .ToList();
        var dailyAvailabilityIds = dailyAvailabilityEntities.Select(d => d.Id).ToList();

        var workingHourEntities = (await _trainerWorkingHoursRepository
            .FetchByConditionAsync(x => dailyAvailabilityIds.Contains(x.DailyAvailabilityId), cancellationToken))
            .ToList();

        foreach (var availability in availabilities)
        {
            availability.DailyAvailabilities = dailyAvailabilityEntities
                .Where(d => d.AvailabilityId == availability.Id)
                .Select(dailyEntity =>
                {
                    var mappedDaily = _mapper.Map<Models.DTOs.TrainerDailyAvailability>(dailyEntity);
                    mappedDaily.WorkingHours = _mapper.Map<List<Models.DTOs.TrainerWorkingHours>>(
                        workingHourEntities.Where(w => w.DailyAvailabilityId == dailyEntity.Id).ToList());
                    return mappedDaily;
                })
                .ToList();
        }

        return availabilities;
    }

    public async Task<bool> IsTrainerWorkingOnDateAsync(
        Guid trainerId, 
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (trainerId == Guid.Empty)
        {
            _logger.Error("Invalid trainer ID: {TrainerId}", trainerId);
            throw new ArgumentException($"{trainerId} is an invalid value for trainer ID", nameof(trainerId));
        }
        
        if (startTime == DateTime.MinValue
            || startTime == DateTime.MaxValue
            || endTime == DateTime.MinValue
            || endTime == DateTime.MaxValue)
        {
            _logger.Error("Start or end time invalid: \nStart time: {StartTime}\nEnd time: {EndTime}", startTime, endTime);
            throw new ArgumentException($"Start or end times are invalid");
        }

        var dayOfWeekForDate = startTime.DayOfWeek;
        var trainerAvailabilities = (await _trainerAvailabilitiesRepository
            .FetchByConditionAsync(x => x.TrainerId == trainerId, cancellationToken))
            .ToList();

        if (trainerAvailabilities.Count == 0)
        {
            return false;
        }

        var dailyAvailabilitiesForTrainer = (await _trainerDailyAvailabilitiesRepository
            .FetchByConditionAsync(
                x => trainerAvailabilities.Select(y => y.Id).Contains(x.AvailabilityId),
                cancellationToken))
            .ToList();

        if (dailyAvailabilitiesForTrainer.Count == 0)
        {
            return false;
        }

        var daysAvailable = dailyAvailabilitiesForTrainer
            .Where(x => !x.IsDayOff
                && x.DayOfWeek == dayOfWeekForDate.ToString())
            .ToList();

        if (daysAvailable.Count == 0)
        {
            return false;
        }
        
        var workingHoursOnAvailableDays = (await _trainerWorkingHoursRepository
            .FetchByConditionAsync(
                x => daysAvailable.Select(y => y.Id).Contains(x.DailyAvailabilityId),
                cancellationToken))
            .ToList();

        if (workingHoursOnAvailableDays.Count == 0)
        {
            return false;
        }

        var trainerAvailableInRequestedTime = workingHoursOnAvailableDays
            .Any(x => x.StartTime <= TimeOnly.FromDateTime(startTime)
                && x.EndTime >= TimeOnly.FromDateTime(endTime));

        return trainerAvailableInRequestedTime;
    }

    public async Task<bool> AddAvailabilityAsync(
        InsertAvailability insertAvailability,
        CancellationToken cancellationToken = default)
    {
        if (insertAvailability is null)
        {
            throw new ArgumentNullException(nameof(insertAvailability));
        }
        
        var availability = new Models.Entities.TrainerAvailability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = insertAvailability.TrainerId,
            WorkingWeekends = insertAvailability.WorkingWeekends,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };

        var (dailyAvailabilities, dailyWorkingHours) = CreateTrainerDailyAvailabilitiesAndWorkingHours(
            insertAvailability, 
            availability);

        if (dailyAvailabilities is null
            || dailyAvailabilities.Count < 1)
        {
            return false;
        }

        _trainerAvailabilitiesRepository.Add(availability);
        _trainerDailyAvailabilitiesRepository.AddRange(dailyAvailabilities);
        _trainerWorkingHoursRepository.AddRange(dailyWorkingHours);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<bool> AddWorkingHoursToDailyAvailability(
        Guid trainerId, 
        string nameOfDay,
        List<InsertWorkingHours> newWorkingHours,
        CancellationToken cancellationToken = default)
    {
        if (trainerId.Equals(Guid.Empty))
        {
            _logger.Error($"Invalid trainer ID: {trainerId}");
            throw new ArgumentException($"{trainerId} is an invalid ID", nameof(trainerId));
        }

        if (!Enum.GetNames<DayOfWeek>().Contains(nameOfDay))
        {
            _logger.Error($"Invalid day of week: {nameOfDay}");
            throw new InvalidOperationException($"{nameOfDay} is not a valid day of the week");
        }

        if (newWorkingHours is null
            || newWorkingHours.Count < 1)
        {
            _logger.Information($"No working hours have been added");
            return true;
        }
        
        var trainerAvailability = (await _trainerAvailabilitiesRepository
            .FetchByConditionAsync(x => x.TrainerId == trainerId, cancellationToken))
            .FirstOrDefault();

        if (trainerAvailability is null)
        {
            _logger.Warning($"Trainer, ID:{trainerId}, doesn't have any availabilities created");
            return false;
        }

        var trainerDailyAvailability = (await _trainerDailyAvailabilitiesRepository
            .FetchByConditionAsync(
                x => x.AvailabilityId == trainerAvailability.Id 
                     && x.DayOfWeek == nameOfDay,
                cancellationToken))
            .FirstOrDefault();
        var dailyAvailabilityId = trainerDailyAvailability?.Id ?? Guid.Empty;

        if (dailyAvailabilityId != Guid.Empty)
        {
            var result = await InsertWorkingHoursForDay(dailyAvailabilityId, newWorkingHours, cancellationToken);
            return result;
        }
        
        (dailyAvailabilityId, var newDailyAvailabilityInserted) = await InsertDailyAvailabilityForDay(
            trainerId, 
            nameOfDay, 
            trainerAvailability.Id,
            cancellationToken);
            
        if (!newDailyAvailabilityInserted)
        {
            return false;
        };

        return await InsertWorkingHoursForDay(dailyAvailabilityId, newWorkingHours, cancellationToken);
    }

    public async Task<bool> DeleteAvailabilityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(nameof(id));
        }

        var availability = new Models.Entities.TrainerAvailability
        {
            Id = id
        };
        
        _trainerAvailabilitiesRepository.Remove(availability);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return result;
    }

    public async Task<bool> UpdateAvailabilityAsync(
        TrainerAvailability trainerAvailability,
        CancellationToken cancellationToken = default)
    {
        if (trainerAvailability is null)
        {
            throw new ArgumentNullException(nameof(trainerAvailability));
        }
        
        var mappedAvailability = _mapper.Map<Models.Entities.TrainerAvailability>(trainerAvailability);
        
        _trainerAvailabilitiesRepository.Update(mappedAvailability);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return result;
    }

    private async Task<bool> InsertWorkingHoursForDay(
        Guid dailyAvailabilityId,
        List<InsertWorkingHours> newWorkingHours, 
        CancellationToken cancellationToken)
    {
        var workingHours = newWorkingHours
            .Select(x => new TrainerWorkingHours
            {
                Id = Guid.CreateVersion7(),
                DailyAvailabilityId = dailyAvailabilityId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow
            })
            .ToList();

        try
        {
            _trainerWorkingHoursRepository.AddRange(workingHours);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw;
        }
    }

    private async Task<(Guid dailyAvailabilityId, bool addWorkingHoursToDailyAvailability)> InsertDailyAvailabilityForDay(
        Guid trainerId, 
        string nameOfDay, 
        Guid trainerAvailabilityId,
        CancellationToken cancellationToken)
    {
        _logger.Information($"Trainer, ID:{trainerId}, has no daily availability created on the day {nameOfDay}. Creating daily availability");

        var dailyAvailabilityId = Guid.CreateVersion7();
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = dailyAvailabilityId,
            AvailabilityId = trainerAvailabilityId,
            DayOfWeek = nameOfDay,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
            
        _trainerDailyAvailabilitiesRepository.Add(dailyAvailability);

        try
        {
            var trainerDailyAvailabilitySaved = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!trainerDailyAvailabilitySaved)
            {
                _logger.Error($"Could not create daily availability for Trainer ID: {trainerId}. Working hours have not been created");
                return (dailyAvailabilityId, false);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw;
        }

        return (dailyAvailabilityId, true);
    }

    private (List<TrainerDailyAvailability> dailyAvailabilities, List<TrainerWorkingHours> dailyWorkingHours) 
        CreateTrainerDailyAvailabilitiesAndWorkingHours(InsertAvailability insertAvailability, Models.Entities.TrainerAvailability availability)
    {
        var dailyAvailabilities = new List<TrainerDailyAvailability>();
        var dailyWorkingHours = new List<TrainerWorkingHours>();
        
        foreach (var insertAvailabilityDailyAvailability in insertAvailability.DailyAvailabilities)
        {
            var validDailyAvailability = Enum.GetNames(typeof(DayOfWeek)).Contains(insertAvailabilityDailyAvailability.DayOfWeek);

            if (!validDailyAvailability)
            {
                continue;
            }
            
            var dailyAvailability = new TrainerDailyAvailability
            {
                Id = Guid.CreateVersion7(),
                AvailabilityId = availability.Id,
                DayOfWeek = insertAvailabilityDailyAvailability.DayOfWeek,
                IsDayOff = insertAvailabilityDailyAvailability.IsDayOff,
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow
            };

            dailyWorkingHours.AddRange(insertAvailabilityDailyAvailability.WorkingHours
                .Select(insertAvailabilityDailyWorkingHours => new TrainerWorkingHours
                {
                    Id = Guid.CreateVersion7(),
                    DailyAvailabilityId = dailyAvailability.Id,
                    StartTime = insertAvailabilityDailyWorkingHours.StartTime,
                    EndTime = insertAvailabilityDailyWorkingHours.EndTime,
                    DateCreatedUtc = DateTime.UtcNow,
                    DateModifiedUtc = DateTime.UtcNow
                }));

            dailyAvailabilities.Add(dailyAvailability);
        }

        if (dailyAvailabilities.Count < 1)
        {
            _logger.Warning($"No daily availabilities have been added");
        }

        return (dailyAvailabilities, dailyWorkingHours);
    }
}
