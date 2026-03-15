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

        // ── Type Check ───────────────────────────────────────────────────────

        private bool IsTypeCompatible(Item item)
        {
            if (definition.acceptsAllTypes) return true;
            if (item.compatibleContainerTypes.Count == 0) return false;
            foreach (var type in item.compatibleContainerTypes)
                if (definition.acceptedTypes.Contains(type))
                    return true;
            return false;
        }

        // ── Capacity Queries ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the maximum quantity of <paramref name="item"/> that can currently be added to this container.
        /// Returns 0 if the item type is incompatible.
        /// </summary>
        public int HowManyCanAdd(Item item)
        {
            if (!IsTypeCompatible(item)) return 0;

            if (definition.capacityMode == ContainerCapacityMode.CountLimited)
                return RemainingCapacity;

            // Slots mode: space in existing stacks + space in free slots
            int spaceInExisting = 0;
            foreach (var stack in _stacks)
                if (stack.item == item)
                    spaceInExisting += item.maxStackSize - stack.quantity;
            int freeSlots = definition.capacity - _stacks.Count;
            return spaceInExisting + freeSlots * item.maxStackSize;
        }

        /// <summary>Returns true if <paramref name="quantity"/> of <paramref name="item"/> can fit in this container.</summary>
        public bool CanAdd(Item item, int quantity = 1) => HowManyCanAdd(item) >= quantity;

        // ── Item Operations ──────────────────────────────────────────────────

        /// <summary>Adds <paramref name="quantity"/> of <paramref name="item"/>. Returns false if it cannot fit.</summary>
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

        /// <summary>
        /// Adds <paramref name="quantity"/> of <paramref name="item"/>, reporting why it failed via <paramref name="result"/>.
        /// </summary>
        public bool TryAdd(Item item, int quantity, out AddResult result)
        {
            if (!IsTypeCompatible(item)) { result = AddResult.WrongType; return false; }
            if (HowManyCanAdd(item) < quantity) { result = AddResult.NoSpace; return false; }
            TryAdd(item, quantity);
            result = AddResult.Success;
            return true;
        }

        /// <summary>
        /// Adds as many of <paramref name="item"/> as possible up to <paramref name="quantity"/>.
        /// Returns the number actually added (0 if incompatible type).
        /// </summary>
        public int AddAsManyAsPossible(Item item, int quantity = 1)
        {
            int canAdd = Mathf.Min(HowManyCanAdd(item), quantity);
            if (canAdd <= 0) return 0;
            TryAdd(item, canAdd);
            return canAdd;
        }

        /// <summary>Removes <paramref name="quantity"/> of <paramref name="item"/>. Returns false if not enough present.</summary>
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
        /// Removes <paramref name="quantity"/> of <paramref name="item"/>, reporting why it failed via <paramref name="result"/>.
        /// </summary>
        public bool TryRemove(Item item, int quantity, out RemoveResult result)
        {
            if (GetQuantity(item) < quantity) { result = RemoveResult.NotEnough; return false; }
            TryRemove(item, quantity);
            result = RemoveResult.Success;
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

        /// <summary>Returns the total quantity of <paramref name="item"/> held in this container.</summary>
        public int GetQuantity(Item item)
        {
            int total = 0;
            foreach (var stack in _stacks)
                if (stack.item == item)
                    total += stack.quantity;
            return total;
        }

        /// <summary>Sorts all stacks in place using the provided <paramref name="comparison"/> and fires <see cref="OnChanged"/>.</summary>
        public void Sort(Comparison<ItemStack> comparison)
        {
            _stacks.Sort(comparison);
            OnChanged?.Invoke();
        }
    }
}
