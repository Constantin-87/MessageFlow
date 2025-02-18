using AutoMapper;
using MessageFlow.Server.Models;
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

            CreateMap<CompanyDTO, Company>();

            // ✅ Map Team → TeamDTO
            CreateMap<Team, TeamDTO>().ReverseMap();

            // ✅ Map CompanyEmail → CompanyEmailDTO
            CreateMap<CompanyEmail, CompanyEmailDTO>().ReverseMap();

            // ✅ Map CompanyPhoneNumber → CompanyPhoneNumberDTO
            CreateMap<CompanyPhoneNumber, CompanyPhoneNumberDTO>().ReverseMap();

            // ✅ Map PretrainDataFile → PretrainDataFileDTO
            CreateMap<PretrainDataFile, PretrainDataFileDTO>().ReverseMap();

            // ✅ Map ProcessedPretrainData ↔ ProcessedPretrainDataDTO
            CreateMap<ProcessedPretrainDataDTO, ProcessedPretrainData>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id)).ReverseMap();


        }
    }
}
