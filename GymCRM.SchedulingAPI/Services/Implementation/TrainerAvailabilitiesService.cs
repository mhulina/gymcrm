using AutoMapper;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class TrainerAvailabilitiesService : ITrainerAvailabilitiesService
{
    private readonly ITrainerWorkingHoursRepository _trainerWorkingHoursRepository;
    private readonly ITrainerAvailabilitiesRepository _trainerAvailabilitiesRepository;
    private readonly ITrainerDailyAvailabilitiesRepository _trainerDailyAvailabilitiesRepository;
    private readonly IHolidayService _holidayService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TrainerAvailabilitiesService> _logger;

    public TrainerAvailabilitiesService(
        ITrainerWorkingHoursRepository trainerWorkingHoursRepository,
        ITrainerAvailabilitiesRepository trainerAvailabilitiesRepository,
        ITrainerDailyAvailabilitiesRepository trainerDailyAvailabilitiesRepository,
        IHolidayService holidayService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TrainerAvailabilitiesService> logger)
    {
        _trainerWorkingHoursRepository = trainerWorkingHoursRepository;
        _trainerAvailabilitiesRepository = trainerAvailabilitiesRepository;
        _trainerDailyAvailabilitiesRepository = trainerDailyAvailabilitiesRepository;
        _holidayService = holidayService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<IEnumerable<TrainerAvailability>> GetAvailabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _trainerAvailabilitiesRepository.FetchAllAsync(cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<IEnumerable<TrainerAvailability>>(result);
        
        return mappedResult;
    }

    public async Task<IEnumerable<TrainerAvailability>> GetAvailabilitiesForTrainerIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var result = await _trainerAvailabilitiesRepository.FetchByConditionAsync(
            x => x.TrainerId == id,
            cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<IEnumerable<TrainerAvailability>>(result);
        
        return mappedResult;
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

        var dailyAvailabilities = new List<Models.Entities.TrainerDailyAvailability>();
        var dailyWorkingHours = new List<Models.Entities.TrainerWorkingHours>();
        
        foreach (var insertAvailabilityDailyAvailability in insertAvailability.DailyAvailabilities)
        {
            var dailyAvailability = new Models.Entities.TrainerDailyAvailability
            {
                Id = Guid.CreateVersion7(),
                AvailabilityId = availability.Id,
                DayOfWeek = insertAvailabilityDailyAvailability.DayOfWeek,
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow
            };

            foreach (var insertAvailabilityDailyWorkingHours in insertAvailabilityDailyAvailability.WorkingHours)
            {
                var dailyWorkingHour = new Models.Entities.TrainerWorkingHours
                {
                    Id = Guid.CreateVersion7(),
                    DailyAvailabilityId = dailyAvailability.Id,
                    StartTime = insertAvailabilityDailyWorkingHours.StartTime,
                    EndTime = insertAvailabilityDailyWorkingHours.EndTime,
                    DateCreatedUtc = DateTime.UtcNow,
                    DateModifiedUtc = DateTime.UtcNow
                };
                
                dailyWorkingHours.Add(dailyWorkingHour);
            }
            
            dailyAvailabilities.Add(dailyAvailability);
        }
        
        _trainerAvailabilitiesRepository.Add(availability);
        _trainerDailyAvailabilitiesRepository.AddRange(dailyAvailabilities);
        _trainerWorkingHoursRepository.AddRange(dailyWorkingHours);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
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
}