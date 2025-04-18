using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Infrastructure.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ApplicationUser <> ApplicationUserDTO
            CreateMap<ApplicationUser, ApplicationUserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => src.LockoutEnabled))
                .ForMember(dest => dest.CompanyDTO, opt => opt.MapFrom(src => src.Company))
                .ForMember(dest => dest.TeamsDTO, opt => opt.MapFrom(src => src.Teams))
                .ForMember(dest => dest.Role, opt => opt.Ignore()) // Role will be set manually after mapping
                .ReverseMap()
                .ForMember(dest => dest.Company, opt => opt.Ignore());

            // Company <> CompanyDTO
            CreateMap<Company, CompanyDTO>()
                .ForMember(dest => dest.CompanyEmails, opt => opt.MapFrom(src => src.CompanyEmails))
                .ForMember(dest => dest.CompanyPhoneNumbers, opt => opt.MapFrom(src => src.CompanyPhoneNumbers))
                .ForMember(dest => dest.Teams, opt => opt.MapFrom(src => src.Teams))
                .ReverseMap();

            // Map CompanyEmail <> CompanyEmailDTO
            CreateMap<CompanyEmail, CompanyEmailDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ReverseMap();

            // Map CompanyPhoneNumber <> CompanyPhoneNumberDTO
            CreateMap<CompanyPhoneNumber, CompanyPhoneNumberDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ReverseMap();

            // Map PretrainDataFile <> PretrainDataFileDTO
            CreateMap<PretrainDataFile, PretrainDataFileDTO>().ReverseMap();

            // Map ProcessedPretrainData <> ProcessedPretrainDataDTO
            CreateMap<ProcessedPretrainDataDTO, ProcessedPretrainData>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id)).ReverseMap();

            // Map Team <> TeamDTO
            CreateMap<Team, TeamDTO>()
                .ForMember(dest => dest.AssignedUsersDTO, opt => opt.MapFrom(src => src.Users))
                .ReverseMap();

            // Map FacebookSettingsModel <> FacebookSettingsDTO
            CreateMap<FacebookSettingsDTO, FacebookSettingsModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.PageId, opt => opt.MapFrom(src => src.PageId))
                .ForMember(dest => dest.AccessToken, opt => opt.MapFrom(src => src.AccessToken))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ReverseMap();

            // Map WhatsAppSettingsDTO <> WhatsAppSettingsModel
            CreateMap<WhatsAppSettingsDTO, WhatsAppSettingsModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ForMember(dest => dest.AccessToken, opt => opt.MapFrom(src => src.AccessToken))
                .ForMember(dest => dest.BusinessAccountId, opt => opt.MapFrom(src => src.BusinessAccountId))
                .ForMember(dest => dest.PhoneNumbers, opt => opt.MapFrom(src => src.PhoneNumbers))
                .ReverseMap();

            // Map PhoneNumberInfoDTO <> PhoneNumberInfo
            CreateMap<PhoneNumberInfoDTO, PhoneNumberInfo>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.PhoneNumberId, opt => opt.MapFrom(src => src.PhoneNumberId))
                .ForMember(dest => dest.PhoneNumberDesc, opt => opt.MapFrom(src => src.PhoneNumberDesc))
                .ForMember(dest => dest.WhatsAppSettingsModelId, opt => opt.MapFrom(src => src.WhatsAppSettingsId))
                .ReverseMap();
                        
            // Map Conversation <> ConversationDTO
            CreateMap<Conversation, ConversationDTO>()
                .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages))
                .ReverseMap();

            // Map Message <> MessageDTO
            CreateMap<Message, MessageDTO>()
                .ForMember(dest => dest.Conversation, opt => opt.Ignore())
                .ReverseMap();

        }
    }
}
