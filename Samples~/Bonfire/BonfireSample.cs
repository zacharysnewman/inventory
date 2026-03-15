using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates checkpoint save/load using the Inventory package.
///
/// Scenario: a bonfire acts as a save point. The player explores, rests again to
/// overwrite the checkpoint, ventures further, then dies — restoring to the last rest.
///
/// Features shown:
///   • Inventory.Save()         — capture a point-in-time InventorySnapshot
///   • Inventory.Load(snapshot) — restore a snapshot, replacing all current contents
///   • InventorySnapshot        — the serializable data contract; pass it to
///                                JsonUtility or a GUID-aware serializer for
///                                cross-session file persistence
///
/// The demo stays entirely in-memory. For cross-session save files, convert the
/// InventorySnapshot to JSON with JsonUtility (ScriptableObject references become
/// Unity instance IDs within a session) or use Addressables to resolve assets by
/// GUID across sessions.
///
/// Attach to the same GameObject as an Inventory component.
/// Leave the Inventory's ContainerDefinitions list empty in the Inspector;
/// this script builds everything in code for demonstration purposes.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class BonfireSample : MonoBehaviour
{
    private Inventory _player;

    // ── Items ────────────────────────────────────────────────────────────────
    private Item _longSword;    // starting weapon — stackSize 1
    private Item _estusFlask;   // consumable      — stackSize 4
    private Item _ember;        // rare material   — stackSize 1
    private Item _dragonScale;  // legendary drop  — stackSize 1

    // The active bonfire checkpoint; null until the player rests
    private InventorySnapshot _checkpoint;

    private void Start()
    {
        _player = GetComponent<Inventory>();
        CreateDefinitions();
        RunDemo();
    }

    // ── Demo ─────────────────────────────────────────────────────────────────

    private void RunDemo()
    {
        Debug.Log("=== Bonfire Sample ===\n");

        // Set up starting loadout
        _player.AddContainer(MakeContainer("Equipment", capacity: 3));
        _player.AddContainer(MakeContainer("Pouch",     capacity: 4));

        _player.TryAddItem(_longSword);
        _player.TryAddItem(_estusFlask, 2);

        LogState("Starting loadout");

        // ── First bonfire rest ────────────────────────────────────────────────
        _checkpoint = _player.Save();
        Debug.Log(">> Rested at bonfire — checkpoint saved.\n");

        // ── First area cleared ────────────────────────────────────────────────
        _player.TryAddItem(_ember);

        LogState("After clearing first area (Ember found)");

        // ── Second bonfire rest — overwrite checkpoint ────────────────────────
        _checkpoint = _player.Save();
        Debug.Log(">> Rested at bonfire — checkpoint overwritten.\n");

        // ── Second area: rare loot, then death ────────────────────────────────
        _player.TryAddItem(_dragonScale);  // legendary drop
        _player.TryRemoveItem(_estusFlask); // used a flask

        LogState("Before death (Dragon Scale found, one Estus used since last bonfire)");

        // ── Death — restore from last bonfire checkpoint ──────────────────────
        Debug.Log(">> You Died. Restoring from bonfire checkpoint...\n");
        _player.Load(_checkpoint);

        // Load() does not fire events, so query state directly after restoring
        LogState("After respawn (Dragon Scale gone, Estus restored)");
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogState(string label)
    {
        Debug.Log($"── {label} ──");
        Debug.Log($"  Long Sword:  {_player.GetItemCount(_longSword)}");
        Debug.Log($"  Estus Flask: {_player.GetItemCount(_estusFlask)}");
        Debug.Log($"  Ember:       {_player.GetItemCount(_ember)}");
        Debug.Log($"  Dragon Scale:{_player.GetItemCount(_dragonScale)}");
        Debug.Log("");
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private void CreateDefinitions()
    {
        _longSword   = MakeItem("Long Sword",   stackSize: 1);
        _estusFlask  = MakeItem("Estus Flask",  stackSize: 4);
        _ember       = MakeItem("Ember",         stackSize: 1);
        _dragonScale = MakeItem("Dragon Scale",  stackSize: 1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ContainerDefinition MakeContainer(string displayName, int capacity)
    {
        var d = ScriptableObject.CreateInstance<ContainerDefinition>();
        d.displayName     = displayName;
        d.acceptsAllTypes = true;
        d.capacity        = capacity;
        return d;
    }

    private static Item MakeItem(string displayName, int stackSize = 1)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.displayName  = displayName;
        item.maxStackSize = stackSize;
        return item;
    }
}
