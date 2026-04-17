using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class CustomerVoucher
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CustomerId { get; set; }

    public int? VoucherId { get; set; }

    public int? Quantity { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
