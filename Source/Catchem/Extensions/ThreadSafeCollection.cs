using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace Catchem.Extensions
{
    public class MtObservableCollection<T> : ObservableCollection<T>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var collectionChanged = CollectionChanged;
            if (collectionChanged == null) return;
            foreach (var @delegate in collectionChanged.GetInvocationList())
            {
                var nh = (NotifyCollectionChangedEventHandler) @delegate;
                var dispObj = nh.Target as DispatcherObject;
                var dispatcher = dispObj?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.BeginInvoke(
                        (Action)(() => nh.Invoke(this,
                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
                        DispatcherPriority.DataBind);
                    continue;
                }
                nh.Invoke(this, e);
            }
        }
    }
}
