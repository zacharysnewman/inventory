using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Demonstrates a shop/merchant workflow using the Inventory package.
///
/// Scenario: a general goods store that sells consumables, weapons, and one unique item.
///
/// Features shown:
///   • TryPurchase with out PurchaseResult — tells you exactly why a purchase failed
///     (CannotAfford vs NoSpace vs ExceedsGlobalLimit surfaced as NoSpace)
///   • Item.maxGlobalCount — the Blessed Amulet can only be owned once per inventory
///   • "Buy max" pattern — HowManyCanAdd + GetCurrency to compute the largest affordable quantity
///   • Sell-back helper — TryRemoveItem + TryAddCurrency models selling items to a merchant
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

    // ── Currencies ───────────────────────────────────────────────────────────
    private Currency _gold;

    // ── Items ────────────────────────────────────────────────────────────────
    private Item _healthPotion;   //  10 gold — stackSize 5
    private Item _manaPotion;     //  15 gold — stackSize 5
    private Item _ironSword;      //  60 gold — stackSize 1
    private Item _blessedAmulet;  // 120 gold — stackSize 1, maxGlobalCount 1 (unique)

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

        // Set up the player with a 10-item backpack and 150 gold
        _player.AddContainer(MakeContainer("Backpack", capacity: 10));
        _player.TryAddCurrency(_gold, 150);

        LogState("Starting state");

        // ── Normal purchases ──────────────────────────────────────────────────
        _player.TryPurchase(_healthPotion, 3, out PurchaseResult r1);  // −30 gold, succeeds
        Debug.Log($"Buy 3× Health Potion  → {r1}");

        _player.TryPurchase(_manaPotion, 2, out PurchaseResult r2);    // −30 gold, succeeds
        Debug.Log($"Buy 2× Mana Potion    → {r2}");

        _player.TryPurchase(_ironSword, 1, out PurchaseResult r3);     // −60 gold, succeeds
        Debug.Log($"Buy 1× Iron Sword     → {r3}");

        LogState("After initial purchases (90 gold spent)");

        // ── Cannot afford ─────────────────────────────────────────────────────
        // 30 gold remaining; Blessed Amulet costs 120 — should fail
        _player.TryPurchase(_blessedAmulet, 1, out PurchaseResult r4);
        Debug.Log($"Buy 1× Blessed Amulet → {r4}");  // CannotAfford

        LogState("After failed amulet purchase");

        // ── Earn gold then buy the unique amulet ──────────────────────────────
        _player.TryAddCurrency(_gold, 200);  // sold some loot to a different vendor

        _player.TryPurchase(_blessedAmulet, 1, out PurchaseResult r5);  // −120 gold
        Debug.Log($"Buy 1× Blessed Amulet → {r5}");  // Success

        // maxGlobalCount = 1 means CanAddItem returns false, surfaced as NoSpace
        _player.TryPurchase(_blessedAmulet, 1, out PurchaseResult r6);
        Debug.Log($"Buy 2nd Blessed Amulet → {r6}");  // NoSpace (ExceedsGlobalLimit)

        LogState("After buying unique amulet");

        // ── Sell items back at half price ─────────────────────────────────────
        SellItem(_healthPotion, 2);  // +10 gold (2 × 10 / 2)
        SellItem(_ironSword, 1);     // +30 gold (60 / 2)

        LogState("After selling 2 potions and the sword");

        // ── Buy as many health potions as gold and bag space allow ────────────
        int bought = BuyAsManyAsPossible(_healthPotion);
        Debug.Log($"Bulk-buy Health Potions → bought {bought}");

        LogState("Final state");
    }

    // ── Shop helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Removes <paramref name="quantity"/> of <paramref name="item"/> from the player's inventory
    /// and refunds half the item's gold cost. Models selling to a merchant.
    /// </summary>
    private void SellItem(Item item, int quantity)
    {
        int priceEach = item.cost.Count > 0 ? item.cost[0].amount : 0;
        int refund    = priceEach * quantity / SellDivisor;

        if (_player.TryRemoveItem(item, quantity))
        {
            _player.TryAddCurrency(_gold, refund);
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
    ///
    /// This models a "buy max" button. For items with multiple currency costs,
    /// extend the affordability check to cover each currency type.
    /// </summary>
    private int BuyAsManyAsPossible(Item item)
    {
        int costEach  = item.cost.Count > 0 ? item.cost[0].amount : 0;
        int canFit    = _player.HowManyCanAdd(item);
        int canAfford = costEach > 0 ? _player.GetCurrency(_gold) / costEach : canFit;
        int qty       = Mathf.Min(canFit, canAfford);
        if (qty <= 0) return 0;
        _player.TryPurchase(item, qty);
        return qty;
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogState(string label)
    {
        Debug.Log($"── {label} ──");
        Debug.Log($"  Gold:           {_player.GetCurrency(_gold)}");
        Debug.Log($"  Health Potions: {_player.GetItemCount(_healthPotion)}");
        Debug.Log($"  Mana Potions:   {_player.GetItemCount(_manaPotion)}");
        Debug.Log($"  Iron Sword:     {_player.GetItemCount(_ironSword)}");
        Debug.Log($"  Blessed Amulet: {_player.GetItemCount(_blessedAmulet)}");
        Debug.Log("");
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private void CreateDefinitions()
    {
        _gold = ScriptableObject.CreateInstance<Currency>();
        _gold.displayName = "Gold";

        _healthPotion  = MakeItem("Health Potion",  stackSize: 5, goldCost: 10);
        _manaPotion    = MakeItem("Mana Potion",    stackSize: 5, goldCost: 15);
        _ironSword     = MakeItem("Iron Sword",     stackSize: 1, goldCost: 60);
        _blessedAmulet = MakeItem("Blessed Amulet", stackSize: 1, goldCost: 120, maxGlobal: 1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ContainerDefinition MakeContainer(string displayName, int capacity)
    {
        var d = ScriptableObject.CreateInstance<ContainerDefinition>();
        d.displayName    = displayName;
        d.acceptsAllTypes = true;
        d.capacity       = capacity;
        return d;
    }

    private Item MakeItem(string displayName, int stackSize = 1, int goldCost = 0, int maxGlobal = 0)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.displayName    = displayName;
        item.maxStackSize   = stackSize;
        item.maxGlobalCount = maxGlobal;
        if (goldCost > 0)
            item.cost.Add(new CurrencyAmount { currency = _gold, amount = goldCost });
        return item;
    }
}
