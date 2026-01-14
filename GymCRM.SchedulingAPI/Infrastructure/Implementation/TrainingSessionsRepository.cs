using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class TrainingSessionsRepository : GenericRepository<TrainingSession>, ITrainingSessionsRepository
{
    public TrainingSessionsRepository(SchedulingDbContext context) : base(context) {}
}