using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Defines an item that can be stored in an inventory container.
    /// Create instances via Assets > Create > Inventory > Item.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        public string displayName;

        [Tooltip("Restricts which containers can hold this item. " +
                 "Empty = general item; only goes in acceptsAllTypes containers.")]
        [ItemTypeSelector]
        public string itemType;

        [Tooltip("Maximum number of this item that can occupy a single stack slot.")]
        public int maxStackSize = 1;
    }
}
