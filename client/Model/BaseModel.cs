using System;
using System.ComponentModel;

namespace DominiShop.Model
{
    public partial class BaseModel : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}

