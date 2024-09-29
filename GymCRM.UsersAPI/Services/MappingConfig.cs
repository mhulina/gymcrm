using AutoMapper;
using GymCRM.UsersAPI.Infrastructure.Entities;
using GymCRM.UsersAPI.Models.DTOs;

namespace GymCRM.UsersAPI.Services
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
