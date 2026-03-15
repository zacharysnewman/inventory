using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Defines an item that can be stored in an inventory container.
    /// Create instances via Assets > Create > Inventory > Item Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
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
