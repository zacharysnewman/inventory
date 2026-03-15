# Planned Features

## ItemRegistry

### Problem

Any script that wants to reference a specific `ItemDefinition` — to add a sword to the player's inventory on pickup, for example — must hold a `[SerializeField] private ItemDefinition _sword` and have that field wired in the Unity Inspector. This is fine for a small number of items, but breaks down as the item count grows:

- Every script that touches items accumulates a growing list of serialized asset references.
- Wiring references is manual and error-prone across many prefabs.
- There is no single authoritative place to look up an item by name or ID.

### Proposed Solution

An `ItemRegistry` ScriptableObject — a project-wide lookup table mapping string keys to `ItemDefinition` assets.

```
Assets > Create > Inventory > Item Registry
```

```csharp
// Lookup by key
ItemDefinition sword = _registry.Get("sword");
inventory.TryAddItem(sword);

// Or via a convenience extension
inventory.TryAddItem(_registry, "sword");
```

Scripts reference the single `ItemRegistry` asset instead of individual `ItemDefinition` assets. The registry is populated once (manually or via an editor tool that auto-discovers all `ItemDefinition` assets in the project) and reused everywhere.

### Design Notes

- The registry key could be an explicit string ID field on `ItemDefinition`, or derived from the asset name — needs decision.
- A fallback (warn + return null vs. throw) for missing keys needs to be defined.
- An editor tool that auto-populates the registry by scanning the project for `ItemDefinition` assets would reduce manual maintenance.
- The registry is read-only at runtime; it is not a dynamic item database.
- Multiple registries (e.g. one per DLC pack) may be desirable; a `Get` that searches a list of registries in priority order would support this.
