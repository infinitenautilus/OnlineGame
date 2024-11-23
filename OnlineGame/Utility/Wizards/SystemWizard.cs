using System;
using System.Collections.Generic;
using System.Text;
using OnlineGame.Core;
using OnlineGame.Network;
using OnlineGame.Utility.Types;

namespace OnlineGame.Utility.Wizards
{
    public sealed class SystemWizard
    {
        // Singleton instance
        private static readonly Lazy<SystemWizard> _instance = new(() => new SystemWizard());

        // Private dictionary for subsystems
        private readonly Dictionary<string, ISubsystem> _subsystems = [];

        // Singleton instance accessor
        public static SystemWizard Instance => _instance.Value;

        // Event for notifications
        public event EventHandler<SystemEventArgs>? NotificationSent;

        // Private constructor to enforce singleton
        private SystemWizard()
        {
            

        }

        public void RegisterSubsystem(ISubsystem subsystem)
        {
            if (!_subsystems.ContainsKey(subsystem.Name))
            {
                _subsystems[subsystem.Name] = subsystem;
                subsystem.StateChanged += OnSubsystemStateChanged;

                Scribe.Notification($"Subsystem {subsystem.Name} registered.");
            }
            else
            {
                Scribe.Notification($"Subsystem {subsystem.Name} is already registered.");
            }
        }

        public ISubsystem GetSubsystem(string name)
        {
            if(_subsystems.TryGetValue(name, out ISubsystem? subsystem))
            {
                return subsystem;
            }

            throw new KeyNotFoundException($"Subsystem {name} not found in GetSubSystem(string name).");
        }

        public void StartAll()
        {
            if (_subsystems.Count == 0)
            {
                _subsystems.Add(SocketWizard.Instance.Name, SocketWizard.Instance);
                _subsystems.Add(Sentinel.Instance.Name, Sentinel.Instance);
                _subsystems.Add(GateKeeper.Instance.Name, GateKeeper.Instance);
            }

            foreach (ISubsystem subsystem in _subsystems.Values)
            {
                try
                {
                    subsystem.Start();
                    Scribe.Notification($"Subsystem {subsystem.Name} started.");
                }
                catch (Exception ex)
                {
                    Scribe.Error(ex, $"Error starting subsystem {subsystem.Name}");
                }
            }
        }

        public void StopAll()
        {
            foreach (ISubsystem subsystem in _subsystems.Values)
            {
                try
                {
                    subsystem.Stop();
                    Scribe.Notification($"Subsystem {subsystem.Name} stopped.");
                }
                catch (Exception ex)
                {
                    Scribe.Error(ex, $"Error stopping subsystem {subsystem.Name}");
                }
            }
        }

        public void ListProcesses()
        {

            foreach (ISubsystem subsystem in _subsystems.Values)
            {
                Scribe.Notification($"{subsystem.Name} - {subsystem.CurrentSystemState}");
            }
        }

        public SubsystemState GetSubsystemState(string name)
        {
            return _subsystems.TryGetValue(name, out ISubsystem? value)
                ? value.CurrentSystemState
                : throw new KeyNotFoundException($"Subsystem {name} not found.");
        }

        public async Task Shutdown()
        {
            try
            {
                StopAll();
                ListProcesses();
                
                Scribe.BeginShutdown();
                SocketWizard.Instance.Stop();
                Sentinel.Instance.Stop();

                Scribe.Scry("Shutting down subsystems...");
                await Scribe.CloseWriter();
                Scribe.Scry("Application shutdown complete.");

            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message} in {ex.StackTrace}");
            }
        }

        private void OnSubsystemStateChanged(object? sender, SystemEventArgs e)
        {
            Notify(e.Type, e.Source, e.Message);
        }

        private void Notify(string type, string source, string message)
        {
            SystemEventArgs eventArgs = new(type, source, message);
            NotificationSent?.Invoke(this, eventArgs);
            Scribe.Notification($"Event Notification: {type} - {source} - {message}");
        }
    }
}
