using UnityEngine;

namespace zacharysnewman.Inventory
{
    /// <summary>
    /// Defines a currency type (e.g. Rupees, Gold, Gems).
    /// Create instances via Assets > Create > Inventory > Currency.
    /// Currency amounts are tracked on the Inventory and referenced by Item costs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCurrency", menuName = "Inventory/Currency")]
    public class Currency : ScriptableObject
    {
        public string displayName;
    }
}
