using System;
using System.Collections.Generic;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// A serializable point-in-time snapshot of an Inventory's state.
    /// Use <see cref="Inventory.Save"/> to capture and <see cref="Inventory.Load"/> to restore.
    ///
    /// Container and ItemDefinition fields are ScriptableObject asset references.
    /// Unity's built-in serialization handles these correctly at edit-time and within a session.
    /// For cross-session file persistence, use a serializer that resolves ScriptableObjects
    /// by GUID (e.g. Addressables or a custom mapping layer) rather than JsonUtility.
    /// </summary>
    [Serializable]
    public class InventorySnapshot
    {
        [Serializable]
        public class ContainerSnapshot
        {
            public ContainerDefinition definition;
            public List<ItemStack> stacks = new List<ItemStack>();
        }

        public List<ContainerSnapshot> containers = new List<ContainerSnapshot>();
    }
}
