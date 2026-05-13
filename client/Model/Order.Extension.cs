using System.ComponentModel.DataAnnotations.Schema;

namespace DominiShop.Model;

public partial class Order : BaseModel
{
    [NotMapped]
    public string FormattedTotal => TotalPrice.HasValue
        ? TotalPrice.Value.ToString("N0") + " ₫"
        : "0 ₫";

    [NotMapped]
    public string FormattedOrderDate => OrderAt.ToString("dd/MM/yyyy HH:mm");

    [NotMapped]
    public string StatusLabel => Status ?? "Unknown";

    [NotMapped]
    public int ItemCount => OrderDetails?.Count ?? 0;

    [NotMapped]
    public string FormattedShippingFee => ShippingFee.HasValue && ShippingFee > 0
        ? ShippingFee.Value.ToString("N0") + " ₫"
        : "—";

    [NotMapped]
    public bool IsOnline => !string.IsNullOrEmpty(Address);

    [NotMapped]
    public string CustomerPhone => Phone ?? "—";
}
