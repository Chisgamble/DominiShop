using System.ComponentModel.DataAnnotations.Schema;

namespace DominiShop.Model;

public partial class Product : BaseModel
{

    [NotMapped]
    public string FormattedPrice => Price.ToString("N0") + " đ";

    [NotMapped]
    public string FormattedBasePrice => BasePrice.ToString("N0") + " đ";

    [NotMapped]
    public string CategoryName => Category?.Name ?? "Chưa phân loại";
}