using AutoMapper;
using MainBotService.Database;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.WebAdmin;

namespace MainBotService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<DtoUserInfo, MainBotUpdateUserInfo>(MemberList.Destination);
            CreateMap<TelegramPublishNewUserFromTelegram, DtoUserInfo>(MemberList.None)
                .ForMember(x => x.BotUserId, s => s.MapFrom(x => x.BotUserId))
                .ForMember(x => x.WhoIsThis, s => s.MapFrom(x => x.WhoIsThis))
                .ForMember(x => x.IsActive, s => s.MapFrom(x => x.IsActive));
            
            CreateMap<DtoUserInfo, ResponseAllUsersMessage.UserInfo>(MemberList.Destination)
                .ForMember(x => x.UserId, s => s.MapFrom(x => x.Id));

            CreateMap<WebAdminUpdateUserInfo, DtoUserInfo>();

        }
    }
}