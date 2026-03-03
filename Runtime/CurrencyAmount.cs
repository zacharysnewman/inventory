using System;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// A currency paired with an amount. Used to express item costs and currency rewards.
    /// Serializable so it appears in the Unity Inspector on Item assets.
    /// </summary>
    [Serializable]
    public class CurrencyAmount
    {
        public Currency currency;
        public int amount;
    }
}
