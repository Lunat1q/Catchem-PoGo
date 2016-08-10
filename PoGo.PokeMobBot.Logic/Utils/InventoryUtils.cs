using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using POGOProtos.Inventory.Item;

namespace PoGo.PokeMobBot.Logic.Utils
{
    public static class InventoryUtils
    {
        public static List<Tuple<ItemId, int>> ToItemList(this RepeatedField<ItemAward> awards)
        {
            var newItemsList = new List<Tuple<ItemId, int>>();
            if (awards == null || awards.Count == 0) return newItemsList;
            foreach (var item in awards)
            {
                newItemsList.Add(Tuple.Create(item.ItemId, item.ItemCount));
            }
            return newItemsList;;
        }
    }
}
