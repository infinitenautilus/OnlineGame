using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using OnlineGame.Core.Interfaces;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Core
{
    public class Heartbeat
    {
        private readonly System.Timers.Timer _timer;

        // Dictionary to hold registered objects and their respective actions
        private readonly ConcurrentDictionary<IUpdateable, Action> _registeredActions = new();

        public Heartbeat(double interval)
        {
            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        // Register with a specific action (or default to Update)
        public void Register(IUpdateable updateable, Action? customAction = null)
        {
            if (!_registeredActions.ContainsKey(updateable))
            {
                _registeredActions[updateable] = customAction ?? updateable.Update;
            }
        }

        public void Unregister(IUpdateable updateable)
        {
            _registeredActions.TryRemove(updateable, out _);
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            foreach (var (updateable, action) in _registeredActions.ToList())
            {
                try
                {
                    action.Invoke(); // Invoke the custom or default action
                }
                catch (Exception ex)
                {
                    Scribe.Error(ex);
                }
            }
        }
    }
}
