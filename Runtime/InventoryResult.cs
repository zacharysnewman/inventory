namespace zacharysnewman.Inventory
{
    /// <summary>Result of an item add operation.</summary>
    public enum AddResult
    {
        Success,
        /// <summary>The item's compatible types don't match any accepted type in this container.</summary>
        WrongType,
        /// <summary>The container has no remaining capacity for the requested quantity.</summary>
        NoSpace,
        /// <summary>Adding would exceed the item's maxGlobalCount limit on this inventory.</summary>
        ExceedsGlobalLimit,
    }

    /// <summary>Result of an item remove operation.</summary>
    public enum RemoveResult
    {
        Success,
        /// <summary>The inventory does not hold enough of the item to remove the requested quantity.</summary>
        NotEnough,
        /// <summary>The item is currently locked and cannot be removed.</summary>
        Locked,
    }

    /// <summary>Result of a purchase operation.</summary>
    public enum PurchaseResult
    {
        Success,
        /// <summary>The inventory does not have enough currency to cover the item's cost.</summary>
        CannotAfford,
        /// <summary>No container has space for the purchased item.</summary>
        NoSpace,
    }
}
