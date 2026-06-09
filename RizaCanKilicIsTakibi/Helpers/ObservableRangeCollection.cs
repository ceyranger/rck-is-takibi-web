using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RizaCanKilicIsTakibi.Helpers;

/// <summary>
/// Toplu eleman ekleme (AddRange) ve değiştirme (ReplaceRange) gibi işlemlerde
/// <see cref="INotifyCollectionChanged.CollectionChanged"/> yordamını her eleman için 
/// tetiklemek yerine 1 kere topluca (Reset parametresiyle) tetikleyerek UI donmalarını 
/// önleyen ObservableCollection sarmalayıcısıdır.
/// </summary>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public ObservableRangeCollection() { }

    public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

    /// <summary>
    /// İçerideki tüm elemanları temizler ve yerine listeyi ekler.
    /// WPF arayüzüne sadece 1 kere "Liste tamamen yenilendi" haberini verir.
    /// </summary>
    public void ReplaceRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        Items.Clear();
        foreach (var item in collection)
        {
            Items.Add(item);
        }
        
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Listedeki en sona birden fazla elemanı tek işlemde ekler.
    /// </summary>
    public void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        bool hasItems = false;
        foreach (var item in collection)
        {
            Items.Add(item);
            hasItems = true;
        }

        if (hasItems)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
