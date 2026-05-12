using System.ComponentModel.DataAnnotations.Schema;

namespace DominiShop.Model;

public partial class OrderDetail : BaseModel
{
    [NotMapped]
    public string ProductName => Product?.Name ?? "Deleted Product";

    [NotMapped]
    public decimal SubTotal => Price * Quantity;

    [NotMapped]
    public string FormattedSubTotal => SubTotal.ToString("N0") + " ₫";

    [NotMapped]
    public string FormattedPrice => Price.ToString("N0") + " ₫";
}
