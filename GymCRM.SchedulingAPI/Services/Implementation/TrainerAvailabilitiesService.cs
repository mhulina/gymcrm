using AutoMapper;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class TrainerAvailabilitiesService : ITrainerAvailabilitiesService
{
    private readonly ITrainerAvailabilitiesRepository _trainerAvailabilitiesRepository;
    private readonly IHolidayService _holidayService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TrainerAvailabilitiesService> _logger;

    public TrainerAvailabilitiesService(
        ITrainerAvailabilitiesRepository trainerAvailabilitiesRepository,
        IHolidayService holidayService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TrainerAvailabilitiesService> logger)
    {
        _trainerAvailabilitiesRepository = trainerAvailabilitiesRepository;
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

        var holidays = await _holidayService.FetchAllHolidays(cancellationToken: cancellationToken);

        foreach (var holiday in holidays)
        {
            if (insertAvailability.StartDate == holiday.Date)
            {
                insertAvailability.StartDate = insertAvailability.StartDate.AddDays(1);
            }

            if (insertAvailability.EndDate == holiday.Date)
            {
                insertAvailability.EndDate = insertAvailability.EndDate.AddDays(-1);
            }

            if (insertAvailability.StartDate > insertAvailability.EndDate)
            {
                _logger.LogError($"Start date, {insertAvailability.StartDate} is greater than end date, {insertAvailability.EndDate}");
                return false;
            }

            if (insertAvailability.StartDate != insertAvailability.EndDate)
            {
                continue;
            }
            
            _logger.LogInformation($"Insert availability failed for trainer, ID: {insertAvailability.TrainerId}." +
                $"Start (Date: {insertAvailability.StartDate}) and end (Date: {insertAvailability.EndDate}) dates are " +
                $"the same and they are a holiday.");
                
            return false;
        }
        
        var availability = new Models.Entities.TrainerAvailability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = insertAvailability.TrainerId,
            DayOfWeek = insertAvailability.DayOfWeek,
            StartDateUtc = insertAvailability.StartDate,
            EndDateUtc = insertAvailability.EndDate,
            IsAvailable = insertAvailability.IsAvailable,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        
        _trainerAvailabilitiesRepository.Add(availability);
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