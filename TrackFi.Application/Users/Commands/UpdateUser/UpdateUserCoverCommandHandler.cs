using MediatR;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.Users.Commands.UpdateUser;

/// <summary>
/// Handler for UpdateUserCoverCommand.
/// Updates the user's cover picture or NFT.
/// </summary>
public class UpdateUserCoverCommandHandler : IRequestHandler<UpdateUserCoverCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCoverCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<Unit> Handle(UpdateUserCoverCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not found");
        }

        // Parse NFT network if provided
        BlockchainNetwork? nftNetwork = null;
        if (!string.IsNullOrWhiteSpace(request.CoverNftNetwork))
        {
            if (Enum.TryParse<BlockchainNetwork>(request.CoverNftNetwork, ignoreCase: true, out var parsed))
            {
                nftNetwork = parsed;
            }
        }

        // Update cover
        user.UpdateCoverPicture(
            request.CoverPictureUrl,
            request.CoverNftContract,
            request.CoverNftTokenId,
            nftNetwork);

        // Save
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Unit.Value;
    }
}
