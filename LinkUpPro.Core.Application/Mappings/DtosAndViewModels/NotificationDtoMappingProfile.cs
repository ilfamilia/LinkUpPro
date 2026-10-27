using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Notification;
using LinkUpPro.Core.Application.ViewModels.Notification;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class NotificationDtoMappingProfile : Profile
    {
        public NotificationDtoMappingProfile()
        {
            CreateMap<NotificationDto, NotificationViewModel>();
        }
    }
}
