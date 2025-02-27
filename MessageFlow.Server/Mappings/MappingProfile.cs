using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ✅ Map Company → CompanyDTO with nested collections
            CreateMap<Company, CompanyDTO>()
                .ForMember(dest => dest.CompanyEmails, opt => opt.MapFrom(src => src.CompanyEmails))
                .ForMember(dest => dest.CompanyPhoneNumbers, opt => opt.MapFrom(src => src.CompanyPhoneNumbers))
                .ForMember(dest => dest.Teams, opt => opt.MapFrom(src => src.Teams));

            // ✅ Company ↔ CompanyDTO
            CreateMap<Company, CompanyDTO>()
                .ForMember(dest => dest.CompanyEmails, opt => opt.MapFrom(src => src.CompanyEmails))
                .ForMember(dest => dest.CompanyPhoneNumbers, opt => opt.MapFrom(src => src.CompanyPhoneNumbers))
                .ForMember(dest => dest.Teams, opt => opt.MapFrom(src => src.Teams))
                .ReverseMap();




            // ✅ Map Team → TeamDTO
            CreateMap<Team, TeamDTO>()
                .ForMember(dest => dest.UsersDTO, opt => opt.MapFrom(src => src.Users))
                .ReverseMap();




            // ✅ Map CompanyEmail ↔ CompanyEmailDTO (ensure CompanyId is mapped)
            CreateMap<CompanyEmail, CompanyEmailDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ReverseMap();

            // ✅ Map CompanyPhoneNumber ↔ CompanyPhoneNumberDTO (ensure CompanyId is mapped)
            CreateMap<CompanyPhoneNumber, CompanyPhoneNumberDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ReverseMap();

            // 🚀 ✅ NEW: ApplicationUser ↔ ApplicationUserDTO (with nested UserTeams)
            CreateMap<ApplicationUser, ApplicationUserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => src.LockoutEnabled))
                .ForMember(dest => dest.CompanyDTO, opt => opt.MapFrom(src => src.Company))
                .ForMember(dest => dest.TeamsDTO, opt => opt.MapFrom(src => src.Teams))
                .ForMember(dest => dest.Role, opt => opt.Ignore()) // Role will be set manually after mapping
                .ReverseMap();

            // ✅ Add this to your MappingProfile constructor
            CreateMap<FacebookSettingsDTO, FacebookSettingsModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.PageId, opt => opt.MapFrom(src => src.PageId))
                .ForMember(dest => dest.AccessToken, opt => opt.MapFrom(src => src.AccessToken))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ReverseMap(); // Optional: Add reverse mapping if needed

            // ✅ Mapping between PhoneNumberInfoDTO and PhoneNumberInfo
            CreateMap<PhoneNumberInfoDTO, PhoneNumberInfo>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.PhoneNumberId, opt => opt.MapFrom(src => src.PhoneNumberId))
                .ForMember(dest => dest.PhoneNumberDesc, opt => opt.MapFrom(src => src.PhoneNumberDesc))
                .ForMember(dest => dest.WhatsAppSettingsModelId, opt => opt.MapFrom(src => src.WhatsAppSettingsModelId))
                .ReverseMap(); // Enables reverse mapping

            // ✅ Mapping between WhatsAppSettingsDTO and WhatsAppSettingsModel
            CreateMap<WhatsAppSettingsDTO, WhatsAppSettingsModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ForMember(dest => dest.AccessToken, opt => opt.MapFrom(src => src.AccessToken))
                .ForMember(dest => dest.BusinessAccountId, opt => opt.MapFrom(src => src.BusinessAccountId))
                .ForMember(dest => dest.PhoneNumbers, opt => opt.MapFrom(src => src.PhoneNumbers)) // Nested mapping
                .ReverseMap(); // Enables reverse mapping



            // ✅ Map PretrainDataFile → PretrainDataFileDTO
            CreateMap<PretrainDataFile, PretrainDataFileDTO>().ReverseMap();

            // ✅ Map ProcessedPretrainData ↔ ProcessedPretrainDataDTO
            CreateMap<ProcessedPretrainDataDTO, ProcessedPretrainData>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id)).ReverseMap();

            // ✅ Map Conversation ↔ ConversationDTO (including nested messages)
            CreateMap<Conversation, ConversationDTO>()
                .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages))
                .ReverseMap();

            // ✅ Map Message ↔ MessageDTO
            CreateMap<Message, MessageDTO>()
                .ForMember(dest => dest.Conversation, opt => opt.Ignore()) // Prevent circular references
                .ReverseMap();

        }
    }
}
