using System;
using System.Collections.Generic;
using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// MonoBehaviour that manages a collection of typed containers and a currency wallet.
    /// Assign ContainerDefinition assets in the Inspector to configure which containers
    /// (e.g. Bomb Bag, Quiver, Wallet) this inventory exposes at runtime.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private List<ContainerDefinition> containerDefinitions = new List<ContainerDefinition>();

        private readonly List<Container> _containers = new List<Container>();
        private readonly Dictionary<Currency, int> _currencies = new Dictionary<Currency, int>();
        private readonly Dictionary<Item, int> _itemCounts = new Dictionary<Item, int>();
        private readonly HashSet<Item> _lockedItems = new HashSet<Item>();

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Raised after an item is successfully added. Reports the item and its new global count.</summary>
        public event Action<Item, int> OnItemAdded;

        /// <summary>Raised after an item is successfully removed. Reports the item and its new global count.</summary>
        public event Action<Item, int> OnItemRemoved;

        /// <summary>Raised after any currency amount changes. Reports the currency and its new total.</summary>
        public event Action<Currency, int> OnCurrencyChanged;

        /// <summary>Raised after a container is added at runtime.</summary>
        public event Action<Container> OnContainerAdded;

        /// <summary>Raised after a container is removed at runtime.</summary>
        public event Action<ContainerDefinition> OnContainerRemoved;

        /// <summary>Raised after an item is moved between two containers within this inventory. Reports the item, source container, and destination container.</summary>
        public event Action<Item, Container, Container> OnItemMoved;

        private void Awake()
        {
            foreach (var def in containerDefinitions)
                _containers.Add(new Container { definition = def });
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
                OnItemRemoved?.Invoke(stack.item, GetItemCount(stack.item));
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

        /// <summary>
        /// Returns true if at least one container can accept <paramref name="quantity"/> of <paramref name="item"/>.
        /// Also enforces <see cref="Item.maxGlobalCount"/>.
        /// </summary>
        public bool CanAddItem(Item item, int quantity = 1)
        {
            if (item.maxGlobalCount > 0 && GetItemCount(item) + quantity > item.maxGlobalCount)
                return false;
            foreach (var container in _containers)
                if (container.CanAdd(item, quantity))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns the maximum quantity of <paramref name="item"/> that can currently be added across all containers.
        /// Accounts for <see cref="Item.maxGlobalCount"/>.
        /// </summary>
        public int HowManyCanAdd(Item item)
        {
            int total = 0;
            foreach (var container in _containers)
                total += container.HowManyCanAdd(item);
            if (item.maxGlobalCount > 0)
                return Mathf.Min(total, item.maxGlobalCount - GetItemCount(item));
            return total;
        }

        /// <summary>Adds <paramref name="quantity"/> of <paramref name="item"/> to the first container that accepts it.</summary>
        public bool TryAddItem(Item item, int quantity = 1)
        {
            if (!CanAddItem(item, quantity)) return false;
            foreach (var container in _containers)
            {
                if (container.TryAdd(item, quantity))
                {
                    UpdateItemCount(item, quantity);
                    OnItemAdded?.Invoke(item, GetItemCount(item));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds <paramref name="quantity"/> of <paramref name="item"/>, reporting why it failed via <paramref name="result"/>.
        /// </summary>
        public bool TryAddItem(Item item, int quantity, out AddResult result)
        {
            if (item.maxGlobalCount > 0 && GetItemCount(item) + quantity > item.maxGlobalCount)
            {
                result = AddResult.ExceedsGlobalLimit;
                return false;
            }
            AddResult bestFailure = AddResult.WrongType;
            foreach (var container in _containers)
            {
                if (container.TryAdd(item, quantity, out result))
                {
                    UpdateItemCount(item, quantity);
                    OnItemAdded?.Invoke(item, GetItemCount(item));
                    return true;
                }
                if (result == AddResult.NoSpace) bestFailure = AddResult.NoSpace;
            }
            result = bestFailure;
            return false;
        }

        /// <summary>
        /// Adds as many of <paramref name="item"/> as possible across all containers, up to <paramref name="quantity"/>.
        /// Respects <see cref="Item.maxGlobalCount"/>. Returns the number actually added.
        /// </summary>
        public int AddAsManyAsPossible(Item item, int quantity = 1)
        {
            if (item.maxGlobalCount > 0)
                quantity = Mathf.Min(quantity, item.maxGlobalCount - GetItemCount(item));
            if (quantity <= 0) return 0;

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
                OnItemAdded?.Invoke(item, GetItemCount(item));
            }
            return totalAdded;
        }

        /// <summary>Removes <paramref name="quantity"/> of <paramref name="item"/> from the first container that has it.</summary>
        public bool TryRemoveItem(Item item, int quantity = 1)
        {
            if (_lockedItems.Contains(item)) return false;
            foreach (var container in _containers)
            {
                if (container.TryRemove(item, quantity))
                {
                    UpdateItemCount(item, -quantity);
                    OnItemRemoved?.Invoke(item, GetItemCount(item));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes <paramref name="quantity"/> of <paramref name="item"/>, reporting why it failed via <paramref name="result"/>.
        /// </summary>
        public bool TryRemoveItem(Item item, int quantity, out RemoveResult result)
        {
            if (_lockedItems.Contains(item)) { result = RemoveResult.Locked; return false; }
            foreach (var container in _containers)
            {
                if (container.TryRemove(item, quantity, out result))
                {
                    UpdateItemCount(item, -quantity);
                    OnItemRemoved?.Invoke(item, GetItemCount(item));
                    return true;
                }
            }
            result = RemoveResult.NotEnough;
            return false;
        }

        /// <summary>
        /// Adds <paramref name="quantity"/> of <paramref name="item"/> directly into <paramref name="container"/>,
        /// which must belong to this inventory. Respects maxGlobalCount. Fires <see cref="OnItemAdded"/>.
        /// </summary>
        public bool TryAddToContainer(Container container, Item item, int quantity = 1)
        {
            if (!_containers.Contains(container)) return false;
            if (item.maxGlobalCount > 0 && GetItemCount(item) + quantity > item.maxGlobalCount) return false;
            if (!container.TryAdd(item, quantity)) return false;
            UpdateItemCount(item, quantity);
            OnItemAdded?.Invoke(item, GetItemCount(item));
            return true;
        }

        /// <summary>
        /// Removes <paramref name="quantity"/> of <paramref name="item"/> directly from <paramref name="container"/>,
        /// which must belong to this inventory. Respects item locks. Fires <see cref="OnItemRemoved"/>.
        /// </summary>
        public bool TryRemoveFromContainer(Container container, Item item, int quantity = 1)
        {
            if (_lockedItems.Contains(item)) return false;
            if (!_containers.Contains(container)) return false;
            if (!container.TryRemove(item, quantity)) return false;
            UpdateItemCount(item, -quantity);
            OnItemRemoved?.Invoke(item, GetItemCount(item));
            return true;
        }

        /// <summary>
        /// Moves <paramref name="quantity"/> of <paramref name="item"/> from <paramref name="from"/> to <paramref name="to"/>,
        /// both of which must belong to this inventory. Atomic — fires <see cref="OnItemMoved"/> on success.
        /// </summary>
        public bool TryMoveItem(Container from, Container to, Item item, int quantity = 1)
        {
            if (!_containers.Contains(from) || !_containers.Contains(to)) return false;
            if (!from.TryMoveTo(to, item, quantity)) return false;
            OnItemMoved?.Invoke(item, from, to);
            return true;
        }

        /// <summary>
        /// Returns all item stacks across all containers, optionally filtered by <paramref name="filter"/>.
        /// </summary>
        public IEnumerable<ItemStack> GetItems(Func<Item, bool> filter = null)
        {
            foreach (var container in _containers)
                foreach (var stack in container.Stacks)
                    if (filter == null || filter(stack.item))
                        yield return stack;
        }

        /// <summary>Returns the total count of <paramref name="item"/> across all containers.</summary>
        public int GetItemCount(Item item)
        {
            _itemCounts.TryGetValue(item, out int count);
            return count;
        }

        // ── Item Locking ─────────────────────────────────────────────────────

        /// <summary>Prevents <paramref name="item"/> from being removed until unlocked.</summary>
        public void LockItem(Item item) => _lockedItems.Add(item);

        /// <summary>Allows a previously locked <paramref name="item"/> to be removed again.</summary>
        public void UnlockItem(Item item) => _lockedItems.Remove(item);

        /// <summary>Returns true if <paramref name="item"/> is currently locked against removal.</summary>
        public bool IsLocked(Item item) => _lockedItems.Contains(item);

        // ── Transfers ────────────────────────────────────────────────────────

        /// <summary>
        /// Transfers <paramref name="quantity"/> of <paramref name="item"/> from this inventory into <paramref name="target"/>.
        /// Atomic — nothing changes if this inventory lacks the item or <paramref name="target"/> cannot accept it.
        /// </summary>
        public bool TryTransferTo(Inventory target, Item item, int quantity = 1)
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
        public bool TryTransferFrom(Inventory source, Item item, int quantity = 1)
            => source.TryTransferTo(this, item, quantity);

        /// <summary>
        /// Transfers all of <paramref name="item"/> (or all items if null) to <paramref name="target"/>.
        /// Returns the total quantity moved.
        /// </summary>
        public int TryTransferAll(Inventory target, Item item = null)
        {
            int totalMoved = 0;
            if (item != null)
            {
                int count = GetItemCount(item);
                if (count > 0 && TryTransferTo(target, item, count))
                    totalMoved = count;
                return totalMoved;
            }

            var items = new HashSet<Item>();
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

        // ── Currency Management ──────────────────────────────────────────────

        /// <summary>Returns the current amount of <paramref name="currency"/> held.</summary>
        public int GetCurrency(Currency currency)
        {
            _currencies.TryGetValue(currency, out int amount);
            return amount;
        }

        /// <summary>
        /// Adds <paramref name="amount"/> of <paramref name="currency"/>.
        /// Returns false and makes no change if the currency has a max and is already at capacity.
        /// If adding would exceed the max, the amount is clamped to the cap.
        /// </summary>
        public bool TryAddCurrency(Currency currency, int amount)
        {
            if (!_currencies.ContainsKey(currency))
                _currencies[currency] = 0;

            if (currency.maxAmount > 0 && _currencies[currency] >= currency.maxAmount)
                return false;

            if (currency.maxAmount > 0)
                _currencies[currency] = Mathf.Min(_currencies[currency] + amount, currency.maxAmount);
            else
                _currencies[currency] += amount;

            OnCurrencyChanged?.Invoke(currency, _currencies[currency]);
            return true;
        }

        /// <summary>
        /// Deducts <paramref name="amount"/> of <paramref name="currency"/>.
        /// Returns false and makes no change if funds are insufficient.
        /// </summary>
        public bool TrySpendCurrency(Currency currency, int amount)
        {
            if (GetCurrency(currency) < amount) return false;
            _currencies[currency] -= amount;
            OnCurrencyChanged?.Invoke(currency, _currencies[currency]);
            return true;
        }

        /// <summary>Returns true if the inventory holds enough of every currency in <paramref name="item"/>'s cost list.</summary>
        public bool CanAfford(Item item, int quantity = 1)
        {
            foreach (var cost in item.cost)
                if (GetCurrency(cost.currency) < cost.amount * quantity)
                    return false;
            return true;
        }

        // ── Purchase ─────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to purchase <paramref name="quantity"/> of <paramref name="item"/>:
        /// checks affordability and container space, deducts currency, then adds the item.
        /// All checks are atomic — nothing is deducted if the item cannot fit.
        /// </summary>
        public bool TryPurchase(Item item, int quantity = 1)
        {
            if (!CanAfford(item, quantity)) return false;

            Container target = null;
            foreach (var container in _containers)
            {
                if (container.CanAdd(item, quantity))
                {
                    target = container;
                    break;
                }
            }
            if (target == null) return false;

            foreach (var cost in item.cost)
                TrySpendCurrency(cost.currency, cost.amount * quantity);

            target.TryAdd(item, quantity);
            UpdateItemCount(item, quantity);
            OnItemAdded?.Invoke(item, GetItemCount(item));
            return true;
        }

        /// <summary>Attempts to purchase, reporting why it failed via <paramref name="result"/>.</summary>
        public bool TryPurchase(Item item, int quantity, out PurchaseResult result)
        {
            if (!CanAfford(item, quantity)) { result = PurchaseResult.CannotAfford; return false; }
            if (!CanAddItem(item, quantity)) { result = PurchaseResult.NoSpace; return false; }
            TryPurchase(item, quantity);
            result = PurchaseResult.Success;
            return true;
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
            foreach (var kvp in _currencies)
                snapshot.currencies.Add(new CurrencyAmount { currency = kvp.Key, amount = kvp.Value });
            return snapshot;
        }

        /// <summary>
        /// Restores inventory state from <paramref name="snapshot"/>, replacing all current contents.
        /// Does not fire events — callers should refresh UI after loading.
        /// </summary>
        public void Load(InventorySnapshot snapshot)
        {
            _containers.Clear();
            _currencies.Clear();
            _itemCounts.Clear();
            _lockedItems.Clear();

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

            foreach (var ca in snapshot.currencies)
                _currencies[ca.currency] = ca.amount;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void UpdateItemCount(Item item, int delta)
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
