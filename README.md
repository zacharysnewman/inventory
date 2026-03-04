# Inventory

A flexible, event-driven inventory management system for Unity. Supports typed containers, stackable items, multiple currencies, and currency-gated purchases. Designed to handle everything from Zelda-style equipment bags to Warzone-style weapon loadouts.

## Features

- **Typed containers** — route items to dedicated slots (ammo pouches, weapon slots, etc.) with overflow into general backpacks
- **Two capacity modes** — count total item quantity (bomb bags) or total slot count (grid inventories)
- **Multi-container aggregation** — `GetItemCount()` returns totals across all containers automatically
- **Atomic operations** — transfers and purchases either succeed completely or have no effect
- **Multiple currencies** — rupees, cash, gold, or any custom type
- **Runtime container management** — add/remove containers at runtime for upgrades and unlocks
- **Event-driven** — every change fires an event; no polling required for UI

## Installation

Add via the Unity Package Manager using this repository's URL.

## Setup

All assets are created via the Unity Editor context menu under **Assets > Create > Inventory**.

1. **ContainerType** — marker assets used for semantic grouping (e.g., `Bombs`, `Arrows`, `Weapons`)
2. **Currency** — define currency types (e.g., `Rupees`, `Cash`)
3. **ContainerDefinition** — configure a container's accepted types, capacity, and capacity mode
4. **Item** — define an item's compatible container types, stack size, and purchase cost
5. **Inventory** — add the `Inventory` MonoBehaviour to a GameObject and assign your `ContainerDefinition` assets

## Core Concepts

### Items and Container Types

An `Item`'s `compatibleContainerTypes` list controls which containers it can enter:

- **Populated** — the item prefers containers that accept at least one matching type; overflows to `acceptsAllTypes` containers when full
- **Empty** — the item only enters `acceptsAllTypes` containers (general backpack behavior)

### Container Priority

`TryAddItem()` iterates containers in order. Place dedicated typed containers before general backpacks so typed items fill their designated slots first.

### Capacity Modes

| Mode | Behavior | Use case |
|------|----------|----------|
| `TotalItems` | Capacity = sum of all item quantities | Ammo bags, bomb pouches |
| `Slots` | Capacity = number of distinct stack slots | Grid inventories, loadouts |

## Usage

```csharp
// Add and remove items
inventory.TryAddItem(bomb, 5);
inventory.TryRemoveItem(bomb, 1);

// Check and spend currency
inventory.AddCurrency(rupees, 100);
inventory.TrySpendCurrency(rupees, 20);

// Purchase (atomic: checks affordability and capacity before deducting)
if (inventory.CanAfford(item))
    inventory.TryPurchase(item);

// Query counts
int bombs = inventory.GetItemCount(bomb);
int wallet = inventory.GetCurrency(rupees);

// Transfer between inventories (e.g., player looting a chest)
chest.TryTransferTo(player, item, quantity);

// Upgrade a container at runtime
int saved = inventory.GetContainer(smallBag).GetQuantity(bomb);
inventory.RemoveContainer(smallBag);
inventory.AddContainer(largeBag);
inventory.TryAddItem(bomb, saved);

// Subscribe to events
inventory.OnItemAdded     += (item, count)           => UpdateUI(item, count);
inventory.OnCurrencyChanged += (currency, amount)    => UpdateWallet(currency, amount);
inventory.OnContainerAdded  += (container)           => ShowSlot(container);
inventory.OnContainerRemoved += (definition)         => HideSlot(definition);
```

## Samples

Import samples from the Package Manager window.

### Zelda Inventory
Classic action-adventure setup with bomb bags, quivers, wallets, equipment slots, heart containers, and potion bottles. Demonstrates upgradeable containers and a rupee-based shop system.

### Warzone Inventory
Tactical shooter loadout with primary/secondary/tertiary weapon slots, lethal/tactical equipment, armor plates, a general backpack, and a cash economy. Demonstrates overflow mechanics and runtime slot unlocking.

### Inventory Renderer
A UI binding example using event subscriptions to display item counts, currency totals, container slot contents, and capacity — entirely without polling.
