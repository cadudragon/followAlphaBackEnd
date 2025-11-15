using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a DeFi position (staking, lending, liquidity pool).
/// </summary>
public class DeFiPosition : Asset
{
    public ContractAddress ProtocolAddress { get; private set; }
    public string ProtocolName { get; private set; }
    public DeFiPositionType PositionType { get; private set; }
    public List<Token> UnderlyingTokens { get; private set; }
    public decimal? Apr { get; private set; }

    private DeFiPosition(
        BlockchainNetwork network,
        AssetMetadata metadata,
        ContractAddress protocolAddress,
        string protocolName,
        DeFiPositionType positionType)
        : base(AssetType.DeFiPosition, AssetCategory.Crypto, network, metadata)
    {
        ProtocolAddress = protocolAddress ?? throw new ArgumentNullException(nameof(protocolAddress));

        if (string.IsNullOrWhiteSpace(protocolName))
            throw new ArgumentException("Protocol name cannot be empty", nameof(protocolName));

        ProtocolName = protocolName;
        PositionType = positionType;
        UnderlyingTokens = new List<Token>();
    }

    public static DeFiPosition Create(
        BlockchainNetwork network,
        AssetMetadata metadata,
        ContractAddress protocolAddress,
        string protocolName,
        DeFiPositionType positionType)
    {
        return new DeFiPosition(network, metadata, protocolAddress, protocolName, positionType);
    }

    public void AddUnderlyingToken(Token token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        UnderlyingTokens.Add(token);
    }

    public void UpdateApr(decimal apr)
    {
        if (apr < 0)
            throw new ArgumentException("APR cannot be negative", nameof(apr));

        Apr = apr;
    }

    public override Money CalculateValue(Currency currency)
    {
        // Sum the value of all underlying tokens
        var total = Money.Zero(currency);

        foreach (var token in UnderlyingTokens)
        {
            total = total.Add(token.CalculateValue(currency));
        }

        return total;
    }

    public override string ToString() => $"DeFi: {ProtocolName} {PositionType} ({Network})";
}

/// <summary>
/// Types of DeFi positions.
/// </summary>
public enum DeFiPositionType
{
    Staking = 1,
    Lending = 2,
    Borrowing = 3,
    LiquidityPool = 4,
    Yield = 5
}
