using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates a Call of Duty: Warzone-style inventory using the Inventory package.
///
/// Container types and capacities:
///   Primary Weapon    — 1 slot  (ARs, LMGs, Snipers)
///   Secondary Weapon  — 1 slot  (SMGs, Shotguns, Pistols)
///   Tertiary Weapon   — 1 slot  (RPGs, Mortars) — LOCKED until Large Backpack is picked up
///   Lethal            — 1 slot  (Frag, Semtex)
///   Tactical          — 1 slot  (Smoke, Stun)
///   Armor Plates      — 5 slots (looted or bought at buy station)
///
/// Currency: Cash — looted from eliminated players, spent at the buy station.
///
/// The demo shows:
///   1. Dropping in with primary + secondary + equipment slots only
///   2. Looting weapons and equipment from the ground
///   3. Attempting to pick up a Tertiary weapon (RPG) — fails without a backpack
///   4. Picking up a Large Backpack — unlocks the Tertiary slot
///   5. Picking up the RPG successfully
///   6. Visiting a buy station to purchase armor plates
///   7. Swapping a primary weapon mid-game
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
    private ContainerDefinition _tertiarySlot;    // added when Large Backpack is looted
    private ContainerDefinition _lethalSlot;
    private ContainerDefinition _tacticalSlot;
    private ContainerDefinition _armorPlatesSlot; // up to 5 plates

    // ── Currencies ───────────────────────────────────────────────────────────
    private Currency _cash;

    // ── Primary Weapons ───────────────────────────────────────────────────────
    private Item _assaultRifle;
    private Item _lmg;
    private Item _sniperRifle;

    // ── Secondary Weapons ─────────────────────────────────────────────────────
    private Item _smg;
    private Item _pistol;
    private Item _shotgun;

    // ── Tertiary Weapons (need Large Backpack) ────────────────────────────────
    private Item _rpg;
    private Item _mortar;

    // ── Lethal Equipment ──────────────────────────────────────────────────────
    private Item _fragGrenade;
    private Item _semtex;

    // ── Tactical Equipment ────────────────────────────────────────────────────
    private Item _smokeGrenade;
    private Item _stunGrenade;

    // ── Armor ─────────────────────────────────────────────────────────────────
    private Item _armorPlate; // looted free or bought at buy station for $1,500

    private void Start()
    {
        _inventory = GetComponent<Inventory>();
        CreateDefinitions();
        RunDemo();
    }

    // ── Demo ─────────────────────────────────────────────────────────────────

    private void RunDemo()
    {
        Debug.Log("=== Warzone Inventory Demo ===\n");

        // ── Drop in — no tertiary slot yet ───────────────────────────────────
        _inventory.AddContainer(_primarySlot);
        _inventory.AddContainer(_secondarySlot);
        _inventory.AddContainer(_lethalSlot);
        _inventory.AddContainer(_tacticalSlot);
        _inventory.AddContainer(_armorPlatesSlot);

        // Loot weapons and gear from the ground (no cost)
        _inventory.TryAddItem(_assaultRifle);
        _inventory.TryAddItem(_pistol);
        _inventory.TryAddItem(_fragGrenade);
        _inventory.TryAddItem(_smokeGrenade);
        _inventory.TryAddItem(_armorPlate, 3);

        // Looted $4,500 from an eliminated player
        _inventory.AddCurrency(_cash, 4500);

        LogState("After initial loot");

        // ── Try to pick up an RPG — fails without the tertiary slot ──────────
        bool gotRpg = _inventory.TryAddItem(_rpg);
        Debug.Log($"Picked up RPG (no backpack): {gotRpg}"); // false
        Debug.Log("");

        // ── Loot a Large Backpack — unlocks the tertiary slot ─────────────────
        Debug.Log("[Picked up Large Backpack]\n");
        _inventory.AddContainer(_tertiarySlot);

        gotRpg = _inventory.TryAddItem(_rpg);
        Debug.Log($"Picked up RPG (large backpack): {gotRpg}"); // true
        Debug.Log("");

        LogState("After Large Backpack + RPG");

        // ── Buy Station — purchase 2 more armor plates ────────────────────────
        _inventory.TryPurchase(_armorPlate); // −$1,500
        _inventory.TryPurchase(_armorPlate); // −$1,500

        LogState("After buy station (2 armor plates)");

        // ── Swap primary: drop AR, pick up Sniper Rifle ───────────────────────
        _inventory.TryRemoveItem(_assaultRifle);
        _inventory.TryAddItem(_sniperRifle);

        LogState("After swapping AR → Sniper Rifle");
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

        _primarySlot     = MakeContainer("Primary Weapon",   _primaryType,    1);
        _secondarySlot   = MakeContainer("Secondary Weapon", _secondaryType,  1);
        _tertiarySlot    = MakeContainer("Tertiary Weapon",  _tertiaryType,   1);
        _lethalSlot      = MakeContainer("Lethal",           _lethalType,     1);
        _tacticalSlot    = MakeContainer("Tactical",         _tacticalType,   1);
        _armorPlatesSlot = MakeContainer("Armor Plates",     _armorPlateType, 5);

        _cash = ScriptableObject.CreateInstance<Currency>();
        _cash.displayName = "Cash";

        // Primary weapons — looted, no currency cost
        _assaultRifle = MakeItem("Assault Rifle", _primaryType);
        _lmg          = MakeItem("LMG",           _primaryType);
        _sniperRifle  = MakeItem("Sniper Rifle",  _primaryType);

        // Secondary weapons
        _smg     = MakeItem("SMG",     _secondaryType);
        _pistol  = MakeItem("Pistol",  _secondaryType);
        _shotgun = MakeItem("Shotgun", _secondaryType);

        // Tertiary weapons (require Large Backpack to carry)
        _rpg    = MakeItem("RPG",    _tertiaryType);
        _mortar = MakeItem("Mortar", _tertiaryType);

        // Lethal / Tactical
        _fragGrenade  = MakeItem("Frag Grenade",  _lethalType);
        _semtex       = MakeItem("Semtex",        _lethalType);
        _smokeGrenade = MakeItem("Smoke Grenade", _tacticalType);
        _stunGrenade  = MakeItem("Stun Grenade",  _tacticalType);

        // Armor plate — lootable free, or $1,500 each at the buy station
        _armorPlate = MakeItem("Armor Plate", _armorPlateType, (_cash, 1500));
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogState(string label)
    {
        Debug.Log($"── {label} ──");
        Debug.Log($"  Cash:          ${_inventory.GetCurrency(_cash)}");
        Debug.Log($"  Primary:       {Equipped(_assaultRifle, _lmg, _sniperRifle)}");
        Debug.Log($"  Secondary:     {Equipped(_smg, _pistol, _shotgun)}");
        Debug.Log($"  Tertiary:      {Equipped(_rpg, _mortar)}");
        Debug.Log($"  Lethal:        {Equipped(_fragGrenade, _semtex)}");
        Debug.Log($"  Tactical:      {Equipped(_smokeGrenade, _stunGrenade)}");
        Debug.Log($"  Armor Plates:  {_inventory.GetItemCount(_armorPlate)}/5");
        Debug.Log("");
    }

    /// Returns the display name of the first item from <paramref name="items"/> that is in the inventory.
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
        d.type = type;
        d.capacity = capacity;
        return d;
    }

    private static Item MakeItem(string displayName, ContainerType type,
        (Currency currency, int amount) cost = default)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.displayName = displayName;
        item.requiredContainerType = type;
        item.maxStackSize = 1;
        if (cost.currency != null)
            item.cost.Add(new CurrencyAmount { currency = cost.currency, amount = cost.amount });
        return item;
    }
}
