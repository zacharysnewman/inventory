using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Defines an item that can be stored in an inventory container.
    /// Create instances via Assets > Create > Inventory > Item.
    /// Each item declares which ContainerType it belongs in and what it costs to acquire.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        public string displayName;

        [Tooltip("Restricts which containers can hold this item.\n\n" +
                 "• Typed (e.g. Quiver) — goes in matching typed containers OR any acceptsAllTypes container (overflow).\n" +
                 "• Null — general item; only goes in acceptsAllTypes containers, never in dedicated typed slots.")]
        public ContainerType requiredContainerType;

        [Tooltip("Maximum number of this item that can occupy a single stack slot.")]
        public int maxStackSize = 1;

        [Tooltip("Currencies consumed when this item is purchased via Inventory.TryPurchase.")]
        public List<CurrencyAmount> cost = new List<CurrencyAmount>();
    }
}
