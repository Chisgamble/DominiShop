using System.ComponentModel.DataAnnotations.Schema;

namespace DominiShop.Model;

public partial class OrderTax : BaseModel
{
    [NotMapped]
    public string TaxName => Tax?.Name ?? "—";

    [NotMapped]
    public string TaxFormattedValue => Tax?.FormattedValue ?? "—";
}
