﻿using AutoMapper;
using RabbitMessageCommunication.MainBot;
using RedmineService.Database;

namespace RedmineService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MainBotUpdateUserInfo, DtoUserInfo>(MemberList.None)
                .ForMember(x => x.BotUserId, s => s.MapFrom(x => x.BotUserId))
                .ForMember(x => x.RedmineName, s => s.MapFrom(x => x.RedmineUserName))
                .ForMember(x => x.WhoIsThis, s => s.MapFrom(x => x.WhoIsThis));
        }

    }
}