using AutoMapper;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services
{
	public class MappingConfig
	{
		public static MapperConfiguration RegisterMaps()
		{
			var mappingConfig = new MapperConfiguration(config =>
			{
				config.CreateMap<UserDto, User>();
				config.CreateMap<User, UserDto>();
			});

			return mappingConfig;
		}
	}
}
