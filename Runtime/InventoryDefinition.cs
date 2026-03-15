using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Defines the initial container layout for an inventory.
    /// Create instances via Assets > Create > Inventory > Inventory Definition and assign
    /// to an <see cref="Inventory"/> component to share the same layout across multiple prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewInventoryDefinition", menuName = "Inventory/Inventory Definition")]
    public class InventoryDefinition : ScriptableObject
    {
        public List<ContainerDefinition> containers = new List<ContainerDefinition>();
    }
}
