using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Catchem.UiTranslation;
using POGOProtos.Inventory.Item;

namespace Catchem.Classes
{
    public class ItemUiData : CatchemNotified
    {
        private static readonly Dictionary<ItemId, int> UsableItems = new Dictionary<ItemId, int>
        {
            {ItemId.ItemIncenseOrdinary, 1800000},
            {ItemId.ItemLuckyEgg, 1800000},
            {ItemId.ItemIncenseCool, 1800000},
            {ItemId.ItemIncenseFloral, 1800000},
            {ItemId.ItemIncenseSpicy, 1800000}
        };

        private string _name;

        public ItemId Id { get; set; }


        public BotWindowData OwnerBot { get; set; }

        private int _amount;

        private readonly DispatcherTimer _usageTimer;

        private TimeSpan _ts;

        public bool Usable => UsableItems.ContainsKey(Id) && !InUse;

        private bool _inUse;

        public string AmountText => TranslationEngine.GetDynamicTranslationString("%AMOUNT%", "Amount: ") + Amount;

        public int Amount
        {
            get { return _amount; }
            set
            {
                _amount = value;
                OnPropertyChanged();
                OnPropertyChangedByName("AmountText");
            }
        }

        public TimeSpan Ts
        {
            get { return _ts; }
            set
            {
                _ts = value;
                OnPropertyChanged();
                OnPropertyChangedByName("LeftTimePercent");
            }
        }

        public double LeftTimePercent
        {
            get
            {
                if (UsableItems.ContainsKey(Id))
                {
                    return 100 * Ts.TotalMilliseconds/UsableItems[Id];
                }
                return 0;
            }
        } 

        public bool InUse
        {
            get { return _inUse; }
            set
            {
                _inUse = value;
                OnPropertyChanged();
                if (!_inUse && _usageTimer.IsEnabled)
                {
                    _usageTimer.Stop();
                }
                else if (_inUse && !_usageTimer.IsEnabled)
                {
                    _usageTimer.Start();
                }
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value; 
                OnPropertyChanged();
            }
        }

        public void SetActive(int activeMs)
        {
            Ts = new TimeSpan(0, 0, 0, 0, activeMs);
            InUse = true;
        }

        public ItemUiData(ItemId id, string name, int amount, BotWindowData bot)
        {
            OwnerBot = bot;
            Id = id;
            Name = name;
            Amount = amount;
            _usageTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            _usageTimer.Tick += delegate
            {
                Ts -= new TimeSpan(0, 0, 1);
                if (Ts.TotalSeconds <= 0)
                {
                    InUse = false;
                }
            };
        }

        public ItemUiData()
        {
            
        }
    }
}
