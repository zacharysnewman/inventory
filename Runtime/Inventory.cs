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
        /// Items inside are discarded — transfer them first if you need to preserve them.
        /// Returns true if at least one container was removed.
        /// </summary>
        public bool RemoveContainer(ContainerDefinition definition)
        {
            bool removed = _containers.RemoveAll(c => c.definition == definition) > 0;
            if (removed) OnContainerRemoved?.Invoke(definition);
            return removed;
        }

        // ── Item Management ──────────────────────────────────────────────────

        /// <summary>Returns true if at least one container in this inventory can accept <paramref name="quantity"/> of <paramref name="item"/>.</summary>
        public bool CanAddItem(Item item, int quantity = 1)
        {
            foreach (var container in _containers)
                if (container.CanAdd(item, quantity))
                    return true;
            return false;
        }

        /// <summary>Adds <paramref name="quantity"/> of <paramref name="item"/> to the first container that accepts it.</summary>
        public bool TryAddItem(Item item, int quantity = 1)
        {
            foreach (var container in _containers)
            {
                if (container.TryAdd(item, quantity))
                {
                    OnItemAdded?.Invoke(item, GetItemCount(item));
                    return true;
                }
            }
            return false;
        }

        /// <summary>Removes <paramref name="quantity"/> of <paramref name="item"/> from the first container that has it.</summary>
        public bool TryRemoveItem(Item item, int quantity = 1)
        {
            foreach (var container in _containers)
            {
                if (container.TryRemove(item, quantity))
                {
                    OnItemRemoved?.Invoke(item, GetItemCount(item));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Transfers <paramref name="quantity"/> of <paramref name="item"/> from this inventory into <paramref name="target"/>.
        /// Atomic — nothing changes if this inventory lacks the item or <paramref name="target"/> cannot accept it.
        /// Returns true on success.
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

        /// <summary>Returns the total count of <paramref name="item"/> across all containers.</summary>
        public int GetItemCount(Item item)
        {
            int total = 0;
            foreach (var container in _containers)
                total += container.GetQuantity(item);
            return total;
        }

        // ── Currency Management ──────────────────────────────────────────────

        /// <summary>Returns the current amount of <paramref name="currency"/> held.</summary>
        public int GetCurrency(Currency currency)
        {
            _currencies.TryGetValue(currency, out int amount);
            return amount;
        }

        /// <summary>Adds <paramref name="amount"/> of <paramref name="currency"/>.</summary>
        public void AddCurrency(Currency currency, int amount)
        {
            if (!_currencies.ContainsKey(currency))
                _currencies[currency] = 0;
            _currencies[currency] += amount;
            OnCurrencyChanged?.Invoke(currency, _currencies[currency]);
        }

        /// <summary>
        /// Deducts <paramref name="amount"/> of <paramref name="currency"/>.
        /// Returns false and makes no change if funds are insufficient.
        /// </summary>
        public bool TrySpendCurrency(Currency currency, int amount)
        {
            if (GetCurrency(currency) < amount)
                return false;
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
            if (!CanAfford(item, quantity))
                return false;

            Container target = null;
            foreach (var container in _containers)
            {
                if (container.CanAdd(item, quantity))
                {
                    target = container;
                    break;
                }
            }

            if (target == null)
                return false;

            foreach (var cost in item.cost)
                TrySpendCurrency(cost.currency, cost.amount * quantity);

            target.TryAdd(item, quantity);
            OnItemAdded?.Invoke(item, GetItemCount(item));
            return true;
        }
    }
}
