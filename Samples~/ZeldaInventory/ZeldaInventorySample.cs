using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates a Zelda-style inventory using the Inventory package.
///
/// Container types and sizes:
///   Bomb Bag  — Small (10), Large (20), Biggest (30)
///   Quiver    — Small (30), Large (40), Biggest (50)
///   Equipment — Sword slot, Bow slot (capacity 1 each)
///   Hearts    — collectible heart containers (max 20)
///   Bottle    — 4 bottle slots for potions
///
/// Currency: Rupees — modelled as an item stored in a Wallet container.
///   Small Wallet  →  99 rupee capacity
///   Large Wallet  → 200 rupee capacity
///   Giant Wallet  → 500 rupee capacity
///   Upgrading the wallet swaps the container, preserving rupees that fit.
///
/// The demo shows:
///   1. Starting with a small bomb bag, small quiver, and small wallet (rupee cap 99)
///   2. Looting the Master Sword and Hero's Bow
///   3. Buying bombs, arrows, and a potion at a shop (manual remove rupees + add item)
///   4. Upgrading the bomb bag (small → large) via SwapContainer
///   5. Upgrading the wallet (small → large) via SwapContainer, raising rupee cap to 200
///   6. Earning a heart container after defeating a boss
///
/// Attach to the same GameObject as an Inventory component.
/// Leave the Inventory's ContainerDefinitions list empty in the Inspector;
/// this script builds everything in code for demonstration purposes.
/// In a real project, create ScriptableObject assets via the Asset menu instead.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class ZeldaInventorySample : MonoBehaviour
{
    private Inventory _inventory;

    // ── Container Definitions ────────────────────────────────────────────────
    private ContainerDefinition _smallBombBag;
    private ContainerDefinition _largeBombBag;
    private ContainerDefinition _biggestBombBag;
    private ContainerDefinition _smallQuiver;
    private ContainerDefinition _largeQuiver;
    private ContainerDefinition _biggestQuiver;
    private ContainerDefinition _swordSlot;
    private ContainerDefinition _bowSlot;
    private ContainerDefinition _heartSlot;
    private ContainerDefinition _bottleSlots;
    private ContainerDefinition _smallWallet;
    private ContainerDefinition _largeWallet;
    private ContainerDefinition _giantWallet;

    // ── Items ────────────────────────────────────────────────────────────────
    private Item _rupee;
    private Item _bomb;
    private Item _arrow;
    private Item _masterSword;
    private Item _heroBow;
    private Item _heartContainer;
    private Item _bluePotion;
    private Item _redPotion;

    private void Start()
    {
        _inventory = GetComponent<Inventory>();
        CreateDefinitions();
        RunDemo();
    }

    // ── Demo ─────────────────────────────────────────────────────────────────

    private void RunDemo()
    {
        Debug.Log("=== Zelda Inventory Demo ===\n");

        _inventory.AddContainer(_smallBombBag);
        _inventory.AddContainer(_smallQuiver);
        _inventory.AddContainer(_swordSlot);
        _inventory.AddContainer(_bowSlot);
        _inventory.AddContainer(_heartSlot);
        _inventory.AddContainer(_bottleSlots);
        _inventory.AddContainer(_smallWallet);

        _inventory.TryAddItem(_heartContainer, 3);
        _inventory.AddAsManyAsPossible(_rupee, 200); // clamped to 99 by small wallet

        LogState("Start of adventure");

        // ── Visit a shop ─────────────────────────────────────────────────────
        TryBuy(_bomb,       5, costPerUnit: 10);
        TryBuy(_arrow,     10, costPerUnit:  5);
        TryBuy(_bluePotion, 1, costPerUnit: 50);

        LogState("After shopping");

        // ── Loot weapons from a dungeon chest ────────────────────────────────
        _inventory.TryAddItem(_masterSword);
        _inventory.TryAddItem(_heroBow);

        // ── Upgrade the bomb bag ──────────────────────────────────────────────
        _inventory.SwapContainer(_smallBombBag, _largeBombBag);

        LogState("After upgrading to Large Bomb Bag");

        // ── Upgrade the wallet ────────────────────────────────────────────────
        _inventory.SwapContainer(_smallWallet, _largeWallet);
        _inventory.AddAsManyAsPossible(_rupee, 50);

        LogState("After upgrading to Large Wallet");

        // ── Defeat a boss — earn a heart container ────────────────────────────
        _inventory.TryAddItem(_heartContainer);

        LogState("After boss reward");
    }

    // ── Shop helper ───────────────────────────────────────────────────────────

    private void TryBuy(Item item, int quantity, int costPerUnit)
    {
        int totalCost = costPerUnit * quantity;
        if (_inventory.GetItemCount(_rupee) >= totalCost && _inventory.CanAddItem(item, quantity))
        {
            _inventory.TryRemoveItem(_rupee, totalCost);
            _inventory.TryAddItem(item, quantity);
            Debug.Log($"Bought {quantity}× {item.displayName} for {totalCost} rupees");
        }
        else
        {
            Debug.Log($"Cannot buy {quantity}× {item.displayName} (cost {totalCost}, have {_inventory.GetItemCount(_rupee)})");
        }
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private void CreateDefinitions()
    {
        _smallBombBag   = MakeContainer("Small Bomb Bag",   "BombBag",   10);
        _largeBombBag   = MakeContainer("Large Bomb Bag",   "BombBag",   20);
        _biggestBombBag = MakeContainer("Biggest Bomb Bag", "BombBag",   30);
        _smallQuiver    = MakeContainer("Small Quiver",     "Quiver",    30);
        _largeQuiver    = MakeContainer("Large Quiver",     "Quiver",    40);
        _biggestQuiver  = MakeContainer("Biggest Quiver",   "Quiver",    50);
        _swordSlot      = MakeContainer("Sword Slot",       "Equipment",  1);
        _bowSlot        = MakeContainer("Bow Slot",         "Equipment",  1);
        _heartSlot      = MakeContainer("Heart Track",      "Heart",     20);
        _bottleSlots    = MakeContainer("Bottle Slots",     "Bottle",     4);
        _smallWallet    = MakeContainer("Small Wallet",     "Wallet",    99);
        _largeWallet    = MakeContainer("Large Wallet",     "Wallet",   200);
        _giantWallet    = MakeContainer("Giant Wallet",     "Wallet",   500);

        _rupee          = MakeItem("Rupee",           "Wallet",    maxStackSize: 99);
        _bomb           = MakeItem("Bomb",            "BombBag");
        _arrow          = MakeItem("Arrow",           "Quiver");
        _masterSword    = MakeItem("Master Sword",    "Equipment");
        _heroBow        = MakeItem("Hero's Bow",      "Equipment");
        _heartContainer = MakeItem("Heart Container", "Heart");
        _bluePotion     = MakeItem("Blue Potion",     "Bottle");
        _redPotion      = MakeItem("Red Potion",      "Bottle");
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogState(string label)
    {
        Debug.Log($"── {label} ──");
        Debug.Log($"  Rupees:      {_inventory.GetItemCount(_rupee)}");
        Debug.Log($"  Hearts:      {_inventory.GetItemCount(_heartContainer)}");
        Debug.Log($"  Bombs:       {_inventory.GetItemCount(_bomb)}");
        Debug.Log($"  Arrows:      {_inventory.GetItemCount(_arrow)}");
        Debug.Log($"  Sword:       {(_inventory.GetItemCount(_masterSword) > 0 ? "Master Sword" : "none")}");
        Debug.Log($"  Bow:         {(_inventory.GetItemCount(_heroBow) > 0 ? "Hero's Bow" : "none")}");
        Debug.Log($"  Blue Potion: {_inventory.GetItemCount(_bluePotion)}");
        Debug.Log($"  Red Potion:  {_inventory.GetItemCount(_redPotion)}");
        Debug.Log("");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ContainerDefinition MakeContainer(string displayName, string type, int capacity)
    {
        var d = ScriptableObject.CreateInstance<ContainerDefinition>();
        d.displayName = displayName;
        d.acceptedTypes.Add(type);
        d.capacity = capacity;
        return d;
    }

    private static Item MakeItem(string displayName, string type, int maxStackSize = 1)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.displayName  = displayName;
        item.itemType     = type ?? string.Empty;
        item.maxStackSize = maxStackSize;
        return item;
    }
}
