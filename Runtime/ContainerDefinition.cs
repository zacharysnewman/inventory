using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// How a container measures its capacity.
    /// </summary>
    public enum ContainerCapacityMode
    {
        /// <summary>Capacity = total item count across all stacks (default). Good for typed containers like bomb bags and quivers.</summary>
        CountLimited,
        /// <summary>Capacity = number of occupied stack slots. Good for general-purpose grid inventories where a stack of 30 arrows takes one slot, not 30.</summary>
        Slots,
    }

    /// <summary>
    /// Defines the configuration for a container: its display name, accepted item types, capacity, and capacity mode.
    /// Create instances via Assets > Create > Inventory > Container Definition.
    /// Attach multiple definitions to an Inventory to give it different container slots.
    /// </summary>
    [CreateAssetMenu(fileName = "NewContainerDefinition", menuName = "Inventory/Container Definition")]
    public class ContainerDefinition : ScriptableObject
    {
        public string displayName;

        [Tooltip("The item types this container accepts. Ignored when acceptsAllTypes is true.")]
        public List<string> acceptedTypes = new List<string>();

        [Tooltip("When true, this container accepts any item regardless of its itemType. Use for general-purpose backpacks and grid inventories.")]
        public bool acceptsAllTypes = false;

        public int capacity;

        [Tooltip("CountLimited: capacity is the sum of all item quantities (good for ammo bags). Slots: capacity is the number of distinct stack slots (good for grid inventories).")]
        public ContainerCapacityMode capacityMode = ContainerCapacityMode.CountLimited;
    }
}
