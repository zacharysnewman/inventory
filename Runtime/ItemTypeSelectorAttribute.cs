using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Apply to a string field to render it as a dropdown populated from the project's
    /// <see cref="InventoryConfig"/> asset in the Unity Inspector.
    /// </summary>
    public class ItemTypeSelectorAttribute : PropertyAttribute { }
}
