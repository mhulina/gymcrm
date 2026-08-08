using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class TimeOffRepository :  GenericRepository<TimeOff>, ITimeOffRepository
{
    public TimeOffRepository(SchedulingDbContext context) : base(context) {}
}