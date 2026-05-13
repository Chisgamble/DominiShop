using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DominiShop.Model
{
    public partial class Tax : BaseModel
    {
        [NotMapped]
        public double ValueAsDouble
        {
            get => (double)(Value ?? 0);
            set => Value = (decimal)value;
        }

        [NotMapped]
        public string FormattedValue
        {
            get
            {
                if (Value == null) return "0";
                return Type == "Percentage"
                    ? $"{Value:N1}%"
                    : string.Format("{0:N0} ₫", Value);
            }
        }

        [NotMapped]
        public string OwnerName => Owner?.Username ?? "System";

        [NotMapped]
        public bool IsAutoApply
        {
            get => AutoApply == true;
            set
            {
                AutoApply = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoApply));
            }
        }
    }
}
