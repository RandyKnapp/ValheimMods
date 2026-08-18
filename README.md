# ValheimMods

## Integrating with Epic Loot

Epic Loot exposes a supported API for other plugins — content registration plus behaviour hooks
(inventory/equipment providers, sacrifice filters, lifecycle events, loot generation). Start at
[EpicLoot/docs/API.md](EpicLoot/docs/API.md); the [`EpicLootAPI`](EpicLootAPI/EpicLootAPI/README.md) shim
provides typed wrappers you can bundle into your own plugin.
