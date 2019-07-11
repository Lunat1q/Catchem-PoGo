using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoGo.PokeMobBot.Logic.Extensions
{
    public class PropertyNotification : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        internal void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));}

        internal void OnPropertyChangedByName(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
