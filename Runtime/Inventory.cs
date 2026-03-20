using System;
using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// MonoBehaviour that manages a collection of typed containers.
    /// Assign an <see cref="InventoryDefinition"/> in the Inspector to configure the initial
    /// container layout. Containers can also be added or removed at runtime.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private InventoryDefinition definition;

        private readonly List<Container> _containers = new List<Container>();
        private readonly Dictionary<ItemDefinition, int> _itemCounts = new Dictionary<ItemDefinition, int>();

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Raised after an item is successfully added. Reports the item and the quantity added.</summary>
        public event Action<ItemDefinition, int> OnItemAdded;

        /// <summary>Raised after an item is successfully removed. Reports the item and the quantity removed.</summary>
        public event Action<ItemDefinition, int> OnItemRemoved;

        /// <summary>Raised after a container is added at runtime.</summary>
        public event Action<Container> OnContainerAdded;

        /// <summary>Raised after a container is removed at runtime.</summary>
        public event Action<ContainerDefinition> OnContainerRemoved;

        /// <summary>Raised after an item is moved between two containers within this inventory. Reports the item, source container, and destination container.</summary>
        public event Action<ItemDefinition, Container, Container> OnItemMoved;

        private void Awake()
        {
            if (definition == null) return;
            foreach (var containerDef in definition.containers)
                _containers.Add(new Container { definition = containerDef });
        }

        // ── Container Management ─────────────────────────────────────────────

        /// <summary>All active containers (read-only view).</summary>
        public IReadOnlyList<Container> Containers => _containers;

        /// <summary>Returns the first container backed by <paramref name="definition"/>, or null.</summary>
        public Container GetContainer(ContainerDefinition definition)
            => _containers.Find(c => c.definition == definition);

        /// <summary>Adds a new container backed by <paramref name="definition"/> at runtime.</summary>
        public void AddContainer(ContainerDefinition definition)
        {
            var container = new Container { definition = definition };
            _containers.Add(container);
            OnContainerAdded?.Invoke(container);
        }

        /// <summary>
        /// Removes all containers backed by <paramref name="definition"/>.
        /// Items inside are discarded — transfer or migrate them first if you need to preserve them.
        /// Returns true if at least one container was removed.
        /// </summary>
        public bool RemoveContainer(ContainerDefinition definition)
        {
            bool removed = _containers.RemoveAll(c => c.definition == definition) > 0;
            if (removed) OnContainerRemoved?.Invoke(definition);
            return removed;
        }

        /// <summary>
        /// Replaces the container backed by <paramref name="oldDef"/> with a new one backed by
        /// <paramref name="newDef"/>, preserving items that fit in the new container.
        /// Returns any items that didn't fit — the caller is responsible for re-adding or discarding overflow.
        /// </summary>
        public List<ItemStack> SwapContainer(ContainerDefinition oldDef, ContainerDefinition newDef)
        {
            var old = GetContainer(oldDef);
            if (old == null) return new List<ItemStack>();

            var overflow = new List<ItemStack>();
            var replacement = new Container { definition = newDef };

            foreach (var stack in old.Stacks)
            {
                int added = replacement.AddAsManyAsPossible(stack.item, stack.quantity);
                int leftover = stack.quantity - added;
                if (leftover > 0)
                    overflow.Add(new ItemStack(stack.item, leftover));
            }

            _containers[_containers.IndexOf(old)] = replacement;

            foreach (var stack in overflow)
            {
                UpdateItemCount(stack.item, -stack.quantity);
                OnItemRemoved?.Invoke(stack.item, stack.quantity);
            }

            OnContainerAdded?.Invoke(replacement);
            OnContainerRemoved?.Invoke(oldDef);
            return overflow;
        }

        /// <summary>
        /// Moves all items from <paramref name="from"/> into <paramref name="to"/>, filling as many as possible.
        /// Both containers must belong to this inventory.
        /// Returns the number of items that could not be moved due to insufficient space in <paramref name="to"/>.
        /// Returns -1 if either container does not belong to this inventory.
        /// </summary>
        public int MigrateItems(Container from, Container to)
        {
            if (!_containers.Contains(from) || !_containers.Contains(to)) return -1;

            int failed = 0;
            var stacksCopy = new List<ItemStack>(from.Stacks);
            foreach (var stack in stacksCopy)
            {
                int toMove = Mathf.Min(stack.quantity, to.HowManyCanAdd(stack.item));
                if (toMove > 0)
                {
                    from.TryRemove(stack.item, toMove);
                    to.TryAdd(stack.item, toMove);
                    OnItemMoved?.Invoke(stack.item, from, to);
                }
                failed += stack.quantity - toMove;
            }
            return failed;
        }

        // ── Item Management ──────────────────────────────────────────────────

        /// <summary>Returns true if at least one container can accept <paramref name="quantity"/> of <paramref name="item"/>.</summary>
        public bool CanAddItem(ItemDefinition item, int quantity = 1)
        {
            foreach (var container in _containers)
                if (container.CanAdd(item, quantity))
                    return true;
            return false;
        }

        /// <summary>Returns the maximum quantity of <paramref name="item"/> that can currently be added across all containers.</summary>
        public int HowManyCanAdd(ItemDefinition item)
        {
            int total = 0;
            foreach (var container in _containers)
                total += container.HowManyCanAdd(item);
            return total;
        }

        /// <summary>Adds <paramref name="quantity"/> of <paramref name="item"/> to the first container that accepts it.</summary>
        public bool TryAddItem(ItemDefinition item, int quantity = 1)
        {
            if (!CanAddItem(item, quantity)) return false;
            foreach (var container in _containers)
            {
                if (container.TryAdd(item, quantity))
                {
                    UpdateItemCount(item, quantity);
                    OnItemAdded?.Invoke(item, quantity);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds <paramref name="quantity"/> of <paramref name="item"/>, reporting why it failed via <paramref name="result"/>.
        /// </summary>
        public bool TryAddItem(ItemDefinition item, int quantity, out AddResult result)
        {
            AddResult bestFailure = AddResult.WrongType;
            foreach (var container in _containers)
            {
                if (container.TryAdd(item, quantity, out result))
                {
                    UpdateItemCount(item, quantity);
                    OnItemAdded?.Invoke(item, quantity);
                    return true;
                }
                if (result == AddResult.NoSpace) bestFailure = AddResult.NoSpace;
            }
            result = bestFailure;
            return false;
        }

        /// <summary>
        /// Adds as many of <paramref name="item"/> as possible across all containers, up to <paramref name="quantity"/>.
        /// Returns the number actually added.
        /// </summary>
        public int AddAsManyAsPossible(ItemDefinition item, int quantity = 1)
        {
            int remaining = quantity;
            foreach (var container in _containers)
            {
                if (remaining == 0) break;
                remaining -= container.AddAsManyAsPossible(item, remaining);
            }
            int totalAdded = quantity - remaining;
            if (totalAdded > 0)
            {
                UpdateItemCount(item, totalAdded);
                OnItemAdded?.Invoke(item, totalAdded);
            }
            return totalAdded;
        }

        /// <summary>Removes <paramref name="quantity"/> of <paramref name="item"/> from the first container that has it.</summary>
        public bool TryRemoveItem(ItemDefinition item, int quantity = 1)
        {
            foreach (var container in _containers)
            {
                if (container.TryRemove(item, quantity))
                {
                    UpdateItemCount(item, -quantity);
                    OnItemRemoved?.Invoke(item, quantity);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes <paramref name="quantity"/> of <paramref name="item"/>, reporting why it failed via <paramref name="result"/>.
        /// </summary>
        public bool TryRemoveItem(ItemDefinition item, int quantity, out RemoveResult result)
        {
            foreach (var container in _containers)
            {
                if (container.TryRemove(item, quantity, out result))
                {
                    UpdateItemCount(item, -quantity);
                    OnItemRemoved?.Invoke(item, quantity);
                    return true;
                }
            }
            result = RemoveResult.NotEnough;
            return false;
        }

        /// <summary>
        /// Adds <paramref name="quantity"/> of <paramref name="item"/> directly into <paramref name="container"/>,
        /// which must belong to this inventory. Fires <see cref="OnItemAdded"/>.
        /// </summary>
        public bool TryAddToContainer(Container container, ItemDefinition item, int quantity = 1)
        {
            if (!_containers.Contains(container)) return false;
            if (!container.TryAdd(item, quantity)) return false;
            UpdateItemCount(item, quantity);
            OnItemAdded?.Invoke(item, quantity);
            return true;
        }

        /// <summary>
        /// Removes <paramref name="quantity"/> of <paramref name="item"/> directly from <paramref name="container"/>,
        /// which must belong to this inventory. Fires <see cref="OnItemRemoved"/>.
        /// </summary>
        public bool TryRemoveFromContainer(Container container, ItemDefinition item, int quantity = 1)
        {
            if (!_containers.Contains(container)) return false;
            if (!container.TryRemove(item, quantity)) return false;
            UpdateItemCount(item, -quantity);
            OnItemRemoved?.Invoke(item, quantity);
            return true;
        }

        /// <summary>
        /// Moves <paramref name="quantity"/> of <paramref name="item"/> from <paramref name="from"/> to <paramref name="to"/>,
        /// both of which must belong to this inventory. Atomic — fires <see cref="OnItemMoved"/> on success.
        /// </summary>
        public bool TryMoveItem(Container from, Container to, ItemDefinition item, int quantity = 1)
        {
            if (!_containers.Contains(from) || !_containers.Contains(to)) return false;
            if (!from.TryMoveTo(to, item, quantity)) return false;
            OnItemMoved?.Invoke(item, from, to);
            return true;
        }

        /// <summary>
        /// Returns all item stacks across all containers, optionally filtered by <paramref name="filter"/>.
        /// </summary>
        public IEnumerable<ItemStack> GetItems(Func<ItemDefinition, bool> filter = null)
        {
            foreach (var container in _containers)
                foreach (var stack in container.Stacks)
                    if (filter == null || filter(stack.item))
                        yield return stack;
        }

        /// <summary>Returns the total count of <paramref name="item"/> across all containers.</summary>
        public int GetItemCount(ItemDefinition item)
        {
            _itemCounts.TryGetValue(item, out int count);
            return count;
        }

        // ── Transfers ────────────────────────────────────────────────────────

        /// <summary>
        /// Transfers <paramref name="quantity"/> of <paramref name="item"/> from this inventory into <paramref name="target"/>.
        /// Atomic — nothing changes if this inventory lacks the item or <paramref name="target"/> cannot accept it.
        /// </summary>
        public bool TryTransferTo(Inventory target, ItemDefinition item, int quantity = 1)
        {
            if (GetItemCount(item) < quantity) return false;
            if (!target.CanAddItem(item, quantity)) return false;
            TryRemoveItem(item, quantity);
            target.TryAddItem(item, quantity);
            return true;
        }

        /// <summary>
        /// Transfers <paramref name="quantity"/> of <paramref name="item"/> from <paramref name="source"/> into this inventory.
        /// Convenience wrapper — delegates to <see cref="TryTransferTo"/>.
        /// </summary>
        public bool TryTransferFrom(Inventory source, ItemDefinition item, int quantity = 1)
            => source.TryTransferTo(this, item, quantity);

        /// <summary>
        /// Transfers all of <paramref name="item"/> (or all items if null) to <paramref name="target"/>.
        /// Returns the total quantity moved.
        /// </summary>
        public int TryTransferAll(Inventory target, ItemDefinition item = null)
        {
            int totalMoved = 0;
            if (item != null)
            {
                int count = GetItemCount(item);
                if (count > 0 && TryTransferTo(target, item, count))
                    totalMoved = count;
                return totalMoved;
            }

            var items = new HashSet<ItemDefinition>();
            foreach (var container in _containers)
                foreach (var stack in container.Stacks)
                    items.Add(stack.item);

            foreach (var i in items)
            {
                int count = GetItemCount(i);
                if (count > 0 && TryTransferTo(target, i, count))
                    totalMoved += count;
            }
            return totalMoved;
        }

        // ── Persistence ──────────────────────────────────────────────────────

        /// <summary>
        /// Captures the current inventory state as a serializable snapshot.
        /// See <see cref="InventorySnapshot"/> for cross-session persistence notes.
        /// </summary>
        public InventorySnapshot Save()
        {
            var snapshot = new InventorySnapshot();
            foreach (var container in _containers)
            {
                var cs = new InventorySnapshot.ContainerSnapshot { definition = container.definition };
                foreach (var stack in container.Stacks)
                    cs.stacks.Add(new ItemStack(stack.item, stack.quantity));
                snapshot.containers.Add(cs);
            }
            return snapshot;
        }

        /// <summary>
        /// Restores inventory state from <paramref name="snapshot"/>, replacing all current contents.
        /// Does not fire events — callers should refresh UI after loading.
        /// </summary>
        public void Load(InventorySnapshot snapshot)
        {
            _containers.Clear();
            _itemCounts.Clear();

            foreach (var cs in snapshot.containers)
            {
                var container = new Container { definition = cs.definition };
                foreach (var stack in cs.stacks)
                {
                    container.TryAdd(stack.item, stack.quantity);
                    UpdateItemCount(stack.item, stack.quantity);
                }
                _containers.Add(container);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void UpdateItemCount(ItemDefinition item, int delta)
        {
            _itemCounts.TryGetValue(item, out int current);
            int newCount = current + delta;
            if (newCount <= 0)
                _itemCounts.Remove(item);
            else
                _itemCounts[item] = newCount;
        }
    }
}
