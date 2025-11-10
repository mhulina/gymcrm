using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class TrainerAvailabilitiesRepository : GenericRepository<TrainerAvailability>, ITrainerAvailabilitiesRepository
{
    public TrainerAvailabilitiesRepository(SchedulingDbContext context) : base(context) {}
}