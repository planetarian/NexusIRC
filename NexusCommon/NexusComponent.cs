﻿// ****************************************************************************
// <copyright file="NexusComponent.cs" company="ALICERAIN">
// Copyright © ALICERAIN 2012
// </copyright>
// ****************************************************************************
// <author>Ash Wolford</author>
// <email>piro@live.com</email>
// <date>2012-03-26</date>
// <project>NexusIRC.Nexus</project>
// <web>http://pirocast.net/</web>
// <license>
// All rights reserved, until I decide on an appropriate license.
// </license>
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nexus.Messages;

namespace Nexus
{
    public abstract class NexusComponent : MarshalByRefObject
    {
        /// <summary> Gets the AppDomain containing the component. </summary>
        public AppDomain AppDomain
        {
            get { return AppDomain.CurrentDomain; }
        }

        /// <summary> Gets the actual name of the component type.
        /// For use across different assemblies/appdomains.</summary>
        public string TypeName
        {
            // No lock needed. Result from GetType().Name never changes.
            get { return _typeName ?? (_typeName = GetType().Name); }
        }

        /// <summary> Gets a list of MethodDescriptor objects representing methods exposed
        /// by this component. </summary>
        /// <returns> Array of MethodDescriptor objects this component exposes. </returns>
        public List<MethodDescriptor> GetExposedMethods()
        {
            lock (_exposedMethodsLock)
            {
                // We don't want anything modifying this, so we'll hand the caller a clone.
                return ExposedMethods.Select(method => method.Key).ToList();
            }
        }  



        /// <summary> Gets a collection of MethodDescriptor objects representing
        /// the component's public declared methods. </summary>
        private Dictionary<MethodDescriptor, MethodInfo> PublicMethods
        {
            get
            {
                return _publicMethods ??
                       (_publicMethods = MethodDescriptor.DictionaryFromType(GetType()));
            }
        }

        /// <summary> Gets a list of MethodDescriptor objects representing methods exposed
        /// to NexusCore by this component.
        /// External methods should use GetExposedMethods() which creates a copy. </summary>
        private Dictionary<MethodDescriptor, MethodInfo> ExposedMethods
        {
            get
            {
                // Locked to prevent different threads from getting different list objects.
                lock (_exposedMethodsLock)
                {
                    return _exposedMethods
                           ?? (_exposedMethods = new Dictionary<MethodDescriptor, MethodInfo>());
                }
            }
        }

        /// <summary> Gets a list of method names that should not be loaded when  </summary>
        private readonly string[] excludedMethods = new[]
                                                     {
                                                         "Startup",
                                                         "Shutdown"
                                                     };

        #region Backing fields. May be locked; don't access directly.

        /// <summary> Backing variable for MethodDescriptors. Locked. </summary>
        private Dictionary<MethodDescriptor, MethodInfo> _exposedMethods;

        /// <summary> Backing variable for MethodDescriptors. </summary>
        private Dictionary<MethodDescriptor, MethodInfo> _publicMethods;

        /// <summary> Name of this object, to prevent repeated GetType() calls. </summary>
        private string _typeName;

        #endregion

        /// <summary> NexusCore that owns this component. </summary>
        private NexusBase _core;

        #region Lock objects

        /// <summary> Lock object for _publicMethods. </summary>
        private readonly object _exposedMethodsLock = new object();

        /// <summary> Lock object for _core. </summary>
        private readonly object _coreLock = new object();

        #endregion



        /// <summary> Sets the instance of NexusBase that owns this component.
        /// This is normally a proxy object to a separate AppDomain.
        /// NexusCore will refuse to load the plugin if a
        /// different NexusBase object is provided. </summary>
        /// <param name="core"> NexusBase object to set as the core.
        /// Should actually be the main NexusCore object.
        /// Anything else will be rejected by NexusCore. </param>
        /// <returns> false if the core is already set.
        /// true if it has not yet been set. </returns>
        public bool SetCore(NexusBase core)
        {
            // Prevent multiple threads from setting the core at the same time.
            lock (_coreLock)
            {
                if (_core != null)
                    return false;
                _core = core;
                return true;
            }
        }

        /// <summary> Method is called after the component has been loaded and is ready to begin execution. </summary>
        public virtual void Startup()
        {
        }

        /// <summary> Method is called when the user has requested a shutdown.
        /// Component should clean up and release all resources in use.
        /// Immediately after this method finishes executing,
        /// the component's AppDomain will be unloaded. </summary>
        public virtual void Shutdown()
        {
            
        }

        #region Methods

        /// <summary> Attempts to call a method on this NexusComponent with no return value. </summary>
        /// <param name="methodName"> Name of the method to execute. </param>
        /// <param name="p"> Parameters supplied to the method. </param>
        /// <returns> true if the requested method is available for use. </returns>
        public Exception CallMyMethod(string methodName, params object[] p)
        {
            if (_core == null)
                throw new InvalidOperationException(
                    "Component tried to call a plugin method before it has been fully constructed.");

            // Create a method descriptor with which we'll match against the collection of exposed methods.
            var descriptor = new MethodDescriptor(methodName, p);

            // See if a matching method has been exposed.
            if (!ExposedMethods.ContainsKey(descriptor))
                throw new InvalidOperationException(
                    String.Format("Attempted to call method {0} which has not been exposed by {1}.",
                    methodName, TypeName));

            // Execute the method.
            try
            {
                ExposedMethods[descriptor].Invoke(this, p);
            }
            catch (TargetInvocationException ex)
            {
                SendMessage(new NexusMethodExceptionEvent(TypeName, methodName, ex));
                return ex;
            }

            return null;
        }

        /// <summary> Attempts to call a method on this NexusComponent with no return value. </summary>
        /// <param name="methodName"> Name of the method to execute. </param>
        /// <param name="returnValue"> Return value for the method. </param>
        /// <param name="p"> Parameters supplied to the method. </param>
        /// <returns> true if the requested method is available for use. </returns>
        public Exception CallMyMethod<T>(string methodName, out T returnValue, params object[] p)
        {
            returnValue = default(T);

            if (_core == null)
                throw new InvalidOperationException(
                    "Component tried to call a plugin method before it has been fully constructed.");

            // Create a method descriptor with which we'll match against the collection of exposed methods.
            var descriptor = new MethodDescriptor(methodName, p, typeof(T));

            // See if a matching method has been exposed.
            if (!ExposedMethods.ContainsKey(descriptor))
                throw new InvalidOperationException(
                    String.Format("Attempted to call method {0} which has not been exposed by {1}.",
                    methodName, TypeName));

            try
            {
                // Execute the method and store its return value.
                returnValue = (T) ExposedMethods[descriptor].Invoke(this, p);

            }
            catch (TargetInvocationException ex)
            {
                SendMessage(new NexusMethodExceptionEvent(TypeName, methodName, ex));
                return ex;
            }


            return null;
        }

        /// <summary> Attempts to call a NexusComponent method with a return value. </summary>
        /// <typeparam name="T"> Return value type of the method to be called. </typeparam>
        /// <param name="methodName"> Name of the method to execute. </param>
        /// <param name="returnValue"> Return value from the method. </param>
        /// <param name="p"> Parameters supplied to the method. </param>
        /// <returns> true if the requested method is available for use. </returns>
        public Exception CallMethod<T>(string methodName, out T returnValue, params object[] p)
        {
            if (_core == null)
                throw new InvalidOperationException(
                    "Component tried to call a plugin method before it has been fully constructed.");

            return _core.CallMethod(this, methodName, out returnValue, p);
        }

        /// <summary> Attempts to call a NexusComponent method with no return value. </summary>
        /// <param name="methodName"> Name of the method to execute. </param>
        /// <param name="p"> Parameters supplied to the method. </param>
        /// <returns> true if the requested method is available for use. </returns>
        public Exception CallMethod(string methodName, params object[] p)
        {
            if (_core == null)
                throw new InvalidOperationException(
                    "Component tried to call a plugin method before it has been fully constructed.");

            return _core.CallMethod(this, methodName, p);
        }
        
        /// <summary> Exposes all public methods. </summary>
        protected void ExposeMethods()
        {
            lock (_exposedMethodsLock)
            {
                foreach (KeyValuePair<MethodDescriptor,MethodInfo> method in PublicMethods)
                    // Don't expose excluded methods.
                    if (!excludedMethods.Contains(method.Value.Name))
                        ExposeMethod(method.Key, method.Value);
            }
        }

        /// <summary> Exposes all public methods under a different component name. </summary>
        /// <param name="componentName"></param>
        protected void ExposeMethodsAs(string componentName)
        {
            lock(_exposedMethodsLock)
            {
                foreach (KeyValuePair<MethodDescriptor, MethodInfo> method in PublicMethods)
                    // Don't expose excluded methods.
                    if (!excludedMethods.Contains(method.Value.Name))
                    {
                        MethodSignature signature = method.Key.Signature;
                        var newDescriptor = new MethodDescriptor(
                            String.Format("{0}.{1}", componentName, method.Value.Name),
                            signature.ParameterTypes, method.Key.ReturnType);
                        ExposeMethod(newDescriptor, method.Value);
                    }
            }
        }

        /// <summary> Exposes all public methods with the given methodName. </summary>
        /// <param name="methodName"> Name of public methods to expose. </param>
        protected void ExposeMethods(string methodName)
        {
            if (methodName.Contains("."))
                throw new Exception(
                    "Component tried to expose an external method without providing a method descriptor.");

            // Prepend the component name if it wasn't provided in the methodName.            
            methodName = String.Format("{0}.{1}", TypeName, methodName);

            // Count how many matching methods we find.
            int count = 0;
            lock (_exposedMethodsLock)
            {
                foreach (KeyValuePair<MethodDescriptor, MethodInfo> method in PublicMethods)
                {
                    // Make sure the name matches.
                    if (method.Key.Signature.MethodName != methodName)
                        continue;
                    
                    // Go for it.
                    ExposeMethod(method.Key, method.Value);
                    count++;
                }
            }
            Debug.WriteLine("{0} method{1} named {2} detected and exposed.",
                count, count == 1 ? "" : "s", methodName);
        }

        /// <summary> Exposes a method with the given name, using a delegate. </summary>
        /// <param name="methodName"> Name of the method that the delegate will be exposed as. </param>
        /// <param name="methodDelegate"> Method delegate to be exposed. </param>
        protected void ExposeMethod(string methodName, Delegate methodDelegate)
        {
            // Get a descriptor of the delegate that was provided.
            var delDescriptor = new MethodDescriptor(methodDelegate.Method);
            // Create a new descriptor with the method name provided,
            // and signature/return type of the delegate provided.
            var methodDescriptor = new MethodDescriptor(methodName, delDescriptor.Signature.ParameterTypes,
                                                        delDescriptor.ReturnType);

            // Expose the method.
            ExposeMethod(methodDescriptor, methodDelegate.Method);
        }

        /// <summary> Exposes a method with the given MethodDescriptor. </summary>
        /// <param name="methodDescriptor"> MethodDescriptor describing the method to expose. </param>
        /// <param name="methodInfo"> MethodInfo referencing to the method to use. </param>
        protected void ExposeMethod(MethodDescriptor methodDescriptor, MethodInfo methodInfo)
        {
            if (_core != null)
                throw new InvalidOperationException(
                    "Component tried to expose a method after it has already been constructed.");

            if (methodInfo == null)
                throw new InvalidOperationException(
                    "Component tried to expose a method without providing the method itself.");

            if (methodInfo.IsGenericMethod)
                throw new InvalidOperationException(
                    "Component tried to expose a generic method.");

            lock (_exposedMethodsLock)
            {
                if (ExposedMethods.ContainsKey(methodDescriptor))
                    throw new InvalidOperationException(
                        "Component tried to expose a method that has already been exposed.");

                // All good, go ahead and expose it.
                ExposedMethods.Add(methodDescriptor, methodInfo);
                Debug.WriteLine("Exposed " + methodDescriptor.ToString(false));
            }
        }

        #endregion

        #region Messaging

        private readonly Dictionary<Type, List<MessageRegistrationInfo>> _messageListeners
            =  new Dictionary<Type, List<MessageRegistrationInfo>>();

        private readonly object _messageListenersLock = new object();

        /// <summary> Sends a message (with an optional token) to this NexusComponent. </summary>
        /// <typeparam name="TMessage"> Type of the message being sent. </typeparam>
        /// <param name="message"> Message to be sent. </param>
        /// <param name="token"> Optional token. Only listeners with a matching token will receive this message. </param>
        public void ReceiveMessage<TMessage>(TMessage message, object token = null)
        {
            Type messageType = typeof (TMessage);
            lock (_messageListenersLock)
            {
                foreach (Type type in _messageListeners.Keys)
                {
                    // Determine if a message should be executed for listeners including derived types.
                    bool isDerived = type.IsAssignableFrom(messageType);

                    // Look through message listener registration info.
                    foreach (MessageRegistrationInfo regInfo in _messageListeners[type])
                    {
                        // Receive the message only if the type matches or is derived.
                        if ((!ReferenceEquals(type, messageType) && (!regInfo.IncludeDerived || !isDerived)) ||
                            ((token != null || regInfo.Token != null) && (token == null || !token.Equals(regInfo.Token))))
                            continue;
                        
                        // Access the WeakAction via its interface to execute its action.
                        var executeAction = regInfo.WeakAction as IExecuteWithObject;
                        Debug.Assert(executeAction != null,
                                     "Listeners action is not an IExecuteWithObject. This should not happen.");

                        // Start a new worker thread for the listener's action.
                        Task.Factory.StartNew(() => DoReceiveMessage(executeAction, message));
                    }
                }
            }
        }

        private void DoReceiveMessage<TMessage>(IExecuteWithObject exObj, TMessage message)
        {
            try
            {
                exObj.ExecuteWithObject(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{3}Exception occurred in {0} when receiving message {1} : {2}{3}",
                                  this, typeof (TMessage), ex, Environment.NewLine);
                SendMessage(new NexusComponentCrashedEvent(ToString(), ex.Message, ex.ToString()));
                _core.UnloadComponent(ToString());
            }
        }


        /// <summary> Sends a message (with an optional token) to all NexusComponents listening for that message/token. </summary>
        /// <typeparam name="TMessage"> Type of the message being sent. </typeparam>
        /// <param name="message"> Message to be sent. </param>
        /// <param name="token"> Optional token. Only listeners with a matching token will receive this message. </param>
        public void SendMessage<TMessage>(TMessage message, object token = null)
        {
            var nexusEvent = message as NexusEvent;
            if (nexusEvent != null)
            {
                if (nexusEvent.SourceComponent != null)
                    throw new InvalidOperationException(
                        "SourceComponent cannot be set manually.");
                nexusEvent.SourceComponent = ToString();
            }

            _core.SendMessage(message, token);
        }

        public void SendMessage(string messageName, Dictionary<string, object> data,
            object token = null)
        {
            var message = new NexusEvent(messageName, data);
            _core.SendMessage(message, token);
        }

        public void SendMessage(string messageName, object data,
            object token = null)
        {
            var message = new NexusEvent(messageName, data);
            _core.SendMessage(message, token);
        }

        /// <summary> Registers a listener for a message with an optional token. </summary>
        /// <typeparam name="TMessage"> Type of the message to listen for. </typeparam>
        /// <param name="action"> Action to execute upon receipt of a matching message. </param>
        /// <param name="token"> Optional token. Only messages with a matching token will be received by this listener. </param>
        /// <param name="receiveDerivedMessagesToo"> Whether to also receive messages with types derived from the specified message type. </param>
        protected void RegisterListener<TMessage>(Action<TMessage> action, object token, bool receiveDerivedMessagesToo = false)
        {
            // Get the message type.
            var messageType = typeof (TMessage);
            lock (_messageListenersLock)
            {
                // If we're not listening for this type already, add a list for listeners.
                if (!_messageListeners.ContainsKey(messageType))
                    _messageListeners.Add(messageType, new List<MessageRegistrationInfo>());
                
                // Set up the listener registration info.
                var weakAction = new WeakAction<TMessage>(this, action);
                var messageInfo = new MessageRegistrationInfo(weakAction, token, receiveDerivedMessagesToo);

                // Add it to the listeners list.
                _messageListeners[messageType].Add(messageInfo);
            }

            // Cleanup.
            // TODO: is this necessary here?
            CleanupListeners();
        }

        /// <summary> Registers a listener for a message. </summary>
        /// <typeparam name="TMessage"> Type of the message to listen for. </typeparam>
        /// <param name="action"> Action to execute upon receipt of a matching message. </param>
        /// <param name="receiveDerivedMessagesToo"> Whether to also receive messages with types derived from the specified message type. </param>
        protected void RegisterListener<TMessage>(Action<TMessage> action, bool receiveDerivedMessagesToo)
        {
            RegisterListener(action, null, receiveDerivedMessagesToo);
        }

        /// <summary> Registers a listener for a message. </summary>
        /// <typeparam name="TMessage"> Type of the message to listen for. </typeparam>
        /// <param name="action"> Action to execute upon receipt of a matching message. </param>
        protected void RegisterListener<TMessage>(Action<TMessage> action)
        {
            RegisterListener(action, null);
        }

        /// <summary> Unregisters a listener registered with a given action and optional token. </summary>
        /// <typeparam name="TMessage"> Type of message the listener was registered for. </typeparam>
        /// <param name="action"> Action that was registered. </param>
        /// <param name="token"> Token that was optionally provided when the listener was registered. </param>
        protected void UnregisterListener<TMessage>(Action<TMessage> action, object token = null)
        {
            // If we're not listening for this message (or anything else), nothing to do.
            Type messageType = typeof (TMessage);
            if (_messageListeners.Count == 0
                || !_messageListeners.ContainsKey(messageType))
                return;

            lock (_messageListenersLock)
            {
                foreach (MessageRegistrationInfo info in _messageListeners[messageType])
                {
                    // Cast to generic WeakAction;
                    // if it fails then it was registered for a different type of message.
                    // (this shouldn't happen!)
                    var weakActionCasted = info.WeakAction as WeakAction<TMessage>;
                    Debug.Assert(weakActionCasted != null,
                        "WeakAction<T> shouldn't magically change its type after registration.");

                    // Make sure the info matches, mark for removal.
                    if ((action == null || action == weakActionCasted.Action)
                        && (token == null || token == info.Token ))
                        info.WeakAction.MarkForDeletion();
                }
            }

            // Remove listeners marked for removal.
            CleanupListeners();
        }

        /// <summary> Unregisters all message listeners currently registered to this NexusComponent. </summary>
        protected void UnregisterAllListeners()
        {
            // If we're not listening for anything anyway, nothing to do.
            if (_messageListeners.Count == 0) return;

            lock (_messageListenersLock)
            {
                foreach (Type messageType in _messageListeners.Keys)
                {
                    foreach (MessageRegistrationInfo info in _messageListeners[messageType])
                    {
                        WeakAction weakAction = info.WeakAction;

                        if (weakAction != null)
                            weakAction.MarkForDeletion();
                    }
                }
            }

            // Remove listeners marked for removal.
            CleanupListeners();
        }

        /// <summary> Removes listeners marked for deletion. </summary>
        private void CleanupListeners()
        {
            lock (_messageListenersLock)
            {
                // Iterate through the types being listened for.
                var listsToRemove = new List<Type>();
                foreach (KeyValuePair<Type, List<MessageRegistrationInfo>> regList in _messageListeners)
                {
                    // Get list of types for which the action has been nulled
                    // or whose object reference is no longer alive.
                    // TODO: not sure if this is actually possible? =/
                    var typesToRemove = regList.Value.Where(
                        item => item.WeakAction == null || !item.WeakAction.IsAlive).ToList();

                    // Remove the dead message registrations from the type's registration list.
                    foreach (MessageRegistrationInfo regInfo in typesToRemove)
                        regList.Value.Remove(regInfo);

                    // If there are no more registrations for this type, mark it for removal.
                    if (regList.Value.Count == 0)
                        listsToRemove.Add(regList.Key);
                }

                // Remove types which no longer have any listeners.
                foreach (Type key in listsToRemove)
                    _messageListeners.Remove(key);
            }
        }
        
        #endregion

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
