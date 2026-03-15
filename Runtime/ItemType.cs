namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Enumerates the item types in your game.
    /// Add a value here for each distinct container-routing category you need.
    /// <para>
    /// <b>None</b> is reserved for general items that only go into <c>acceptsAllTypes</c> containers.
    /// </para>
    /// </summary>
    public enum ItemType
    {
        None = 0,  // general item — only accepted by acceptsAllTypes containers

        // ── Add your game's item types below ─────────────────────────────────
        BombBag,
        Quiver,
        Equipment,
        Heart,
        Bottle,
        Wallet,
        Primary,
        Secondary,
        Tertiary,
        Lethal,
        Tactical,
        ArmorPlate,
    }
}
