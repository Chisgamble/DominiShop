using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DominiShop.Model;

public partial class Order : BaseModel
{
    [NotMapped]
    public static readonly string[] AvailableStatuses = new[] { "Pending", "Completed" };

    [NotMapped]
    public string FormattedTotal => TotalPrice.HasValue
        ? TotalPrice.Value.ToString("N0") + " ₫"
        : "0 ₫";

    [NotMapped]
    public string FormattedOrderDate => OrderAt.ToString("dd/MM/yyyy HH:mm");

    [NotMapped]
    public string StatusLabel => Status ?? "Unknown";

    [NotMapped]
    public string StatusColor
    {
        get
        {
            return Status switch
            {
                "Pending" => "#FF9800", // Orange
                "Completed" => "#4CAF50", // Green
                _ => "#9E9E9E" // Grey
            };
        }
    }

    public void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusColor));
    }

    [NotMapped]
    public int ItemCount => OrderDetails?.Sum(d => d.Quantity) ?? 0;

    [NotMapped]
    public string FormattedShippingFee => ShippingFee.HasValue && ShippingFee > 0
        ? ShippingFee.Value.ToString("N0") + " ₫"
        : "—";

    [NotMapped]
    public bool IsOnline => !string.IsNullOrEmpty(Address);

    [NotMapped]
    public string CustomerPhone => Phone ?? "—";
}
