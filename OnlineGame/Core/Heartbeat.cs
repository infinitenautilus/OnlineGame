using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using OnlineGame.Core.Interfaces;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Core
{
    public class Heartbeat
    {
        private readonly System.Timers.Timer _timer;
        private readonly ThreadSafeList<IUpdateable> _registeredObjects = [];


        public Heartbeat(double interval)
        {
            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        public void Register(IUpdateable updateable)
        {
            if(!_registeredObjects.Contains(updateable)) 
                _registeredObjects.Add(updateable);
        }

        public void Unregister(IUpdateable updateable)
        {
            if (_registeredObjects.Contains(updateable))
                _registeredObjects.Remove(updateable);
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            foreach(IUpdateable u in _registeredObjects.ToList())
            {
                try
                {
                    u.Update();
                }
                catch(Exception ex)
                {
                    Scribe.Error(ex);
                }
            }
        }
    }
}
