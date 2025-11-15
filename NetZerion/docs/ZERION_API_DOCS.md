# Zerion API Documentation

**Base URL:** `https://api.zerion.io/v1`

**Official Documentation:** https://developers.zerion.io/

**Status Page:** https://status.zerion.io/

---

## Overview

The Zerion API provides access to comprehensive web3 data across multiple blockchain networks. It enables developers to build feature-rich applications with:

- **Wallet Data**: Portfolio balances, DeFi positions, transaction history, NFT holdings
- **Asset Information**: Token prices, metadata, and availability across chains
- **Chain Data**: Gas prices, chain information, DApp listings
- **Subscriptions**: Transaction monitoring with webhook callbacks
- **Swap/Bridge**: Cross-chain asset swaps and bridge offers

---

## Authentication

### Method: HTTP Basic Authentication

**Header Format:**
```
Authorization: Basic {base64-encoded-credentials}
```

**Encoding:**
```javascript
// API key is used as username, password is empty
const credentials = btoa(`${apiKey}:`);
// Result: Authorization: Basic <credentials>
```

**Example (C#):**
```csharp
var apiKey = "your-api-key";
var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:"));
client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
```

---

## Rate Limits

| Tier | Requests/Day | Requests/Minute | Cost |
|------|--------------|-----------------|------|
| **Free** | 3,000 | ~100 | $0 |
| **Starter** | 10,000 | ~200 | $99/mo |
| **Pro** | 50,000 | ~500 | $299/mo |
| **Business** | 200,000 | ~1000 | $599/mo |

**Rate Limit Headers:**
- `X-RateLimit-Limit` - Maximum requests allowed
- `X-RateLimit-Remaining` - Requests remaining
- `X-RateLimit-Reset` - Unix timestamp when limit resets

**429 Response:**
```json
{
  "errors": [{
    "title": "Rate limit exceeded",
    "detail": "Too many requests",
    "status": "429"
  }],
  "links": {
    "docs": "https://developers.zerion.io/"
  }
}
```

---

## Supported Networks (Chain IDs)

| Network | Chain ID | Native Token |
|---------|----------|--------------|
| Ethereum | `ethereum` | ETH |
| Polygon | `polygon` | MATIC |
| Arbitrum | `arbitrum` | ETH |
| Optimism | `optimism` | ETH |
| Base | `base` | ETH |
| BSC | `binance-smart-chain` | BNB |
| Avalanche | `avalanche` | AVAX |
| Fantom | `fantom` | FTM |
| zkSync Era | `zksync-era` | ETH |
| Scroll | `scroll` | ETH |
| Linea | `linea` | ETH |
| Blast | `blast` | ETH |
| Unichain | `unichain` | ETH |

---

## Core Endpoints

### 1. Get Wallet Positions (DeFi)

Returns DeFi protocol positions including LP, staking, lending, borrowing, etc.

**Endpoint:**
```
GET /v1/wallets/{address}/positions/
```

**Parameters:**
- `address` (required) - Wallet address (0x...)
- `filter[chain_ids]` (optional) - Comma-separated chain IDs (e.g., "ethereum,polygon")
- `filter[position_types]` (optional) - Filter by type (e.g., "deposit,loan,staked")
- `filter[trash]` (optional) - Include dust positions ("only", "exclude")
- `sort` (optional) - Sort field (e.g., "-value")
- `page[size]` (optional) - Results per page (max 100, default 30)
- `page[after]` (optional) - Pagination cursor

**Request Example:**
```bash
curl -X GET "https://api.zerion.io/v1/wallets/0x123.../positions/?filter[chain_ids]=ethereum" \
  -H "Authorization: Basic {credentials}"
```

**Response Schema:**
```json
{
  "data": [
    {
      "type": "position",
      "id": "0x123..._ethereum_uniswap-v3",
      "attributes": {
        "parent": null,
        "protocol": "Uniswap V3",
        "name": "ETH/USDC Pool",
        "position_type": "deposit",
        "quantity": {
          "int": "100000000000000000",
          "decimals": 18,
          "float": 0.1,
          "numeric": "0.1"
        },
        "value": 250.50,
        "price": 2505,
        "changes": {
          "absolute_1d": 5.25,
          "percent_1d": 0.021
        },
        "fungible_info": {
          "name": "ETH/USDC LP",
          "symbol": "UNI-V3-ETH-USDC",
          "icon": {
            "url": "https://..."
          },
          "flags": {
            "verified": true
          },
          "implementations": [
            {
              "chain_id": "ethereum",
              "address": "0x...",
              "decimals": 18
            }
          ]
        }
      },
      "relationships": {
        "chain": {
          "data": {
            "type": "chain",
            "id": "ethereum"
          }
        },
        "fungibles": {
          "data": [
            {
              "type": "fungible",
              "id": "ethereum_0x..."
            }
          ]
        }
      }
    }
  ],
  "links": {
    "self": "https://api.zerion.io/v1/wallets/0x.../positions/",
    "next": "https://api.zerion.io/v1/wallets/0x.../positions/?page[after]=..."
  },
  "included": [
    {
      "type": "chain",
      "id": "ethereum",
      "attributes": {
        "name": "Ethereum",
        "icon": { "url": "..." }
      }
    }
  ]
}
```

---

### 2. Get Fungible Portfolio (Tokens)

Returns wallet's fungible token balances with prices and metadata.

**Endpoint:**
```
GET /v1/wallets/{address}/portfolio/
```

**Parameters:**
- `address` (required) - Wallet address
- `filter[chain_ids]` (optional) - Comma-separated chain IDs
- `filter[trash]` (optional) - Include dust ("only", "exclude")
- `sort` (optional) - Sort field (e.g., "-value")
- `page[size]` (optional) - Results per page (max 100)
- `page[after]` (optional) - Pagination cursor
- `currency` (optional) - Price currency (default: "usd")

**Response Schema:**
```json
{
  "data": [
    {
      "type": "fungible",
      "id": "ethereum_0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
      "attributes": {
        "name": "Wrapped Ether",
        "symbol": "WETH",
        "description": "Wrapped ETH",
        "icon": {
          "url": "https://..."
        },
        "flags": {
          "verified": true,
          "displayable": true
        },
        "external_links": [],
        "market_data": {
          "total_supply": 3500000,
          "circulating_supply": 3400000,
          "market_cap": 8500000000,
          "fully_diluted_valuation": 8750000000,
          "price": 2500,
          "changes": {
            "percent_1d": 0.025,
            "percent_7d": -0.05,
            "percent_30d": 0.15
          }
        },
        "implementations": [
          {
            "chain_id": "ethereum",
            "address": "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            "decimals": 18
          }
        ],
        "quantity": {
          "int": "1500000000000000000",
          "decimals": 18,
          "float": 1.5,
          "numeric": "1.5"
        },
        "value": 3750
      },
      "relationships": {
        "chain": {
          "data": { "type": "chain", "id": "ethereum" }
        }
      }
    }
  ],
  "links": {
    "self": "...",
    "next": "..."
  }
}
```

---

### 3. Get Transaction History

Returns decoded transaction history with human-readable descriptions.

**Endpoint:**
```
GET /v1/wallets/{address}/transactions/
```

**Parameters:**
- `address` (required) - Wallet address
- `filter[chain_ids]` (optional) - Chain filter
- `filter[operation_types]` (optional) - Filter by type ("send", "receive", "trade", "approve", etc.)
- `filter[min_mined_at]` (optional) - Unix timestamp, transactions after this time
- `filter[max_mined_at]` (optional) - Unix timestamp, transactions before this time
- `page[size]` (optional) - Results per page (max 100)
- `page[after]` (optional) - Pagination cursor
- `sort` (optional) - Sort field (default: "-mined_at")

**Response Schema:**
```json
{
  "data": [
    {
      "type": "transaction",
      "id": "0xabc123..._ethereum",
      "attributes": {
        "operation_type": "trade",
        "hash": "0xabc123...",
        "mined_at_block": 18500000,
        "mined_at": 1698768000,
        "status": "confirmed",
        "nonce": 42,
        "fee": {
          "fungible_info": {
            "name": "Ether",
            "symbol": "ETH"
          },
          "quantity": {
            "numeric": "0.00521"
          },
          "price": 2500,
          "value": 13.025
        },
        "transfers": [
          {
            "fungible_info": {
              "name": "USD Coin",
              "symbol": "USDC",
              "icon": { "url": "..." }
            },
            "direction": "out",
            "quantity": {
              "numeric": "1000"
            },
            "value": 1000,
            "price": 1,
            "sender": "0x123...",
            "recipient": "0xdef..."
          },
          {
            "fungible_info": {
              "name": "Ether",
              "symbol": "ETH"
            },
            "direction": "in",
            "quantity": {
              "numeric": "0.4"
            },
            "value": 1000,
            "price": 2500,
            "sender": "0xdef...",
            "recipient": "0x123..."
          }
        ],
        "approvals": [],
        "application_metadata": {
          "name": "Uniswap V3",
          "icon": { "url": "..." },
          "contract_address": "0x..."
        }
      },
      "relationships": {
        "chain": {
          "data": { "type": "chain", "id": "ethereum" }
        }
      }
    }
  ],
  "links": {
    "self": "...",
    "next": "..."
  }
}
```

---

### 4. Get Transaction Details

Returns detailed information about a specific transaction.

**Endpoint:**
```
GET /v1/transactions/{tx_hash}/
```

**Parameters:**
- `tx_hash` (required) - Transaction hash
- `chain_id` (required, query param) - Chain ID

**Response:** Same structure as transaction history, but for a single transaction.

---

## Transaction Types (operation_type)

| Type | Description |
|------|-------------|
| `send` | Token transfer out |
| `receive` | Token transfer in |
| `trade` | Token swap (DEX) |
| `approve` | Token approval |
| `mint` | Token/NFT minting |
| `burn` | Token burning |
| `deposit` | Deposit to protocol |
| `withdraw` | Withdraw from protocol |
| `borrow` | Borrowing from lending protocol |
| `repay` | Repaying loan |
| `stake` | Staking tokens |
| `unstake` | Unstaking tokens |
| `claim` | Claiming rewards |
| `bridge` | Cross-chain bridge |
| `deployment` | Contract deployment |
| `execution` | Contract execution |

---

## Position Types (position_type)

| Type | Description |
|------|-------------|
| `deposit` | Liquidity pool positions, deposits in protocols |
| `loan` | Lending positions (supplying assets) |
| `borrow` | Borrowing positions (debt) |
| `staked` | Staked tokens (earning rewards) |
| `locked` | Locked/vested tokens |
| `claimable` | Claimable rewards |

---

## Error Responses

### Standard Error Format (JSON:API)
```json
{
  "errors": [
    {
      "status": "404",
      "title": "Not Found",
      "detail": "Wallet not found or has no activity"
    }
  ],
  "links": {
    "docs": "https://developers.zerion.io/"
  }
}
```

### Common Error Codes

| Status | Title | Meaning |
|--------|-------|---------|
| 400 | Bad Request | Invalid parameters or malformed request |
| 401 | Unauthorized | Missing or invalid API key |
| 403 | Forbidden | API key doesn't have required permissions |
| 404 | Not Found | Resource not found |
| 429 | Rate Limit Exceeded | Too many requests |
| 500 | Internal Server Error | Server-side error |
| 503 | Service Unavailable | Temporary outage |

---

## Best Practices

### 1. Polling & Timeouts
- **Don't poll forever**: Stop retries after 2 minutes
- **Implement backoff**: Use exponential backoff for retries
- **Cache aggressively**: Position data changes infrequently (cache 5+ min)

### 2. Pagination
- **Use cursors**: Always use `page[after]` for pagination
- **Limit page size**: Don't exceed 100 items per page
- **Watch URL length**: Many filters can create long URLs

### 3. ID Handling
- **Treat IDs as strings**: Never parse or assume ID format
- **IDs are unique**: Use them for caching keys
- **Chain-specific**: Same address on different chains = different IDs

### 4. Network-Specific Notes

**Solana:**
- Protocol positions not yet supported
- Token positions may take a few seconds to bootstrap
- Retry if position missing on first query

**Testnets:**
- Use `X-Env` header for testnet access
- Data quality may vary

---

## Response Format

All responses follow the **JSON:API** specification:

### Structure
```json
{
  "data": [],           // Main response data
  "included": [],       // Related resources (chains, tokens, etc.)
  "links": {            // Pagination links
    "self": "...",
    "next": "...",
    "prev": "..."
  },
  "meta": {}            // Additional metadata
}
```

### Relationships
Resources use relationships to link to other resources:
- Position → Chain, Fungibles
- Transaction → Chain, Application
- Fungible → Chain

Use `included` array to resolve relationships without additional requests.

---

## Code Examples

### C# - Get Positions
```csharp
using System.Net.Http;
using System.Text;

var apiKey = "your-api-key";
var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb";

using var client = new HttpClient();
client.BaseAddress = new Uri("https://api.zerion.io/v1/");

var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:"));
client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

var response = await client.GetAsync(
    $"wallets/{address}/positions/?filter[chain_ids]=ethereum");

var json = await response.Content.ReadAsStringAsync();
Console.WriteLine(json);
```

### JavaScript - Get Portfolio
```javascript
const apiKey = 'your-api-key';
const address = '0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb';

const credentials = btoa(`${apiKey}:`);

const response = await fetch(
  `https://api.zerion.io/v1/wallets/${address}/portfolio/`,
  {
    headers: {
      'Authorization': `Basic ${credentials}`
    }
  }
);

const data = await response.json();
console.log(data);
```

### Python - Get Transactions
```python
import requests
import base64

api_key = 'your-api-key'
address = '0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb'

credentials = base64.b64encode(f'{api_key}:'.encode()).decode()

response = requests.get(
    f'https://api.zerion.io/v1/wallets/{address}/transactions/',
    headers={
        'Authorization': f'Basic {credentials}'
    },
    params={
        'filter[chain_ids]': 'ethereum',
        'page[size]': 50
    }
)

data = response.json()
print(data)
```

---

## Additional Resources

- **Official Docs:** https://developers.zerion.io/
- **API Status:** https://status.zerion.io/
- **Discord Community:** https://discord.gg/zerion
- **GitHub:** https://github.com/zeriontech

---

## Version History

| Date | Changes |
|------|---------|
| 2025-10-28 | Initial documentation for NetZerion library |

---

**Note:** This documentation is maintained for NetZerion library development. For the most up-to-date information, always refer to the official Zerion API documentation at https://developers.zerion.io/
