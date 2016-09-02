using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoGo.PokeMobBot.Logic.Extensions
{
    public class PropertyNotification : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
