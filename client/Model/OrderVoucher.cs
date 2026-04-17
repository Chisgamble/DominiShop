using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class OrderVoucher
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? OrderId { get; set; }

    public int? VoucherId { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
