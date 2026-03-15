using System;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// A runtime grouping of identical items within a container slot.
    /// Quantity is capped at ItemDefinition.maxStackSize by the Container that owns it.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        public ItemDefinition item;
        public int quantity;

        public ItemStack(ItemDefinition item, int quantity = 1)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }
}
