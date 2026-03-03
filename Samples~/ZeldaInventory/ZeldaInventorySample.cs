using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates a Zelda-style inventory using the Inventory package.
///
/// Container types and sizes:
///   Bomb Bag  — Small (10), Large (20), Biggest (30)
///   Quiver    — Small (30), Large (40), Biggest (50)
///   Wallet    — Small (99 rupees max), Large (200), Giant (500)
///   Equipment — Sword slot, Bow slot (capacity 1 each)
///   Hearts    — collectible heart containers (max 20)
///   Bottle    — 4 bottle slots for potions
///
/// Currency: Rupees — consumed when purchasing bombs, arrows, or potions.
///
/// The demo shows:
///   1. Starting with a small bomb bag, small quiver, and small wallet
///   2. Looting the Master Sword and Hero's Bow
///   3. Buying bombs, arrows, and a potion at a shop
///   4. Upgrading the bomb bag (small → large), transferring existing bombs
///   5. Earning a heart container after defeating a boss
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

    // ── Container Definitions ────────────────────────────────────────────────

    // Bomb bags — player starts with Small, can upgrade at a shop
    private ContainerDefinition _smallBombBag;    // 10 bombs
    private ContainerDefinition _largeBombBag;    // 20 bombs
    private ContainerDefinition _biggestBombBag;  // 30 bombs

    // Quivers — upgradeable the same way
    private ContainerDefinition _smallQuiver;     // 30 arrows
    private ContainerDefinition _largeQuiver;     // 40 arrows
    private ContainerDefinition _biggestQuiver;   // 50 arrows

    // Wallets — upgrading raises the rupee cap enforced by the container
    private ContainerDefinition _smallWallet;     //  99 rupees max (unused beyond capacity demo)
    private ContainerDefinition _largeWallet;     // 200 rupees max
    private ContainerDefinition _giantWallet;     // 500 rupees max

    // Equipment — one slot per item type; capacity 1 means "equipped or not"
    private ContainerDefinition _swordSlot;
    private ContainerDefinition _bowSlot;

    // Hearts — each Heart Container item = one heart; max 20
    private ContainerDefinition _heartSlot;

    // Bottles — up to 4 potions stored at once
    private ContainerDefinition _bottleSlots;

    // ── Currencies ───────────────────────────────────────────────────────────
    private Currency _rupees;

    // ── Items ────────────────────────────────────────────────────────────────
    private Item _bomb;
    private Item _arrow;
    private Item _masterSword;
    private Item _heroBow;
    private Item _heartContainer;
    private Item _bluePotion;   //  50 rupees — restores some magic/health
    private Item _redPotion;    // 150 rupees — fully restores health

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
        _inventory.AddContainer(_smallWallet);
        _inventory.AddContainer(_swordSlot);
        _inventory.AddContainer(_bowSlot);
        _inventory.AddContainer(_heartSlot);
        _inventory.AddContainer(_bottleSlots);

        // Start with 3 heart containers and some rupees
        _inventory.TryAddItem(_heartContainer, 3);
        _inventory.AddCurrency(_rupees, 200);

        LogState("Start of adventure");

        // ── Visit a shop ─────────────────────────────────────────────────────
        _inventory.TryPurchase(_bomb, 5);    // −50 rupees
        _inventory.TryPurchase(_arrow, 10);  // −50 rupees
        _inventory.TryPurchase(_bluePotion); // −50 rupees

        LogState("After shopping");

        // ── Loot weapons from a dungeon chest ────────────────────────────────
        _inventory.TryAddItem(_masterSword);
        _inventory.TryAddItem(_heroBow);

        // ── Upgrade the bomb bag ──────────────────────────────────────────────
        // Transfer current bombs before swapping so they are not lost
        var oldBag = _inventory.GetContainer(_smallBombBag);
        int carriedBombs = oldBag?.GetQuantity(_bomb) ?? 0;
        _inventory.RemoveContainer(_smallBombBag);
        _inventory.AddContainer(_largeBombBag);
        if (carriedBombs > 0)
            _inventory.TryAddItem(_bomb, carriedBombs); // restore transferred bombs

        LogState("After upgrading to Large Bomb Bag");

        // ── Defeat a boss — earn a heart container ────────────────────────────
        _inventory.TryAddItem(_heartContainer);

        LogState("After boss reward");
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private void CreateDefinitions()
    {
        _bombBagType   = MakeType("BombBag");
        _quiverType    = MakeType("Quiver");
        _equipmentType = MakeType("Equipment");
        _heartType     = MakeType("Heart");
        _bottleType    = MakeType("Bottle");

        _smallBombBag   = MakeContainer("Small Bomb Bag",   _bombBagType,  10);
        _largeBombBag   = MakeContainer("Large Bomb Bag",   _bombBagType,  20);
        _biggestBombBag = MakeContainer("Biggest Bomb Bag", _bombBagType,  30);

        _smallQuiver   = MakeContainer("Small Quiver",   _quiverType, 30);
        _largeQuiver   = MakeContainer("Large Quiver",   _quiverType, 40);
        _biggestQuiver = MakeContainer("Biggest Quiver", _quiverType, 50);

        _smallWallet = MakeContainer("Small Wallet", _equipmentType,  99);
        _largeWallet = MakeContainer("Large Wallet", _equipmentType, 200);
        _giantWallet = MakeContainer("Giant Wallet", _equipmentType, 500);

        _swordSlot   = MakeContainer("Sword Slot",   _equipmentType, 1);
        _bowSlot     = MakeContainer("Bow Slot",     _equipmentType, 1);
        _heartSlot   = MakeContainer("Heart Track",  _heartType,    20);
        _bottleSlots = MakeContainer("Bottle Slots", _bottleType,    4);

        _rupees = ScriptableObject.CreateInstance<Currency>();
        _rupees.displayName = "Rupees";

        _bomb          = MakeItem("Bomb",           _bombBagType,   (_rupees,  10));
        _arrow         = MakeItem("Arrow",          _quiverType,    (_rupees,   5));
        _masterSword   = MakeItem("Master Sword",   _equipmentType);
        _heroBow       = MakeItem("Hero's Bow",     _equipmentType);
        _heartContainer = MakeItem("Heart Container", _heartType);
        _bluePotion    = MakeItem("Blue Potion",    _bottleType,    (_rupees,  50));
        _redPotion     = MakeItem("Red Potion",     _bottleType,    (_rupees, 150));
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogState(string label)
    {
        Debug.Log($"── {label} ──");
        Debug.Log($"  Rupees:      {_inventory.GetCurrency(_rupees)}");
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

    private static Item MakeItem(string displayName, ContainerType type,
        (Currency currency, int amount) cost = default)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.displayName = displayName;
        if (type != null)
            item.requiredContainerTypes.Add(type);
        item.maxStackSize = 1;
        if (cost.currency != null)
            item.cost.Add(new CurrencyAmount { currency = cost.currency, amount = cost.amount });
        return item;
    }
}
