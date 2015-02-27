using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using Nexus.Messages;

namespace UIWPF.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class IRCViewModel : ViewModelBaseExtended
    {
        public string Message
        {
            get { return _message; }
            set { SetProperty(() => Message, ref _message, value); }
        }
        private string _message;

        public List<IRCConnectionViewModel> Connections
        {
            get { return _connections; }
            set { SetProperty(() => Connections, ref _connections, value); }
        }
        private List<IRCConnectionViewModel> _connections;

        
        /// <summary>
        /// Initializes a new instance of the IRCViewModel class.
        /// </summary>
        public IRCViewModel()
        {
            
        }
    }
}