// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace System.Windows.Data;

public sealed class ListCollectionViewTests
{
    public sealed class TestItem : INotifyPropertyChanged
    {
        public int Value
        {
            get => field;
            set
            {
                if (field != value)
                {
                    field = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [WpfFact]
    public async Task LiveSorting_INotifyCollectionChanged_Consistency()
    {
        var random = new Random(0);

        var sourceList = new ObservableCollection<TestItem>(
            Enumerable.Range(0, 1000).Select(_ => new TestItem { Value = random.Next() }));

        var collectionView = new ListCollectionView(sourceList)
        {
            IsLiveSorting = true,
            SortDescriptions = { new SortDescription(nameof(TestItem.Value), ListSortDirection.Ascending) },
            LiveSortingProperties = { nameof(TestItem.Value) }
        };

        var observedList = new List<TestItem>(collectionView.Cast<TestItem>());
        ((INotifyCollectionChanged)collectionView).CollectionChanged += (_, a) =>
        {
            Assert.True(a.Action == NotifyCollectionChangedAction.Move);
            Assert.True(a.OldItems != null);
            Assert.True(a.OldStartingIndex >= 0 && a.OldStartingIndex + a.OldItems.Count <= observedList.Count);
            int idx = a.OldStartingIndex;
            for (int i = 0; i < a.OldItems.Count; i++)
            {
                Assert.Same(observedList[idx + i], a.OldItems[i]);
            }
            observedList.RemoveRange(a.OldStartingIndex, a.OldItems.Count);
            Assert.True(a.NewItems != null);
            Assert.True(a.OldItems.Cast<TestItem>().SequenceEqual(a.NewItems.Cast<TestItem>()));
            idx = a.NewStartingIndex;
            for (int i = 0; i < a.NewItems.Count; i++)
            {
                Assert.Equal(idx + i, collectionView.IndexOf(a.NewItems[i]));
            }
            observedList.InsertRange(a.NewStartingIndex, a.NewItems.Cast<TestItem>());
        };

        for (int i = 0; i < 10; i++)
        {
            foreach (var item in sourceList)
            {
                item.Value = random.Next();
            }
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.DataBind); // Trigger RestoreLiveShaping()
        }
    }
}
