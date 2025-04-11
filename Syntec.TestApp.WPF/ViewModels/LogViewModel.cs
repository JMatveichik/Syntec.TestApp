using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Syntec.TestApp.WPF.Log;
using Syntec.TestApp.WPF.Utils;

namespace Syntec.TestApp.WPF.ViewModels
{
    public class LogViewModel : ReactiveObject, ILogService
    {
        private const int MaxEntries = 1000;
        
        private readonly FixedSizeObservableCollection<LogEntry> _entries;
        private readonly ReadOnlyObservableCollection<LogEntry> _latestEntries;
        
        private NotificationType _selectedLogLevel = NotificationType.Debug;
        private readonly ObservableAsPropertyHelper<ReadOnlyObservableCollection<LogEntry>> _filteredEntries;

        /// <summary>
        /// Доступные уровни логирования для фильтрации
        /// </summary>
        public ReadOnlyObservableCollection<NotificationType> LogLevels { get; } =
            new ReadOnlyObservableCollection<NotificationType>(
                new ObservableCollection<NotificationType>(
                    Enum.GetValues(typeof(NotificationType))
                        .Cast<NotificationType>()));

        /// <summary>
        /// Выбранный уровень логирования для фильтрации
        /// </summary>
        public NotificationType SelectedLogLevel
        {
            get => _selectedLogLevel;
            set => this.RaiseAndSetIfChanged(ref _selectedLogLevel, value);
        }

        /// <summary>
        /// Отфильтрованные записи лога по выбранному уровню
        /// </summary>
        public ReadOnlyObservableCollection<LogEntry> FilteredEntries => _filteredEntries.Value;

        /// <summary>
        /// Команда для очистки лога
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearLogCommand { get; }

        public LogViewModel()
        {
            _entries = new FixedSizeObservableCollection<LogEntry>(MaxEntries);
            _latestEntries = new ReadOnlyObservableCollection<LogEntry>(_entries);

            // 1. Создаем поток изменений коллекции (преобразуем в Unit)
            var collectionChanges = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => _entries.CollectionChanged += h,
                h => _entries.CollectionChanged -= h)
                .Select(_ => Unit.Default);

            // 2. Создаем поток изменений MinLogLevel (тоже преобразуем в Unit)
            var levelChanges = this.WhenAnyValue(x => x.SelectedLogLevel)
                .Select(_ => Unit.Default);

            // 3. Объединяем оба потока
            var allChanges = Observable.Merge(collectionChanges, levelChanges);

            // 4. Применяем фильтрацию при любых изменениях
            allChanges
                .Select(_ => new ReadOnlyObservableCollection<LogEntry>(
                    new ObservableCollection<LogEntry>(
                        _entries.Where(e => e.Type >= SelectedLogLevel))))
                .ToProperty(this, x => x.FilteredEntries, out _filteredEntries);

            Entries = Observable.FromEventPattern<LogEntry>(
                h => EntryAdded += h,
                h => EntryAdded -= h)
                .Select(x => x.EventArgs);

            ClearLogCommand = ReactiveCommand.Create(Clear);
        }

        /// <summary>
        /// Событие добавления новой записи в лог
        /// </summary>
        public event EventHandler<LogEntry> EntryAdded;

        /// <summary>
        /// Последние записи лога (без фильтрации)
        /// </summary>
        public ReadOnlyObservableCollection<LogEntry> LatestEntries => _latestEntries;

        /// <summary>
        /// Поток новых записей лога
        /// </summary>
        public IObservable<LogEntry> Entries { get; }

        /// <summary>
        /// Добавляет новую запись в лог
        /// </summary>
        /// <param name="entry">Запись для добавления</param>
        public void AddEntry(LogEntry entry)
        {
            _entries.Add(entry);
            EntryAdded?.Invoke(null, entry);
        }

        /// <summary>
        /// Очищает все записи лога
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}