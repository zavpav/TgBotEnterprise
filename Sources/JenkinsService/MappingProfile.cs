using AutoMapper;
using CommonInfrastructure;
using JenkinsService.Database;
using RabbitMessageCommunication.BuildService;
using RabbitMessageCommunication.MainBot;

namespace JenkinsService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MainBotUpdateUserInfo, DbeUserInfo>(MemberList.None)
                .ForMember(x => x.BotUserId, s => s.MapFrom(x => x.BotUserId))
                .ForMember(x => x.JenkinsName, s => s.MapFrom(x => x.JenkinsUserName))
                .ForMember(x => x.WhoIsThis, s => s.MapFrom(x => x.WhoIsThis));

            CreateMap<DbeJenkinsJob, BuildInfo>(MemberList.None)
                .ForMember(x => x.JobName, s => s.MapFrom(x => x.JenkinsJobName))
                .ForMember(x => x.ProjectSysName, s => s.MapFrom(x => x.ProjectSysName))
                .ForMember(x => x.BuildSubType, s => s.MapFrom(x => x.BuildSubType))
                .ForMember(x => x.BuildStatus, s => s.MapFrom(x => x.BuildStatus))
                .ForMember(x => x.ExecuterInfo, s => s.MapFrom(x => x.JenkinsBuildStarter))
                .ForMember(x => x.IsProgressing, s => s.MapFrom(x => x.BuildIsProcessing))
                .ForMember(x => x.JobName, s => s.MapFrom(x => x.JenkinsJobName))
                .ForMember(x => x.BuildNumber, s => s.MapFrom(x => x.BuildNumber))
                .ForMember(x => x.BuildName, s => s.MapFrom(x => x.BuildName))
                .ForMember(x => x.BuildDescription, s => s.MapFrom(x => x.BuildDescription))
                .ForMember(x => x.BuildDuration, s => s.MapFrom(x => x.BuildDuration));
            CreateMap<DbeJenkinsJob.ChangeInfo, BuildInfo.ChangeInfo>();
        }

}
}