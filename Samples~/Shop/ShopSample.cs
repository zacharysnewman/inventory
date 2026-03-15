using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates a shop/merchant workflow using the Inventory package.
///
/// Scenario: a general goods store that sells consumables and weapons.
/// Gold is modelled as a regular item stored in a wallet container.
///
/// Features shown:
///   • Manual purchase pattern — TryRemoveItem(gold) + TryAddItem(item) keeps purchasing
///     logic in user space, where costs and rules belong
///   • Sell-back helper — TryRemoveItem + TryAddItem(gold) models selling to a merchant
///   • "Buy max" pattern — HowManyCanAdd + GetItemCount to compute the largest
///     affordable and fittable quantity in one shot
///
/// Attach to the same GameObject as an Inventory component.
/// Leave the Inventory's ContainerDefinitions list empty in the Inspector;
/// this script builds everything in code for demonstration purposes.
/// In a real project, create ScriptableObject assets via the Asset menu instead.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class ShopSample : MonoBehaviour
{
    private Inventory _player;

    // ── Items ────────────────────────────────────────────────────────────────
    private Item _gold;           // currency item — stackSize 999, lives in wallet
    private Item _healthPotion;   //  10 gold — stackSize 5
    private Item _manaPotion;     //  15 gold — stackSize 5
    private Item _ironSword;      //  60 gold — stackSize 1

    // Merchant buys items back at half their sell price
    private const int SellDivisor = 2;

    private void Start()
    {
        _player = GetComponent<Inventory>();
        CreateDefinitions();
        RunDemo();
    }

    // ── Demo ─────────────────────────────────────────────────────────────────

    private void RunDemo()
    {
        Debug.Log("=== Shop Sample ===\n");

        // Player has a 10-item backpack and a wallet holding 150 gold
        _player.AddContainer(MakeContainer("Backpack", capacity: 10));
        _player.AddContainer(MakeContainer("Wallet",   capacity: 999));
        _player.TryAddItem(_gold, 150);

        LogState("Starting state");

        // ── Normal purchases ──────────────────────────────────────────────────
        TryBuy(_healthPotion, 3, goldCost: 10);  // −30 gold
        TryBuy(_manaPotion,   2, goldCost: 15);  // −30 gold
        TryBuy(_ironSword,    1, goldCost: 60);  // −60 gold

        LogState("After initial purchases (120 gold spent)");

        // ── Cannot afford ─────────────────────────────────────────────────────
        // 30 gold remaining; iron sword costs 60 — should fail
        TryBuy(_ironSword, 1, goldCost: 60);

        LogState("After failed sword purchase");

        // ── Sell items back at half price ─────────────────────────────────────
        SellItem(_healthPotion, 2, goldCost: 10);  // +10 gold
        SellItem(_ironSword,    1, goldCost: 60);  // +30 gold

        LogState("After selling 2 potions and the sword");

        // ── Buy as many health potions as gold and bag space allow ────────────
        int bought = BuyAsManyAsPossible(_healthPotion, goldCost: 10);
        Debug.Log($"Bulk-buy Health Potions → bought {bought}");

        LogState("Final state");
    }

    // ── Shop helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Removes gold and adds the item if the player can afford it and has space.
    /// </summary>
    private void TryBuy(Item item, int quantity, int goldCost)
    {
        int total = goldCost * quantity;
        if (_player.GetItemCount(_gold) < total)
        {
            Debug.Log($"Buy {quantity}× {item.displayName} → failed (cannot afford {total} gold)");
            return;
        }
        if (!_player.CanAddItem(item, quantity))
        {
            Debug.Log($"Buy {quantity}× {item.displayName} → failed (no space)");
            return;
        }
        _player.TryRemoveItem(_gold, total);
        _player.TryAddItem(item, quantity);
        Debug.Log($"Buy {quantity}× {item.displayName} → success (−{total} gold)");
    }

    /// <summary>
    /// Removes <paramref name="quantity"/> of <paramref name="item"/> from the player's inventory
    /// and refunds half the item's gold cost. Models selling to a merchant.
    /// </summary>
    private void SellItem(Item item, int quantity, int goldCost)
    {
        int refund = goldCost * quantity / SellDivisor;
        if (_player.TryRemoveItem(item, quantity))
        {
            _player.TryAddItem(_gold, refund);
            Debug.Log($"Sold {quantity}× {item.displayName} → +{refund} gold");
        }
        else
        {
            Debug.Log($"Sell {quantity}× {item.displayName} → failed (not enough held)");
        }
    }

    /// <summary>
    /// Purchases as many of <paramref name="item"/> as the player can simultaneously afford
    /// and fit in their inventory. Returns the quantity purchased.
    /// </summary>
    private int BuyAsManyAsPossible(Item item, int goldCost)
    {
        int canFit    = _player.HowManyCanAdd(item);
        int canAfford = goldCost > 0 ? _player.GetItemCount(_gold) / goldCost : canFit;
        int qty       = Mathf.Min(canFit, canAfford);
        if (qty <= 0) return 0;
        _player.TryRemoveItem(_gold, goldCost * qty);
        _player.TryAddItem(item, qty);
        return qty;
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogState(string label)
    {
        Debug.Log($"── {label} ──");
        Debug.Log($"  Gold:           {_player.GetItemCount(_gold)}");
        Debug.Log($"  Health Potions: {_player.GetItemCount(_healthPotion)}");
        Debug.Log($"  Mana Potions:   {_player.GetItemCount(_manaPotion)}");
        Debug.Log($"  Iron Sword:     {_player.GetItemCount(_ironSword)}");
        Debug.Log("");
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private void CreateDefinitions()
    {
        _gold          = MakeItem("Gold",          stackSize: 999);
        _healthPotion  = MakeItem("Health Potion", stackSize: 5);
        _manaPotion    = MakeItem("Mana Potion",   stackSize: 5);
        _ironSword     = MakeItem("Iron Sword",    stackSize: 1);
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
