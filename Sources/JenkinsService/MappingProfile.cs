using AutoMapper;
using JenkinsService.Database;
using RabbitMessageCommunication.MainBot;

namespace JenkinsService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MainBotUpdateUserInfo, DtoUserInfo>(MemberList.None)
                .ForMember(x => x.BotUserId, s => s.MapFrom(x => x.BotUserId))
                .ForMember(x => x.JenkinsName, s => s.MapFrom(x => x.JenkinsUserName))
                .ForMember(x => x.WhoIsThis, s => s.MapFrom(x => x.WhoIsThis));
        }

    }
}