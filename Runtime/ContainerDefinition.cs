using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Defines the configuration for a container: its type, display name, and maximum capacity.
    /// Create instances via Assets > Create > Inventory > Container Definition.
    /// Attach multiple definitions to an Inventory to give it different container slots.
    /// </summary>
    [CreateAssetMenu(fileName = "NewContainerDefinition", menuName = "Inventory/Container Definition")]
    public class ContainerDefinition : ScriptableObject
    {
        public string displayName;
        public ContainerType type;
        public int capacity;
    }
}
