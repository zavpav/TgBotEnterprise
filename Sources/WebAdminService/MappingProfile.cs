using AutoMapper;

namespace WebAdminService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RabbitMessageCommunication.WebAdmin.ResponseAllUsersMessage.UserInfo, WebAdminService.Data.CurrentUsersService.UserDataPresentor>(MemberList.None);
        }
    }
}