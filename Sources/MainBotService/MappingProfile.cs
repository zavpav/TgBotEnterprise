using AutoMapper;
using MainBotService.Database;
using MainBotService.RabbitCommunication.Telegram;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.WebAdmin;

namespace MainBotService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<DbeUserInfo, MainBotUpdateUserInfo>(MemberList.Destination);
            CreateMap<TelegramPublishNewUserFromTelegram, DbeUserInfo>(MemberList.None)
                .ForMember(x => x.BotUserId, s => s.MapFrom(x => x.BotUserId))
                .ForMember(x => x.WhoIsThis, s => s.MapFrom(x => x.WhoIsThis))
                .ForMember(x => x.IsActive, s => s.MapFrom(x => x.IsActive));
            
            CreateMap<DbeUserInfo, ResponseAllUsersMessage.UserInfo>(MemberList.Destination)
                .ForMember(x => x.UserId, s => s.MapFrom(x => x.Id));

            CreateMap<WebAdminUpdateUserInfo, DbeUserInfo>();

            CreateMap<DbeProject, MainBotProjectInfo>();

            CreateMap<TelegramIncomeMessage, OutgoingPreMessageInfo>();
        }
    }
}