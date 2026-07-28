using System;
using System.Collections.Generic;
using System.Linq;
using UniSky.Services.Navigation;
using Windows.UI.Core;

namespace UniSky.Navigation;

/// <summary>
/// Owns this view's one <see cref="SystemNavigationManager.BackRequested"/> subscription and walks
/// registered handlers outermost-first.
/// </summary>
internal sealed class BackNavigationCoordinator : IBackNavigationCoordinator
{
    private sealed class Entry
    {
        public IBackHandler Handler { get; set; }
        public int Priority { get; set; }
        public long Sequence { get; set; }
    }

    private readonly List<Entry> _entries = [];
    private readonly SystemNavigationManager _manager;

    private long _sequence;
    private bool _lastCanGoBack;

    public BackNavigationCoordinator()
    {
        _manager = SystemNavigationManager.GetForCurrentView();
        _manager.BackRequested += OnBackRequested;
    }

    public event EventHandler CanGoBackChanged;

    public bool CanGoBack
        => _entries.Any(e => e.Handler.CanGoBack);

    public IDisposable Register(IBackHandler handler, int priority)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var entry = new Entry
        {
            Handler = handler,
            Priority = priority,
            Sequence = ++_sequence
        };

        _entries.Add(entry);
        Invalidate();

        return new Registration(this, entry);
    }

    public bool TryGoBack()
    {
        // Highest priority first; among equals, most recently registered — the top sheet of a stack
        // gets the gesture before the one underneath it.
        foreach (var entry in _entries.OrderByDescending(e => e.Priority).ThenByDescending(e => e.Sequence).ToArray())
        {
            if (!entry.Handler.CanGoBack)
                continue;

            if (entry.Handler.TryGoBack())
            {
                Invalidate();
                return true;
            }
        }

        return false;
    }

    public void Invalidate()
    {
        var canGoBack = CanGoBack;

        // deprecated, we have our own titlebar :)
        //_manager.AppViewBackButtonVisibility = canGoBack
        //    ? AppViewBackButtonVisibi/colity.Visible
        //    : AppViewBackButtonVisibility.Collapsed;

        if (canGoBack == _lastCanGoBack)
            return;

        _lastCanGoBack = canGoBack;
        CanGoBackChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnBackRequested(object sender, BackRequestedEventArgs e)
    {
        if (e.Handled)
            return;

        e.Handled = TryGoBack();
    }

    private void Unregister(Entry entry)
    {
        if (_entries.Remove(entry))
            Invalidate();
    }

    private sealed class Registration : IDisposable
    {
        private BackNavigationCoordinator _coordinator;
        private readonly Entry _entry;

        public Registration(BackNavigationCoordinator coordinator, Entry entry)
        {
            _coordinator = coordinator;
            _entry = entry;
        }

        public void Dispose()
        {
            var coordinator = _coordinator;
            if (coordinator == null)
                return;

            _coordinator = null;
            coordinator.Unregister(_entry);
        }
    }
}
