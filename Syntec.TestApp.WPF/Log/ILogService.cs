using System.Collections.ObjectModel;
using System;

namespace Syntec.TestApp.WPF.Log
{
    public interface ILogService
    {
        void AddEntry(LogEntry entry);
        IObservable<LogEntry> Entries { get; }
        ReadOnlyObservableCollection<LogEntry> LatestEntries { get; }
        void Clear();
    }

}