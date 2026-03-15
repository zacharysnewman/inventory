using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Project-wide inventory configuration asset.
    /// Create one instance via Assets > Create > Inventory > Config and add your item types.
    /// The custom inspectors on Item and ContainerDefinition will read from this asset
    /// to populate type dropdowns.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "Inventory/Config")]
    public class InventoryConfig : ScriptableObject
    {
        [Tooltip("All item type names available in this project. " +
                 "Add a name here to make it selectable in Item and ContainerDefinition inspectors.")]
        public List<string> itemTypes = new List<string>();
    }
}
