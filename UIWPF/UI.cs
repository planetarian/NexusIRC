using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Ioc;
using Nexus;
using Nexus.Messages;
using UIWPF.View;
using UIWPF.ViewModel;

namespace UIWPF
{
    public class UI : NexusComponent
    {
        private Thread uiThread;
        private Dispatcher dispatcher;
        private static readonly object _startupLock = new object();

        internal static UI Instance { get; private set; }

        private IRCViewModel _viewModel;
        internal IRCViewModel ViewModel
        {
            get { return _viewModel ?? (_viewModel = Locator.IRC); }
        }

        private ViewModelLocator _locator;
        internal ViewModelLocator Locator
        {
            get { return _locator ?? (_locator = new ViewModelLocator()); }
        }



        public override void Startup()
        {
            lock (_startupLock)
            {
                if (Instance != null)
                    throw new Exception("Cannot load multiple instances of UI");
                Instance = this;
            }

            
             (Application.Current ?? new Application())
                 .Resources.Add("Locator", Locator);

            uiThread = new Thread(StartUI) {Name = "UIWPF UI Thread"};
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();

            RegisterListener<IRCConnectionCreatedEvent>(AddConnection);
            RegisterListener<IRCConnectionClosedEvent>(RemoveConnection);
        }

        private void AddConnection(IRCConnectionCreatedEvent ev)
        {
            ViewModel.Connections.Add(
                new IRCConnectionViewModel(
                    ev.EventInfo.ConnectionId, ev.EventInfo.ServerAddress));
        }

        private void RemoveConnection(IRCConnectionClosedEvent ev)
        {
            IRCConnectionViewModel vm = ViewModel.Connections
                .FirstOrDefault(cn => cn.ConnectionId == ev.EventInfo.ConnectionId);
            if (vm != null)
                ViewModel.Connections.Remove(vm);
        }

        private void StartUI()
        {
            // Keep this inside the thread to avoid weird COM issues on shutdown
            var view = new MainView();
            dispatcher = view.Dispatcher;
            view.ShowDialog();
        }

        public override void Shutdown()
        {
            if (dispatcher != null)
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        }
    }
}
