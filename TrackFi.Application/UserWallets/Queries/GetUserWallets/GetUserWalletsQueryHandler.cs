using AutoMapper;
using MediatR;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.UserWallets.Queries.GetUserWallets;

public class GetUserWalletsQueryHandler : IRequestHandler<GetUserWalletsQuery, List<UserWalletDto>>
{
    private readonly IUserWalletRepository _walletRepository;
    private readonly IMapper _mapper;

    public GetUserWalletsQueryHandler(IUserWalletRepository walletRepository, IMapper mapper)
    {
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<List<UserWalletDto>> Handle(GetUserWalletsQuery request, CancellationToken cancellationToken)
    {
        var wallets = await _walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return _mapper.Map<List<UserWalletDto>>(wallets);
    }
}
