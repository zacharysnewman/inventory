using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates a player backpack / home stash workflow using the Inventory package.
///
/// Scenario: a player returns from an adventure with a full backpack, deposits loot
/// into their home stash, grabs supplies for the next run, then tidies the stash.
///
/// Features shown:
///   • Two Inventory instances interacting — one per GameObject
///   • TryTransferAll(target, item)  — deposit all of a specific item at once
///   • TryTransferAll(target)        — deposit everything remaining in one call
///   • TryTransferFrom(source, item) — withdraw a specific item from the stash
///   • Container.Sort(comparison)    — sort stash slots by item display name
///   • Inventory.GetItems(filter)    — enumerate only items matching a predicate
///
/// Attach to any GameObject in the scene.
/// In a real project, the player and stash Inventory components would typically
/// live on separate GameObjects wired up in the Inspector.
/// </summary>
public class StashSample : MonoBehaviour
{
    private Inventory _player;
    private Inventory _stash;

    // ── Items ────────────────────────────────────────────────────────────────
    private ItemDefinition _goldGem;      // stackSize 10 — trade material
    private ItemDefinition _ironBar;      // stackSize 5  — crafting material
    private ItemDefinition _healthPotion; // stackSize 5  — consumable
    private ItemDefinition _ancientKey;   // stackSize 1  — quest item

    private void Start()
    {
        // Player inventory lives on this GameObject; stash is a child GameObject
        _player = gameObject.AddComponent<Inventory>();
        _stash  = new GameObject("Stash").AddComponent<Inventory>();
        _stash.transform.SetParent(transform);

        CreateDefinitions();
        RunDemo();
    }

    // ── Demo ─────────────────────────────────────────────────────────────────

    private void RunDemo()
    {
        Debug.Log("=== Stash Sample ===\n");

        // Player backpack: 20 items total
        _player.AddContainer(MakeContainer("Backpack", capacity: 20));

        // Stash: 50 items total (home storage)
        _stash.AddContainer(MakeContainer("Home Stash", capacity: 50));

        // ── Seed starting items ───────────────────────────────────────────────

        // Player returns from adventure with a mixed haul
        _player.TryAddItem(_goldGem,      10);
        _player.TryAddItem(_healthPotion,  2);
        _player.TryAddItem(_ancientKey,    1);
        _player.TryAddItem(_ironBar,       5);

        // Stash already holds supplies from prior sessions
        _stash.TryAddItem(_ironBar,       20);
        _stash.TryAddItem(_healthPotion,   8);

        LogBoth("Starting state");

        // ── Deposit gems to stash ─────────────────────────────────────────────
        // TryTransferAll with a specific item moves every unit in one call
        int gemsMoved = _player.TryTransferAll(_stash, _goldGem);
        Debug.Log($"Deposited {gemsMoved}× Gold Gem to stash");

        // ── Deposit the quest key for safe keeping ────────────────────────────
        _player.TryTransferTo(_stash, _ancientKey);
        Debug.Log("Deposited Ancient Key to stash");

        LogBoth("After depositing gems and key");

        // ── Withdraw potions for the next run ─────────────────────────────────
        // TryTransferFrom pulls from the stash into the player's backpack
        bool gotPotions = _player.TryTransferFrom(_stash, _healthPotion, 6);
        Debug.Log($"Withdrew 6× Health Potion from stash → {(gotPotions ? "success" : "failed")}");

        LogBoth("After withdrawing potions");

        // ── Deposit everything else left in the backpack ──────────────────────
        // TryTransferAll with no item argument moves every item at once
        int totalDeposited = _player.TryTransferAll(_stash);
        Debug.Log($"Deposited {totalDeposited} remaining items to stash (backpack now empty)");

        LogBoth("After depositing remaining backpack contents");

        // ── Sort the stash alphabetically by item display name ────────────────
        var stashContainer = _stash.Containers[0];
        stashContainer.Sort((a, b) =>
            string.Compare(a.item.displayName, b.item.displayName, System.StringComparison.Ordinal));

        Debug.Log("── Stash after sorting A → Z ──");
        foreach (var stack in stashContainer.Stacks)
            Debug.Log($"  {stack.item.displayName}: {stack.quantity}");
        Debug.Log("");

        // ── Filter: show only bulk/stackable items ────────────────────────────
        Debug.Log("── Stackable items in stash (maxStackSize > 1) ──");
        foreach (var stack in _stash.GetItems(item => item.maxStackSize > 1))
            Debug.Log($"  {stack.item.displayName}: {stack.quantity}");
        Debug.Log("");
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogBoth(string label)
    {
        Debug.Log($"── {label} ──");
        LogInventory("  Player", _player);
        LogInventory("  Stash",  _stash);
        Debug.Log("");
    }

    private void LogInventory(string prefix, Inventory inv)
    {
        Debug.Log($"{prefix}:");
        Debug.Log($"    Gold Gem:      {inv.GetItemCount(_goldGem)}");
        Debug.Log($"    Iron Bar:      {inv.GetItemCount(_ironBar)}");
        Debug.Log($"    Health Potion: {inv.GetItemCount(_healthPotion)}");
        Debug.Log($"    Ancient Key:   {inv.GetItemCount(_ancientKey)}");
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private void CreateDefinitions()
    {
        _goldGem      = MakeItem("Gold Gem",      stackSize: 10);
        _ironBar      = MakeItem("Iron Bar",       stackSize:  5);
        _healthPotion = MakeItem("Health Potion",  stackSize:  5);
        _ancientKey   = MakeItem("Ancient Key",    stackSize:  1);
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

    private static ItemDefinition MakeItem(string displayName, int stackSize = 1)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.displayName  = displayName;
        item.maxStackSize = stackSize;
        return item;
    }
}
