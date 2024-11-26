using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Processes;
using OnlineGame.Core.Types;
using OnlineGame.Game.Core.Processes;
using OnlineGame.Network;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Core
{
    public sealed class SystemWizard
    {
        // Singleton instance
        private static readonly Lazy<SystemWizard> _instance = new(() => new SystemWizard());

        // Thread-safe dictionary for subsystems
        private readonly ConcurrentDictionary<string, ISubsystem> _subsystems = new();

        // Singleton instance accessor
        public static SystemWizard Instance => _instance.Value;

        // Have I started processes?
        public SubsystemState CurrentState { get; private set; } = SubsystemState.Stopped;

        // Event for notifications (thread-safe subscription)
        private event EventHandler<SystemEventArgs>? SafeNotificationSent;
        public event EventHandler<SystemEventArgs>? NotificationSent
        {
            add
            {
                lock (_eventLock)
                {
                    SafeNotificationSent += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    SafeNotificationSent -= value;
                }
            }
        }

        private readonly object _eventLock = new();

        // Private constructor to enforce singleton
        private SystemWizard() 
        {
        }

        public void RegisterSubsystem(ISubsystem subsystem)
        {
            if (_subsystems.TryAdd(subsystem.Name, subsystem))
            {
                subsystem.StateChanged += OnSubsystemStateChanged;
            }
            else
            {
                Scribe.Notification($"Subsystem {subsystem.Name} is already registered");
            }
        }

        public ISubsystem GetSubsystem(string name)
        {
            if (_subsystems.TryGetValue(name, out ISubsystem? subsystem))
            {
                return subsystem;
            }

            throw new KeyNotFoundException($"Subsystem {name} not found in GetSubsystem(string name)");
        }

        public async void StartAll()
        {
            try
            {
                if (_subsystems.IsEmpty)
                {
                    LoadSubsystemsFromNamespace("OnlineGame.Core.Processes");
                    _subsystems.TryAdd(Sentinel.Instance.Name, Sentinel.Instance);
                    _subsystems.TryAdd(GateKeeper.Instance.Name, GateKeeper.Instance);
                    GameDirector.Instance.Start();
                    await RoomRepository.Instance.Start();
                }

                foreach (ISubsystem subsystem in _subsystems.Values)
                {
                    try
                    {
                        if (subsystem.CurrentSystemState != SubsystemState.Running)
                        {
                            subsystem.Start();
                            Scribe.Notification($"Subsystem {subsystem.Name} started");
                        }
                    }
                    catch (Exception ex)
                    {
                        Scribe.Error(ex, $"Error starting subsystem {subsystem.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public void StopAll()
        {
            foreach (ISubsystem subsystem in _subsystems.Values)
            {
                try
                {
                    if (subsystem.CurrentSystemState == SubsystemState.Running)
                    {
                        UnloadSubsystemFromNamespace("OnlineGame.Core.Processes");
                        subsystem.Stop();
                        
                        GameDirector.Instance.Stop();
                        Scribe.Notification($"Subsystem {subsystem.Name} stopped");
                    }
                    else
                    {
                        Scribe.Notification($"StopAll() in SystemWizard had an issue with trying to stop subsystem: {subsystem.Name}");
                    }
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
                : throw new KeyNotFoundException($"Subsystem {name} not found");
        }

        public void Shutdown()
        {
            try
            {
                UnloadSubsystemFromNamespace("");
                StopAll();
                
                SocketWizard.Instance.Stop();
                Sentinel.Instance.Stop();

                Scribe.Scry("Shutting down subsystems...");
                Scribe.Close();
                Scribe.Scry("Application shutdown complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message} in {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Loads all subsystems implementing ISubsystem from the specified namespace.
        /// </summary>
        /// <param name="namespaceName">The namespace to scan for subsystems.</param>
        private void LoadSubsystemsFromNamespace(string namespaceName)
        {
            Console.WriteLine($"LoadSubsystemsFromNameSpace {namespaceName}");
            // Get all types in the current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Find types implementing ISubsystem in the specified namespace
            IEnumerable<Type> subsystemTypes = assembly.GetTypes()
                .Where(type => type.Namespace == namespaceName &&
                               typeof(ISubsystem).IsAssignableFrom(type) &&
                               !type.IsAbstract && type.IsClass);

            foreach (Type subsystemType in subsystemTypes)
            {
                // Get the singleton instance (assuming the convention is Instance property)
                PropertyInfo? instanceProperty = subsystemType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

                if (instanceProperty?.GetValue(null) is ISubsystem subsystem)
                {
                    _subsystems[subsystem.Name] = subsystem;
                }
            }
        }

        /// <summary>
        /// Helps with automating shutdown of requires components/processes.
        /// </summary>
        /// <param name="nameSpace"></param>
        private void UnloadSubsystemFromNamespace(string nameSpace)
        {
            try
            {
                // Find subsystems matching the specified namespace
                var subsystemsToRemove = _subsystems
                    .Where(kv => kv.Value.GetType().Namespace == nameSpace)
                    .Select(kv => kv.Key)
                    .ToList(); // ToList ensures we avoid modifying the dictionary during iteration

                if (subsystemsToRemove.Count == 0)
                {
                    Scribe.Notification($"No subsystems found in namespace: {nameSpace}");
                    return;
                }

                // Remove subsystems and clean up
                foreach (var subsystemName in subsystemsToRemove)
                {
                    if (_subsystems.TryRemove(subsystemName, out var subsystem))
                    {
                        // Unsubscribe from events
                        subsystem.StateChanged -= OnSubsystemStateChanged;
                        Scribe.Notification($"Subsystem '{subsystemName}' removed from namespace '{nameSpace}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, $"Error unloading subsystems from namespace: {nameSpace}");
            }
        }


        private void OnSubsystemStateChanged(object? sender, SystemEventArgs e)
        {
            Notify(e.Type, e.Source, e.Message);
        }

        private void Notify(string type, string source, string message)
        {
            SystemEventArgs eventArgs = new(type, source, message);
            lock (_eventLock)
            {
                SafeNotificationSent?.Invoke(this, eventArgs);
            }
            Scribe.Notification($"Event Notification: {type} - {source} - {message}");
        }
    }
}
