using AutoMapper;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.WebAdmin;
using WebAdminService.Data;

namespace WebAdminService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RabbitMessageCommunication.WebAdmin.ResponseAllUsersMessage.UserInfo, CurrentUsersService.UserDataPresentor>(MemberList.None)
                .ForMember(x => x.OriginalBotUserId, s => s.MapFrom(x => x.BotUserId));
            CreateMap<CurrentUsersService.UserDataPresentor, RabbitMessageCommunication.WebAdmin.WebAdminUpdateUserInfo>();

            CreateMap<MainBotProjectInfo, ProjectSettingService.ProjectSettingsMainInfoPresentor>();
            CreateMap<ProjectSettingService.ProjectSettingsMainInfoPresentor, MainBotProjectInfo>();
            CreateMap<WebAdminResponseProjectSettingsMessage.SettingsItem, WebAdminUpdateProjectSettings.SettingsItem>();

        }
    }
}