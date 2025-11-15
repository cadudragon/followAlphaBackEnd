using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a fungible token (ERC-20, SPL, native tokens).
/// </summary>
public class Token : Asset
{
    public ContractAddress? ContractAddress { get; private set; }
    public TokenStandard Standard { get; private set; }
    public Quantity Balance { get; private set; }
    public bool IsNative { get; private set; }

    private Token(
        BlockchainNetwork network,
        AssetMetadata metadata,
        ContractAddress? contractAddress,
        TokenStandard standard,
        Quantity balance,
        bool isNative)
        : base(AssetType.Token, AssetCategory.Crypto, network, metadata)
    {
        if (!isNative && contractAddress == null)
            throw new ArgumentException("Contract address is required for non-native tokens", nameof(contractAddress));

        ContractAddress = contractAddress;
        Standard = standard;
        Balance = balance ?? throw new ArgumentNullException(nameof(balance));
        IsNative = isNative;
    }

    public static Token CreateNative(
        BlockchainNetwork network,
        AssetMetadata metadata,
        Quantity balance)
    {
        return new Token(network, metadata, null, TokenStandard.Native, balance, true);
    }

    public static Token CreateErc20(
        BlockchainNetwork network,
        AssetMetadata metadata,
        ContractAddress contractAddress,
        Quantity balance)
    {
        return new Token(network, metadata, contractAddress, TokenStandard.ERC20, balance, false);
    }

    // TODO: Re-enable when adding Solana support (Solana moved to NonEvmBlockchainNetwork enum)
    // public static Token CreateSpl(
    //     AssetMetadata metadata,
    //     ContractAddress contractAddress,
    //     Quantity balance)
    // {
    //     return new Token(NonEvmBlockchainNetwork.Solana, metadata, contractAddress, TokenStandard.SPL, balance, false);
    // }

    public void UpdateBalance(Quantity newBalance)
    {
        Balance = newBalance ?? throw new ArgumentNullException(nameof(newBalance));
    }

    public override Money CalculateValue(Currency currency)
    {
        if (CurrentPrice == null)
            return Money.Zero(currency);

        if (CurrentPrice.Price.Currency != currency)
            throw new InvalidOperationException($"Price currency {CurrentPrice.Price.Currency} does not match requested currency {currency}");

        return CurrentPrice.Price.Multiply(Balance.Value);
    }

    public override string ToString() => $"{Metadata.Symbol}: {Balance.Value} ({Network})";
}
