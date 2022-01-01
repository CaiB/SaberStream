using System;
using System.Collections.Generic;

namespace SaberStream.Data
{
    public static class RequestQueue
    {
        private static readonly List<MapInfo> Items = new();
        public static event EventHandler<QueueChangeEventArgs>? QueueChanged;

        public static int AddItem(MapInfo map)
        {
            Items.Add(map);
            int Index = Items.Count - 1;
            QueueChanged?.Invoke(null, new(map, Index, true));
            return Index;
        }

        public static MapInfo GetItem(int index) => Items[index];

        public static int GetItemCount() => Items.Count;

        public static void RemoveItem(int index)
        {
            MapInfo map = Items[index];
            Items.RemoveAt(index);
            QueueChanged?.Invoke(null, new(map, index, false));
        }

        public static void RemoveItem(MapInfo map)
        {
            int Index = Items.IndexOf(map);
            if (Index >= 0)
            {
                Items.RemoveAt(Index);
                QueueChanged?.Invoke(null, new(map, Index, false));
            }
        }
    }

    public class QueueChangeEventArgs : EventArgs
    {
        public MapInfo Map { get; init; }
        public int Index { get; init; }
        public bool Added { get; init; }

        public QueueChangeEventArgs(MapInfo map, int index, bool wasAdded)
        {
            this.Map = map;
            this.Index = index;
            this.Added = wasAdded;
        }
    }
}
