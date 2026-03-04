using System;
using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// A runtime container that holds items according to its ContainerDefinition.
    /// Capacity mode and type acceptance are controlled by the ContainerDefinition.
    /// </summary>
    public class Container
    {
        public ContainerDefinition definition;

        /// <summary>
        /// Raised after any successful mutation (TryAdd or TryRemove).
        /// Subscribe to this for per-container UI (e.g. a bomb counter tied to a specific bag).
        /// </summary>
        public event Action OnChanged;

        private readonly List<ItemStack> _stacks = new List<ItemStack>();

        public IReadOnlyList<ItemStack> Stacks => _stacks;

        public int UsedCapacity
        {
            get
            {
                if (definition.capacityMode == ContainerCapacityMode.Slots)
                    return _stacks.Count;
                int used = 0;
                foreach (var stack in _stacks)
                    used += stack.quantity;
                return used;
            }
        }

        public int RemainingCapacity => definition.capacity - UsedCapacity;

        public bool CanAdd(Item item, int quantity = 1)
        {
            bool typeOk;
            if (definition.acceptsAllTypes)
                typeOk = true;
            else if (item.compatibleContainerTypes.Count == 0)
                typeOk = false;
            else
            {
                typeOk = false;
                foreach (var type in item.compatibleContainerTypes)
                {
                    if (definition.acceptedTypes.Contains(type))
                    {
                        typeOk = true;
                        break;
                    }
                }
            }
            if (!typeOk) return false;

            if (definition.capacityMode == ContainerCapacityMode.TotalItems)
                return RemainingCapacity >= quantity;

            // Slots mode: predict how many new stack slots would be opened
            int remaining = quantity;
            foreach (var stack in _stacks)
            {
                if (stack.item != item) continue;
                remaining -= item.maxStackSize - stack.quantity;
                if (remaining <= 0) return true;
            }
            int newSlotsNeeded = Mathf.CeilToInt((float)remaining / item.maxStackSize);
            return _stacks.Count + newSlotsNeeded <= definition.capacity;
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
                if (remaining == 0) break;
            }

            // Open new stacks for the remainder
            while (remaining > 0)
            {
                int toAdd = Mathf.Min(item.maxStackSize, remaining);
                _stacks.Add(new ItemStack(item, toAdd));
                remaining -= toAdd;
            }

            OnChanged?.Invoke();
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

            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Moves <paramref name="quantity"/> of <paramref name="item"/> from this container into <paramref name="target"/>.
        /// Atomic — nothing changes if either side cannot complete the operation.
        /// </summary>
        public bool TryMoveTo(Container target, Item item, int quantity = 1)
        {
            if (GetQuantity(item) < quantity) return false;
            if (!target.CanAdd(item, quantity)) return false;
            TryRemove(item, quantity);
            target.TryAdd(item, quantity);
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
