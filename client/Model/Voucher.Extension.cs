using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DominiShop.Model
{
    public partial class Voucher : BaseModel
    {
        [NotMapped]
        public string DisplayDiscount => Type switch
        {
            "percent" => Percent.HasValue ? $"{Percent}% OFF" : "— %",
            "fixed" => Percent.HasValue ? $"{Percent:N0} VNĐ OFF" : "— VNĐ",
            "free_shipping" => "Free shipping",
            _ => Percent.HasValue ? $"{Percent}%" : "—"
        };

        [NotMapped]
        public string DisplayType => Type switch
        {
            "percent" => "Percent",
            "fixed" => "Fixed",
            "free_shipping" => "Free Shipping",
            _ => Type ?? "—"
        };

        [NotMapped]
        public string DisplayExpiry => ExpiryDate.HasValue
            ? ExpiryDate.Value.ToString("dd/MM/yyyy")
            : "No expiry";

        [NotMapped]
        public string StatusLabel => IsActive ? "Active" : "Inactive";

        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    }

}
