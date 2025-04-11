using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Syntec.TestApp.WPF.Utils
{
    /// <summary>
    /// Коллекция с фиксированным размером, автоматически удаляющая старые элементы
    /// </summary>
    public class FixedSizeObservableCollection<T> : ObservableCollection<T>
    {
        private readonly int _maxSize;
        private readonly object _lock = new object();

        public FixedSizeObservableCollection(int maxSize)
        {
            if (maxSize <= 0)
                throw new ArgumentException("Max size must be greater than 0");

            _maxSize = maxSize;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            lock (_lock)
            {
                // Удаляем старые элементы при превышении размера
                while (Count > _maxSize)
                {
                    RemoveAt(0);
                }
            }

            base.OnCollectionChanged(e);
        }

        public new void Add(T item)
        {
            lock (_lock)
            {
                base.Add(item);
            }
        }
    }
}