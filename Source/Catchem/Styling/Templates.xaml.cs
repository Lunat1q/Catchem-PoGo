using System.Collections;
using System.Windows.Controls;
using PoGo.PokeMobBot.Logic.Tasks;

namespace Catchem.Styling
{
    public partial class Templates
    {
        private void RemoveAction_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.DataContext == null) return;
            var obj = btn.DataContext as ManualAction;
            var parentList = btn.Tag as ListBox;
            var source = (IList)parentList?.ItemsSource;
            if (source == null || obj == null || !source.Contains(obj)) return;
            obj.Session.RemoveActionFromQueue(obj);
            source.Remove(obj);
        }
    }
}
