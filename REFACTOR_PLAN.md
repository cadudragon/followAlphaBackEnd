# Plano de Refatoração - ZerionPortfolioProvider

## Objetivo
Mover TODA a lógica de agregação e transformação para DENTRO do ZerionPortfolioProvider.
Eliminar serviços intermediários que apenas fazem ruído.

## Arquivos Fonte (já existem no git)
1. **TrackFi.Infrastructure/DeFi/ZerionService.cs** (commit 4a18182) - 544 linhas
   - ✅ Tem agregação: `AggregateZerionPositions()`, `AggregateFarmingByGroupId()`, `AggregateLendingByProtocol()`
   - ✅ Tem mapeamento Zerion → DeFiPositionData

2. **TrackFi.Infrastructure/Portfolio/DeFiPortfolioService.cs** (commit 4a18182) - 456 linhas
   - ✅ Tem transformação: `TransformToFarmingDto()`, `TransformToLendingDto()`, `TransformToStakingDto()`
   - ✅ Tem categorização por PositionType (Farming, Lending, Staking, etc.)

3. **TrackFi.Infrastructure/Portfolio/DeFiPortfolioDtos.cs** (já restaurado)
   - ✅ Tem DTOs categorizados: `FarmingPositionDto`, `LendingPositionDto`, etc.

## Estrutura do Novo ZerionPortfolioProvider

```csharp
public class ZerionPortfolioProvider : IPortfolioProvider
{
    // MÉTODO 1: Wallet Positions (~100 linhas)
    Task<MultiNetworkWalletDto> GetWalletPositionsAsync(...)
    {
        // 1. Fetch Zerion with filter=only_simple
        // 2. Extract Fungible[] from positions
        // 3. Transform to TokenBalanceDto
        // 4. Group by network → NetworkWalletDto[]
        // 5. Return MultiNetworkWalletDto
    }

    // MÉTODO 2: DeFi Positions (~400 linhas) - CORE DO CÓDIGO
    Task<MultiNetworkDeFiPortfolioDto> GetDeFiPositionsAsync(...)
    {
        // 1. Fetch Zerion with filter=only_complex
        // 2. **AGREGAR** positions (do ZerionService):
        //    - AggregateFarmingByGroupId() - group farming by group_id
        //    - AggregateLendingByProtocol() - combine supplied + borrowed
        //    - AggregateStakingByProtocol() - combine staked + rewards
        // 3. **CATEGORIZAR** por PositionType → Farming[], Lending[], Staking[]
        // 4. **TRANSFORMAR** para DTOs (do DeFiPortfolioService):
        //    - TransformToFarmingDto() → FarmingPositionDto
        //    - TransformToLendingDto() → LendingPositionDto
        //    - TransformToStakingDto() → StakingPositionDto
        //    - TransformToYieldDto() → YieldPositionDto
        // 5. Group by network → NetworkDeFiPortfolioDto[]
        // 6. Return MultiNetworkDeFiPortfolioDto
    }

    // MÉTODO 3: Full Portfolio (~150 linhas)
    Task<FullPortfolioDto> GetFullPortfolioAsync(...)
    {
        // 1. Fetch Zerion with filter=no_filter
        // 2. Separate wallet tokens vs DeFi positions
        // 3. Apply aggregation to DeFi
        // 4. Transform both
        // 5. Combine in NetworkFullPortfolioDto[]
        // 6. Return FullPortfolioDto
    }

    // MÉTODOS PRIVADOS DE AGREGAÇÃO (copiar do ZerionService)
    private List<DeFiPositionData> AggregateZerionPositions(...)
    private List<DeFiPositionData> AggregateFarmingByGroupId(...)
    private List<DeFiPositionData> AggregateLendingByProtocol(...)
    private List<DeFiPositionData> AggregateStakingByProtocol(...)

    // MÉTODOS PRIVADOS DE TRANSFORMAÇÃO (copiar do DeFiPortfolioService)
    private FarmingPositionDto TransformToFarmingDto(...)
    private LendingPositionDto TransformToLendingDto(...)
    private StakingPositionDto TransformToStakingDto(...)
    private YieldPositionDto TransformToYieldDto(...)
    // + outros helpers

    // MÉTODOS DE MAPEAMENTO
    private ChainId MapNetworkToChainId(string network)
}
```

## Total Estimado
- **~700-800 linhas** no ZerionPortfolioProvider
- **Copiar/adaptar** ~60% do código existente
- **Escrever novo** ~40% (principalmente wallet positions e combinação)

## PortfolioService Simplificado
Depois disso, PortfolioService vira APENAS cache + metadata (~100 linhas):

```csharp
public class PortfolioService
{
    private readonly IPortfolioProvider _provider;
    private readonly DistributedCacheService _cache;
    private readonly INetworkMetadataRepository _networkMetadata;

    // Apenas 3 métodos que fazem:
    // 1. Check cache
    // 2. Call provider
    // 3. Enrich with network metadata (logos)
    // 4. Cache result
    // 5. Return

    Task<MultiNetworkWalletDto> GetWalletPositionsAsync(...)
    Task<MultiNetworkDeFiPortfolioDto> GetDeFiPositionsAsync(...)
    Task<FullPortfolioDto> GetFullPortfolioAsync(...)
}
```

## Decisão
Implemento tudo agora (~1h de trabalho, muito token) ou fazemos diferente?
