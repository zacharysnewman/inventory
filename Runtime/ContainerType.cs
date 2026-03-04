using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Defines a type of container (e.g. Bomb Bag, Quiver, Wallet).
    /// Create instances via Assets > Create > Inventory > Container Type.
    /// Items reference a ContainerType to restrict which containers they can enter.
    /// </summary>
    [CreateAssetMenu(fileName = "NewContainerType", menuName = "Inventory/Container Type")]
    public class ContainerType : ScriptableObject
    {
    }
}
