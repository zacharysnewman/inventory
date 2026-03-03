using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// A runtime container that holds items according to its ContainerDefinition.
    /// Capacity is measured in total item count across all stacks.
    /// Items are only accepted when their requiredContainerType matches this container's type.
    /// </summary>
    public class Container
    {
        public ContainerDefinition definition;

        private readonly List<ItemStack> _stacks = new List<ItemStack>();

        public IReadOnlyList<ItemStack> Stacks => _stacks;

        public int UsedCapacity
        {
            get
            {
                int used = 0;
                foreach (var stack in _stacks)
                    used += stack.quantity;
                return used;
            }
        }

        public int RemainingCapacity => definition.capacity - UsedCapacity;

        public bool CanAdd(Item item, int quantity = 1)
        {
            return item.requiredContainerType == definition.type
                && RemainingCapacity >= quantity;
        }

        public bool TryAdd(Item item, int quantity = 1)
        {
            if (!CanAdd(item, quantity))
                return false;

            int remaining = quantity;

            // Fill existing stacks that have room first
            foreach (var stack in _stacks)
            {
                if (stack.item != item) continue;
                int space = item.maxStackSize - stack.quantity;
                if (space <= 0) continue;
                int toAdd = Mathf.Min(space, remaining);
                stack.quantity += toAdd;
                remaining -= toAdd;
                if (remaining == 0) return true;
            }

            // Open new stacks for the remainder
            while (remaining > 0)
            {
                int toAdd = Mathf.Min(item.maxStackSize, remaining);
                _stacks.Add(new ItemStack(item, toAdd));
                remaining -= toAdd;
            }

            return true;
        }

        public bool TryRemove(Item item, int quantity = 1)
        {
            if (GetQuantity(item) < quantity)
                return false;

            int remaining = quantity;
            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_stacks[i].item != item) continue;
                int toRemove = Mathf.Min(_stacks[i].quantity, remaining);
                _stacks[i].quantity -= toRemove;
                remaining -= toRemove;
                if (_stacks[i].quantity == 0)
                    _stacks.RemoveAt(i);
            }

            return true;
        }

        public int GetQuantity(Item item)
        {
            int total = 0;
            foreach (var stack in _stacks)
                if (stack.item == item)
                    total += stack.quantity;
            return total;
        }
    }
}
