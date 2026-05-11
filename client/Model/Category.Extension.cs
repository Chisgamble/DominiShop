using DominiShop.Model;

namespace DominiShop.Model;

public partial class Category : BaseModel
{
   public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy");
   public int ProductCount => Products?.Count ?? 0;
}