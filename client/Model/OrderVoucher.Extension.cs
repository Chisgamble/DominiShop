using System.ComponentModel.DataAnnotations.Schema;

namespace DominiShop.Model;

public partial class OrderVoucher : BaseModel
{
    [NotMapped]
    public string VoucherCode => Voucher?.Code ?? "—";

    [NotMapped]
    public string VoucherDiscount => Voucher?.DisplayDiscount ?? "—";
}
