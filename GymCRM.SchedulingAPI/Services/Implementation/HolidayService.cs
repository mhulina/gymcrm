using AutoMapper;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _holidayRepository;
    private readonly IMapper _mapper;

    public HolidayService(IHolidayRepository holidayRepository, IMapper mapper)
    {
        _holidayRepository = holidayRepository;
        _mapper = mapper;
    }
    
    public async Task<List<Holiday>> FetchHolidaysForMonth(int month, int year, CancellationToken cancellationToken = default)
    {
        if (month < 1 
            || month > 12)
        {
            throw new ArgumentException($"{month} is an invalid month");
        }
        
        var holidaysForMonth = await _holidayRepository.GetByMonthAsync(month, year, cancellationToken);
        var mappedHolidays = _mapper.Map<List<Holiday>>(holidaysForMonth);
        
        return mappedHolidays;
    }

    public async Task<List<Holiday>> FetchAllHolidays(CancellationToken cancellationToken = default)
    {
        var result = await _holidayRepository.GetAllAsync(cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<List<Holiday>>(result);

        return mappedResult;
    }
}