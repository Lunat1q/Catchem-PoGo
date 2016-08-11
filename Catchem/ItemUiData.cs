using POGOProtos.Inventory.Item;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Catchem
{
    public class ItemUiData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ItemId Id { get; set; }
        public BitmapSource Image { get; set; }
        public string Name { get; set; }
        private int _amount;

        public int Amount
        {
            get { return _amount; }
            set
            {
                _amount = value;
                OnPropertyChanged();
            }
        }

        public ItemUiData(ItemId id, BitmapSource img, string name, int amount)
        {
            Id = id;
            Image = img;
            Name = name;
            Amount = amount;
        }
    }
}
