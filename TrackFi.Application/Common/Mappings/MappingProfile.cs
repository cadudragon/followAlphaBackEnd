using AutoMapper;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Entities;

namespace TrackFi.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for mapping between Domain entities and DTOs.
/// Enums are converted to strings for API responses.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.PrimaryWalletNetwork, opt => opt.MapFrom(src => src.PrimaryWalletNetwork.ToString()))
            .ForMember(dest => dest.CoverNftNetwork, opt => opt.MapFrom(src => src.CoverNftNetwork.HasValue ? src.CoverNftNetwork.Value.ToString() : null));

        // UserWallet mappings
        CreateMap<UserWallet, UserWalletDto>()
            .ForMember(dest => dest.Network, opt => opt.MapFrom(src => src.Network.ToString()));

        // WatchlistEntry mappings
        CreateMap<WatchlistEntry, WatchlistEntryDto>()
            .ForMember(dest => dest.Network, opt => opt.MapFrom(src => src.Network.ToString()));
    }
}
