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
    }

    /// <summary>Result of an item remove operation.</summary>
    public enum RemoveResult
    {
        Success,
        /// <summary>The inventory does not hold enough of the item to remove the requested quantity.</summary>
        NotEnough,
    }
}
