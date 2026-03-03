using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates a Call of Duty: Warzone / DMZ-style inventory using the Inventory package.
///
/// Container types and capacities:
///   Primary Weapon    — 1 slot  (ARs, LMGs, Snipers)
///   Secondary Weapon  — 1 slot  (SMGs, Shotguns, Pistols)
///   Tertiary Weapon   — 1 slot  (RPGs, Mortars) — LOCKED until Large Backpack is picked up
///   Lethal            — 1 slot  (Frag, Semtex)
///   Tactical          — 1 slot  (Smoke, Stun)
///   Armor Plates      — 5 slots (dedicated; looted or bought at buy station)
///   General Backpack  — 6 slots (acceptsAllTypes; weapons overflow here, general items only go here)
///
/// Currency: Cash — looted from eliminated players, spent at the buy station.
///
/// Container ordering matters: dedicated slots are added BEFORE the general backpack,
/// so TryAddItem fills dedicated slots first and only overflows to the backpack when they are full.
///
/// Type model:
///   Typed item   (requiredContainerType = PrimaryWeapon) → dedicated slot first, backpack as overflow
///   General item (requiredContainerType = null)          → backpack ONLY, never enters a dedicated slot
///
/// The demo shows:
///   1. Dropping in with dedicated slots + general backpack
///   2. Weapons filling dedicated slots first, then overflowing to the backpack
///   3. General items (med kit, contract tablet) accepted only by the backpack
///   4. Confirming a general item is rejected by a typed dedicated slot
///   5. Picking up a Large Backpack — unlocks the Tertiary slot
///   6. Visiting a buy station to purchase armor plates with cash
///
/// Attach to the same GameObject as an Inventory component.
/// Leave the Inventory's ContainerDefinitions list empty in the Inspector;
/// this script builds everything in code for demonstration purposes.
/// In a real project, create ScriptableObject assets via the Asset menu instead.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class WarzoneInventorySample : MonoBehaviour
{
    private Inventory _inventory;

    // ── Container Types ──────────────────────────────────────────────────────
    private ContainerType _primaryType;
    private ContainerType _secondaryType;
    private ContainerType _tertiaryType;   // gated behind Large Backpack
    private ContainerType _lethalType;
    private ContainerType _tacticalType;
    private ContainerType _armorPlateType;

    // ── Container Definitions ────────────────────────────────────────────────
    private ContainerDefinition _primarySlot;
    private ContainerDefinition _secondarySlot;
    private ContainerDefinition _tertiarySlot;     // added when Large Backpack is looted
    private ContainerDefinition _lethalSlot;
    private ContainerDefinition _tacticalSlot;
    private ContainerDefinition _armorPlatesSlot;  // dedicated; capacity = total items
    private ContainerDefinition _backpackSlot;     // general; acceptsAllTypes, capacity = slots

    // ── Currencies ───────────────────────────────────────────────────────────
    private Currency _cash;

    // ── Primary Weapons (typed) ───────────────────────────────────────────────
    private Item _assaultRifle;
    private Item _lmg;
    private Item _sniperRifle;

    // ── Secondary Weapons (typed) ─────────────────────────────────────────────
    private Item _smg;
    private Item _pistol;
    private Item _shotgun;

    // ── Tertiary Weapons (typed, need Large Backpack) ─────────────────────────
    private Item _rpg;
    private Item _mortar;

    // ── Lethal / Tactical (typed) ─────────────────────────────────────────────
    private Item _fragGrenade;
    private Item _semtex;
    private Item _smokeGrenade;
    private Item _stunGrenade;

    // ── Armor (typed) ─────────────────────────────────────────────────────────
    private Item _armorPlate;

    // ── General Items (null type — backpack only) ─────────────────────────────
    private Item _medKit;
    private Item _contractTablet;

    private void Start()
    {
        _inventory = GetComponent<Inventory>();
        CreateDefinitions();
        RunDemo();
    }

    // ── Demo ─────────────────────────────────────────────────────────────────

    private void RunDemo()
    {
        Debug.Log("=== Warzone / DMZ Inventory Demo ===\n");

        // ── Drop in ───────────────────────────────────────────────────────────
        // Dedicated slots first — they get priority in TryAddItem's container scan.
        // General backpack last — typed items overflow here when dedicated slots are full.
        _inventory.AddContainer(_primarySlot);
        _inventory.AddContainer(_secondarySlot);
        _inventory.AddContainer(_lethalSlot);
        _inventory.AddContainer(_tacticalSlot);
        _inventory.AddContainer(_armorPlatesSlot);
        _inventory.AddContainer(_backpackSlot);   // ← general slot, listed last

        _inventory.AddCurrency(_cash, 4500);

        // ── Loot first set of weapons — fill dedicated slots ──────────────────
        _inventory.TryAddItem(_assaultRifle);  // → primary slot
        _inventory.TryAddItem(_pistol);        // → secondary slot
        _inventory.TryAddItem(_fragGrenade);   // → lethal slot
        _inventory.TryAddItem(_smokeGrenade);  // → tactical slot
        _inventory.TryAddItem(_armorPlate, 3); // → armor plates slot

        LogState("After initial loot (dedicated slots filled)");

        // ── Overflow: dedicated slots full → weapons go to backpack ───────────
        // Primary slot is taken; a second primary-type weapon overflows to backpack.
        bool gotLmg = _inventory.TryAddItem(_lmg);
        Debug.Log($"Looted LMG (primary slot full) → backpack: {gotLmg}"); // true

        // Secondary slot is taken; SMG overflows to backpack.
        bool gotSmg = _inventory.TryAddItem(_smg);
        Debug.Log($"Looted SMG (secondary slot full) → backpack: {gotSmg}"); // true
        Debug.Log("");

        LogState("After overflow weapons");

        // ── General items: backpack only, dedicated slots reject them ─────────
        bool gotMedKit = _inventory.TryAddItem(_medKit);
        Debug.Log($"Picked up Med Kit: {gotMedKit}"); // true — goes to backpack

        bool gotTablet = _inventory.TryAddItem(_contractTablet);
        Debug.Log($"Picked up Contract Tablet: {gotTablet}"); // true — goes to backpack

        // Confirm a general item cannot enter a dedicated typed slot directly.
        var primaryContainer = _inventory.GetContainer(_primarySlot);
        bool medKitInPrimary = primaryContainer != null && primaryContainer.CanAdd(_medKit);
        Debug.Log($"Can Med Kit enter Primary slot: {medKitInPrimary}"); // false
        Debug.Log("");

        LogState("After general items");

        // ── Pick up Large Backpack → unlocks tertiary slot ────────────────────
        Debug.Log("[Picked up Large Backpack]\n");
        _inventory.AddContainer(_tertiarySlot);

        bool gotRpg = _inventory.TryAddItem(_rpg);
        Debug.Log($"Picked up RPG → tertiary slot: {gotRpg}"); // true
        Debug.Log("");

        // ── Buy station ───────────────────────────────────────────────────────
        _inventory.TryPurchase(_armorPlate); // −$1,500
        _inventory.TryPurchase(_armorPlate); // −$1,500

        LogState("Final state");
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private void CreateDefinitions()
    {
        _primaryType    = MakeType("Primary");
        _secondaryType  = MakeType("Secondary");
        _tertiaryType   = MakeType("Tertiary");
        _lethalType     = MakeType("Lethal");
        _tacticalType   = MakeType("Tactical");
        _armorPlateType = MakeType("ArmorPlate");

        // Dedicated typed slots
        _primarySlot     = MakeContainer("Primary Weapon",   _primaryType,    1);
        _secondarySlot   = MakeContainer("Secondary Weapon", _secondaryType,  1);
        _tertiarySlot    = MakeContainer("Tertiary Weapon",  _tertiaryType,   1);
        _lethalSlot      = MakeContainer("Lethal",           _lethalType,     1);
        _tacticalSlot    = MakeContainer("Tactical",         _tacticalType,   1);
        _armorPlatesSlot = MakeContainer("Armor Plates",     _armorPlateType, 5);

        // General backpack — accepts any item type; capacity measured in slots
        _backpackSlot = ScriptableObject.CreateInstance<ContainerDefinition>();
        _backpackSlot.displayName   = "Backpack";
        _backpackSlot.acceptsAllTypes = true;
        _backpackSlot.capacity      = 6;
        _backpackSlot.capacityMode  = ContainerCapacityMode.Slots;

        _cash = ScriptableObject.CreateInstance<Currency>();
        _cash.displayName = "Cash";

        // Typed weapons — go in their dedicated slot, overflow to backpack
        _assaultRifle = MakeItem("Assault Rifle", _primaryType);
        _lmg          = MakeItem("LMG",           _primaryType);
        _sniperRifle  = MakeItem("Sniper Rifle",  _primaryType);
        _smg          = MakeItem("SMG",           _secondaryType);
        _pistol       = MakeItem("Pistol",        _secondaryType);
        _shotgun      = MakeItem("Shotgun",       _secondaryType);
        _rpg          = MakeItem("RPG",           _tertiaryType);
        _mortar       = MakeItem("Mortar",        _tertiaryType);
        _fragGrenade  = MakeItem("Frag Grenade",  _lethalType);
        _semtex       = MakeItem("Semtex",        _lethalType);
        _smokeGrenade = MakeItem("Smoke Grenade", _tacticalType);
        _stunGrenade  = MakeItem("Stun Grenade",  _tacticalType);
        _armorPlate   = MakeItem("Armor Plate",   _armorPlateType, (_cash, 1500));

        // General items (null type) — backpack only, never enter a dedicated slot
        _medKit         = MakeItem("Med Kit",          null);
        _contractTablet = MakeItem("Contract Tablet",  null);
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogState(string label)
    {
        var backpack = _inventory.GetContainer(_backpackSlot);
        int backpackUsed = backpack?.UsedCapacity ?? 0;

        Debug.Log($"── {label} ──");
        Debug.Log($"  Cash:          ${_inventory.GetCurrency(_cash)}");
        Debug.Log($"  Primary slot:  {Equipped(_assaultRifle, _lmg, _sniperRifle)}");
        Debug.Log($"  Secondary slot:{Equipped(_smg, _pistol, _shotgun)}");
        Debug.Log($"  Tertiary slot: {Equipped(_rpg, _mortar)}");
        Debug.Log($"  Lethal:        {Equipped(_fragGrenade, _semtex)}");
        Debug.Log($"  Tactical:      {Equipped(_smokeGrenade, _stunGrenade)}");
        Debug.Log($"  Armor Plates:  {_inventory.GetItemCount(_armorPlate)}/5");
        Debug.Log($"  Backpack:      {backpackUsed}/6 slots used");
        Debug.Log("");
    }

    private string Equipped(params Item[] items)
    {
        foreach (var item in items)
            if (_inventory.GetItemCount(item) > 0)
                return item.displayName;
        return "none";
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
