using System.ComponentModel.DataAnnotations.Schema;

namespace DominiShop.Model;

public partial class Product : BaseModel
{
    private int _cartQuantity;

    [NotMapped]
    public int CartQuantity
    {
        get => _cartQuantity;
        set
        {
            if (_cartQuantity != value)
            {
                _cartQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanIncrement));
                OnPropertyChanged(nameof(CanDecrement));
            }
        }
    }

    [NotMapped]
    public bool CanDecrement => CartQuantity > 0;

    [NotMapped]
    public bool CanIncrement => CartQuantity < Quantity;

    [NotMapped]
    public string FormattedPrice => Price.ToString("N0") + " đ";

    [NotMapped]
    public string FormattedBasePrice => BasePrice.ToString("N0") + " đ";

    [NotMapped]
    public string CategoryName => Category?.Name ?? "Chưa phân loại";
}