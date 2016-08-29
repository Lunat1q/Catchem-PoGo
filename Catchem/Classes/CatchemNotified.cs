using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Catchem.Classes
{
    public abstract class CatchemNotified : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
