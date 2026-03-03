using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using zacharysnewman.Inventory;

/// <summary>
/// Binds an Item to a TMP_Text that displays the item's current global count.
/// </summary>
[Serializable]
public class ItemCountBinding
{
    public Item item;
    public TMP_Text text;
}

/// <summary>
/// Binds a Currency to a TMP_Text that displays the current amount held.
/// </summary>
[Serializable]
public class CurrencyBinding
{
    public Currency currency;
    public TMP_Text text;
}

/// <summary>
/// Binds a ContainerDefinition to a set of UI elements:
/// <list type="bullet">
///   <item><description><b>slotRoot</b> — activated when the container exists, deactivated when removed (e.g. tertiary weapon slot hidden until a large backpack is picked up).</description></item>
///   <item><description><b>itemNameText</b> — displays the first item's display name, or <see cref="emptyLabel"/> when the slot is empty. Best for capacity-1 equipment slots.</description></item>
///   <item><description><b>capacityText</b> — displays "used/capacity" (e.g. "5/10"). Best for ammo or stackable-item slots.</description></item>
/// </list>
/// Any of these fields can be left null to skip that part of the update.
/// </summary>
[Serializable]
public class ContainerSlotBinding
{
    public ContainerDefinition container;
    [Tooltip("Shown while this container exists; hidden after RemoveContainer is called.")]
    public GameObject slotRoot;
    [Tooltip("Displays the first item's name, or emptyLabel when nothing is in the slot.")]
    public TMP_Text itemNameText;
    [Tooltip("Displays \"used/capacity\", e.g. \"5/10\".")]
    public TMP_Text capacityText;
    [Tooltip("Text shown in itemNameText when the container is empty.")]
    public string emptyLabel = "Empty";
}

/// <summary>
/// Example HUD renderer that subscribes to <see cref="Inventory"/> events and
/// keeps TMP_Text components up to date without polling.
///
/// Setup:
///   1. Attach to any GameObject in the scene (does not need to be the same one as Inventory).
///   2. Assign the Inventory reference, or leave it null to auto-resolve via GetComponent.
///   3. Fill in the three binding lists in the Inspector.
///
/// Requires TextMeshPro (com.unity.textmeshpro).
/// </summary>
public class InventoryRenderer : MonoBehaviour
{
    [SerializeField] private Inventory inventory;

    [Header("Item Counts")]
    [Tooltip("Each entry drives one TMP_Text with the item's total count across all containers.")]
    [SerializeField] private List<ItemCountBinding> itemBindings = new List<ItemCountBinding>();

    [Header("Currencies")]
    [Tooltip("Each entry drives one TMP_Text with the currency's current amount.")]
    [SerializeField] private List<CurrencyBinding> currencyBindings = new List<CurrencyBinding>();

    [Header("Container Slots")]
    [Tooltip("Each entry manages a slot's visibility and content text as containers are added or removed at runtime.")]
    [SerializeField] private List<ContainerSlotBinding> slotBindings = new List<ContainerSlotBinding>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();

        inventory.OnItemAdded       += HandleItemChanged;
        inventory.OnItemRemoved     += HandleItemChanged;
        inventory.OnCurrencyChanged += HandleCurrencyChanged;
        inventory.OnContainerAdded  += HandleContainerAdded;
        inventory.OnContainerRemoved += HandleContainerRemoved;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (inventory == null) return;

        inventory.OnItemAdded       -= HandleItemChanged;
        inventory.OnItemRemoved     -= HandleItemChanged;
        inventory.OnCurrencyChanged -= HandleCurrencyChanged;
        inventory.OnContainerAdded  -= HandleContainerAdded;
        inventory.OnContainerRemoved -= HandleContainerRemoved;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void HandleItemChanged(Item item, int newCount)
    {
        foreach (var binding in itemBindings)
            if (binding.item == item && binding.text != null)
                binding.text.text = newCount.ToString();

        // An item change can affect any container's displayed contents
        RefreshSlotContents();
    }

    private void HandleCurrencyChanged(Currency currency, int newAmount)
    {
        foreach (var binding in currencyBindings)
            if (binding.currency == currency && binding.text != null)
                binding.text.text = newAmount.ToString();
    }

    private void HandleContainerAdded(Container container)
    {
        foreach (var binding in slotBindings)
        {
            if (binding.container != container.definition) continue;
            if (binding.slotRoot != null) binding.slotRoot.SetActive(true);
            RefreshSlot(binding, container);
        }
    }

    private void HandleContainerRemoved(ContainerDefinition definition)
    {
        foreach (var binding in slotBindings)
            if (binding.container == definition && binding.slotRoot != null)
                binding.slotRoot.SetActive(false);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    /// Full refresh — call this after bulk changes or on initial setup.
    private void RefreshAll()
    {
        foreach (var binding in itemBindings)
            if (binding.item != null && binding.text != null)
                binding.text.text = inventory.GetItemCount(binding.item).ToString();

        foreach (var binding in currencyBindings)
            if (binding.currency != null && binding.text != null)
                binding.text.text = inventory.GetCurrency(binding.currency).ToString();

        foreach (var binding in slotBindings)
        {
            if (binding.container == null) continue;
            var container = inventory.GetContainer(binding.container);
            bool exists = container != null;
            if (binding.slotRoot != null) binding.slotRoot.SetActive(exists);
            if (exists) RefreshSlot(binding, container);
        }
    }

    private void RefreshSlotContents()
    {
        foreach (var binding in slotBindings)
        {
            if (binding.container == null) continue;
            var container = inventory.GetContainer(binding.container);
            if (container != null) RefreshSlot(binding, container);
        }
    }

    private static void RefreshSlot(ContainerSlotBinding binding, Container container)
    {
        if (binding.capacityText != null)
            binding.capacityText.text = $"{container.UsedCapacity}/{container.definition.capacity}";

        if (binding.itemNameText != null)
        {
            var stacks = container.Stacks;
            binding.itemNameText.text = stacks.Count > 0
                ? stacks[0].item.displayName
                : binding.emptyLabel;
        }
    }
}
