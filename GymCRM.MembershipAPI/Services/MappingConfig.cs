using AutoMapper;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Models.DTOs;
using Member = GymCRM.MembershipAPI.Models.DTOs.Member;

namespace GymCRM.MembershipAPI.Services
{
	public class MappingConfig
	{
		public static MapperConfiguration RegisterMaps()
		{
			var mappingConfig = new MapperConfiguration(config =>
			{
				config.CreateMap<Member, Infrastructure.Entities.Member>();
				config.CreateMap<Infrastructure.Entities.Member, Member>();
			});

			return mappingConfig;
		}
	}
}
