﻿using AutoMapper;
using RabbitMessageCommunication.MainBot;
using TelegramService.Database;

namespace TelegramService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MainBotUpdateUserInfo, DbeUserInfo>(MemberList.None)
                .ForMember(x => x.BotUserId, s => s.MapFrom(x => x.BotUserId))
                .ForMember(x => x.WhoIsThis, s => s.MapFrom(x => x.WhoIsThis));
        }

    }
}