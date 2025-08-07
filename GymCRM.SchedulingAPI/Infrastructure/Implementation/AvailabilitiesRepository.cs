using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class AvailabilitiesRepository : GenericRepository<Availability>, IAvailabilitiesRepository
{
    public AvailabilitiesRepository(SchedulingDbContext context) : base(context) {}
}