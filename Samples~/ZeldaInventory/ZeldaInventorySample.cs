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

    // ── Container Types ──────────────────────────────────────────────────────
    private ContainerType _bombBagType;
    private ContainerType _quiverType;
    private ContainerType _equipmentType;
    private ContainerType _heartType;
    private ContainerType _bottleType;
    private ContainerType _walletType;

    // ── Container Definitions ────────────────────────────────────────────────

    // Bomb bags — player starts with Small, can upgrade at a shop
    private ContainerDefinition _smallBombBag;    // 10 bombs
    private ContainerDefinition _largeBombBag;    // 20 bombs
    private ContainerDefinition _biggestBombBag;  // 30 bombs

    // Quivers — upgradeable the same way
    private ContainerDefinition _smallQuiver;     // 30 arrows
    private ContainerDefinition _largeQuiver;     // 40 arrows
    private ContainerDefinition _biggestQuiver;   // 50 arrows

    // Equipment — one slot per item type; capacity 1 means "equipped or not"
    private ContainerDefinition _swordSlot;
    private ContainerDefinition _bowSlot;

    // Hearts — each Heart Container item = one heart; max 20
    private ContainerDefinition _heartSlot;

    // Bottles — up to 4 potions stored at once
    private ContainerDefinition _bottleSlots;

    // Wallets — rupees are items stored here; capacity = rupee cap
    private ContainerDefinition _smallWallet;   //  99 rupees
    private ContainerDefinition _largeWallet;   // 200 rupees
    private ContainerDefinition _giantWallet;   // 500 rupees

    // ── Items ────────────────────────────────────────────────────────────────
    private Item _rupee;
    private Item _bomb;
    private Item _arrow;
    private Item _masterSword;
    private Item _heroBow;
    private Item _heartContainer;
    private Item _bluePotion;   //  50 rupees at the shop
    private Item _redPotion;    // 150 rupees at the shop

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

        // Give the player a starting loadout
        _inventory.AddContainer(_smallBombBag);
        _inventory.AddContainer(_smallQuiver);
        _inventory.AddContainer(_swordSlot);
        _inventory.AddContainer(_bowSlot);
        _inventory.AddContainer(_heartSlot);
        _inventory.AddContainer(_bottleSlots);
        _inventory.AddContainer(_smallWallet);

        // Start with 3 hearts and 99 rupees (small wallet fills to its cap)
        _inventory.TryAddItem(_heartContainer, 3);
        _inventory.AddAsManyAsPossible(_rupee, 200); // clamped to 99 by small wallet

        LogState("Start of adventure");

        // ── Visit a shop ─────────────────────────────────────────────────────
        // Purchasing = remove rupees then add item
        TryBuy(_bomb,       5, costPerUnit: 10);  // −50 rupees
        TryBuy(_arrow,     10, costPerUnit:  5);  // −50 rupees
        TryBuy(_bluePotion, 1, costPerUnit: 50);  // −50 rupees (but we only have ~99−50−50 = ... depends on order)

        LogState("After shopping");

        // ── Loot weapons from a dungeon chest ────────────────────────────────
        _inventory.TryAddItem(_masterSword);
        _inventory.TryAddItem(_heroBow);

        // ── Upgrade the bomb bag ──────────────────────────────────────────────
        // SwapContainer preserves bombs that fit and returns overflow (none expected here)
        var overflow = _inventory.SwapContainer(_smallBombBag, _largeBombBag);
        if (overflow.Count > 0)
            Debug.Log($"Bomb bag upgrade overflow: {overflow[0].quantity} bombs lost");

        LogState("After upgrading to Large Bomb Bag");

        // ── Upgrade the wallet ────────────────────────────────────────────────
        // SwapContainer preserves rupees up to the new cap.
        _inventory.SwapContainer(_smallWallet, _largeWallet);
        _inventory.AddAsManyAsPossible(_rupee, 50); // could now hold up to 200

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
        _bombBagType   = MakeType("BombBag");
        _quiverType    = MakeType("Quiver");
        _equipmentType = MakeType("Equipment");
        _heartType     = MakeType("Heart");
        _bottleType    = MakeType("Bottle");
        _walletType    = MakeType("Wallet");

        _smallBombBag   = MakeContainer("Small Bomb Bag",   _bombBagType,  10);
        _largeBombBag   = MakeContainer("Large Bomb Bag",   _bombBagType,  20);
        _biggestBombBag = MakeContainer("Biggest Bomb Bag", _bombBagType,  30);

        _smallQuiver   = MakeContainer("Small Quiver",   _quiverType, 30);
        _largeQuiver   = MakeContainer("Large Quiver",   _quiverType, 40);
        _biggestQuiver = MakeContainer("Biggest Quiver", _quiverType, 50);

        _swordSlot   = MakeContainer("Sword Slot",   _equipmentType, 1);
        _bowSlot     = MakeContainer("Bow Slot",     _equipmentType, 1);
        _heartSlot   = MakeContainer("Heart Track",  _heartType,    20);
        _bottleSlots = MakeContainer("Bottle Slots", _bottleType,    4);

        _smallWallet = MakeContainer("Small Wallet",  _walletType,  99);
        _largeWallet = MakeContainer("Large Wallet",  _walletType, 200);
        _giantWallet = MakeContainer("Giant Wallet",  _walletType, 500);

        _rupee          = MakeItem("Rupee",           _walletType,    maxStackSize: 99);
        _bomb           = MakeItem("Bomb",            _bombBagType);
        _arrow          = MakeItem("Arrow",           _quiverType);
        _masterSword    = MakeItem("Master Sword",    _equipmentType);
        _heroBow        = MakeItem("Hero's Bow",      _equipmentType);
        _heartContainer = MakeItem("Heart Container", _heartType);
        _bluePotion     = MakeItem("Blue Potion",     _bottleType);
        _redPotion      = MakeItem("Red Potion",      _bottleType);
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

    private static ContainerType MakeType(string typeName)
    {
        var t = ScriptableObject.CreateInstance<ContainerType>();
        t.name = typeName;
        return t;
    }

    private static ContainerDefinition MakeContainer(string displayName, ContainerType type, int capacity)
    {
        var d = ScriptableObject.CreateInstance<ContainerDefinition>();
        d.displayName = displayName;
        d.acceptedTypes.Add(type);
        d.capacity = capacity;
        return d;
    }

    private static Item MakeItem(string displayName, ContainerType type, int maxStackSize = 1)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.displayName = displayName;
        if (type != null)
            item.compatibleContainerTypes.Add(type);
        item.maxStackSize = maxStackSize;
        return item;
    }
}
