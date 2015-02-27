﻿// ****************************************************************************
// <copyright file="NexusCore.cs" company="ALICERAIN">
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
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Nexus.Messages;

namespace Nexus
{
    public class NexusCore : NexusBase
    {
        private const string coreComponentName = "Nexus.Core";

        private readonly Random random = new Random();
        private readonly string appDomainPrefix;


        private const int shutdownSeconds = 5;

        /// <summary> Path to the directory containing component assemblies. </summary>
        private readonly string componentsPath = String.Format(".{0}plugins", Path.DirectorySeparatorChar);

        private int toShutdownComponentsCount;
        private readonly object _toShutdownComponentsCountLock = new object();
        public bool ShuttingDown { get; private set; }

        #region Collection fields

        /// <summary> Dictionary in which keys are method descriptors
        /// and values are the components in which those methods are registered. </summary>
        private Dictionary<MethodDescriptor, NexusComponent> componentMethods
            = new Dictionary<MethodDescriptor, NexusComponent>();
        
        /// <summary> Dictionary in which keys are component names and values are the components themselves. </summary>
        private readonly Dictionary<string, NexusComponent> components = new Dictionary<string, NexusComponent>();

        /// <summary> List of component names to be unloaded on the next cleanup. </summary>
        private readonly List<string> componentsMarkedForRemoval = new List<string>();

        #region Lock objects

        /// <summary> Lock object for componentMethods. </summary>
        private readonly object _componentMethodsLock = new object();

        /// <summary> Lock object for components. </summary>
        private readonly object _componentsLock = new object();

        /// <summary> Lock object for componentsMarkedForRemoval. </summary>
        private readonly object _componentsMarkedForRemovalLock = new object();

        #endregion

        #endregion

        /// <summary> Creates an instance of the NexusCore class and loads components. </summary>
        public NexusCore()
        {
            Console.WriteLine("{0}Starting Nexus Core.{0}", Environment.NewLine);

            appDomainPrefix = String.Format("[{0}]:", random.Next(10000, 99999));
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Thread.CurrentThread.Name = "Nexus Core thread";

            // Only bother loading components if the plugins folder is there.
            if (!Directory.Exists(componentsPath))
                Console.WriteLine("Plugins directory not found.");
            else
            {
                Console.WriteLine("{0}Loading components.{0}", Environment.NewLine);

                string[] files = Directory.GetFiles(componentsPath, "*.dll");

                // Iterate through the assemblies.
                foreach (string file in files)
                {

                    // Get the filename sans the path and extension.
                    // Assume the filename is also the assembly name.
                    // Thus, renaming a component .dll will prevent it from loading.
                    int dirPos = file.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                    int assemblyNameLength = file.LastIndexOf('.') - dirPos;
                    var assemblyName = file.Substring(dirPos, assemblyNameLength);


                    // Create new AppDomain for this component.
                    var newAppDomainSetup
                        = new AppDomainSetup
                              {
                                  ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                                  PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory + componentsPath + ";" +
                                                   AppDomain.CurrentDomain.BaseDirectory + componentsPath +
                                                   Path.DirectorySeparatorChar + assemblyName
                              };
                    AppDomain newDomain = AppDomain.CreateDomain(appDomainPrefix+assemblyName, null, newAppDomainSetup);

                    // ComponentLoader PROXY -- the actual loader instance exists in the new AppDomain.
                    // Anything done in it will be executed there.
                    var loader = (ComponentLoader) newDomain.CreateInstanceAndUnwrap(
                        "NexusCommon", "Nexus.ComponentLoader");

                    // Try to load the component
                    Debug.WriteLine("Loading from assembly " + assemblyName);
                    NexusComponent component = null;
                    try
                    {
                        component = loader.LoadComponent(assemblyName);
                    }
                    catch (TimeoutException ex) // Constructor timed out.
                    {
                        Console.WriteLine("Timeout occurred loading {0}: {1}",assemblyName,ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("Invalid operation performed while loading {0}: {1}", assemblyName, ex.Message);
                    }
                    catch (EntryPointNotFoundException ex) // No NexusComponent instance found.
                    {
                        Console.WriteLine("Assembly {0} does not contain a NexusComponent.", assemblyName);
                    }
                    catch (TypeLoadException ex)
                    {
                        Console.WriteLine("");
                        Console.WriteLine(
                            "Assembly {0} threw an unhandled {1} during construction:",
                            assemblyName, ex.InnerException.GetType().Name);
                        Console.WriteLine(" Inner exception: " + ex.InnerException);
                        Console.WriteLine("");
                    }


                    // Load failed
                    if (component == null)
                    {
                        Debug.WriteLine("Couldn't load assembly " + assemblyName);
                        AppDomain.Unload(newDomain);
                        continue;
                    }


                    lock (_componentsLock)
                    {

                        if (components.ContainsKey(component.ToString()))
                        {
                            FailComponentLoad(component, "Component already loaded.");
                            continue;
                        }

                        // Inject NexusCore into the component and verify it hasn't been tampered with.
                        if (!component.SetCore(this))
                        {
                            FailComponentLoad(component, String.Format("Component {0}.{1} already has core set.",
                                                                assemblyName, component.TypeName));
                            continue;
                        }

                        // All good! we can official mark it as loaded.
                        components[component.ToString()] = component;
                    } // lock (_componentsLock)
                    Console.WriteLine("Loaded component " + component);
                } // foreach (string file in files)

                Console.WriteLine("{0}Registering methods.{0}", Environment.NewLine);
                
                // Register exposed methods.
                lock (_componentsLock)
                {
                    foreach (NexusComponent component in components.Values)
                    {
                        // Register the component's methods.

                        // Check to see if the component's methods have already been registered.
                        List<MethodDescriptor> methods = component.GetExposedMethods();
                        bool failed = false;
                        lock (_componentMethodsLock)
                        {
                            foreach (MethodDescriptor method in methods)
                            {
                                if (!componentMethods.ContainsKey(method))
                                    continue;
                                // Already registered.
                                Console.WriteLine("Method in {0} already registered: {1}", component, method.ToString(false));
                                lock(_componentsMarkedForRemovalLock)
                                {
                                    componentsMarkedForRemoval.Add(component.ToString());
                                }
                                failed = true;
                                break;
                            }
                            if (failed) continue;


                            // Register the component's methods.
                            foreach (MethodDescriptor method in methods)
                            {
                                componentMethods[method] = component;
                                Console.WriteLine("Registered " + method.ToString(false));
                            }
                        }
                    }

                    // Cleanup any plugins that have been marked for removal.
                    CleanupComponents();
                }

                Console.WriteLine("{0}Starting components.{0}", Environment.NewLine);
                
                // Start all components.
                lock (_componentsLock)
                {
                    foreach (NexusComponent component in components.Values)
                    {
                        Console.WriteLine("Starting component {0}", component);
                        var c = component;
                        var t = new Thread(()=>Start(c)) {Name = component + " StartComponent()"};
                        t.Start();
                        
                    }
                }
                
                Console.WriteLine("{0}Startup complete.{0}", Environment.NewLine);

            } // if (!Directory.Exists(pluginsPath)) {} else {
        }

        private void Start(NexusComponent component)
        {
            string componentName = component.ToString();
            try
            {
                component.Startup();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "{3}Component {0} threw an unhandled {1} during startup:{3}{2}{3}",
                    componentName, ex.GetType().Name, ex, Environment.NewLine);
                var message = new NexusComponentCrashedEvent(componentName, ex.Message, ex.ToString());
                message.SourceComponent = coreComponentName;
                SendMessage(message);
                UnloadComponent(componentName);
            }
        }
        
        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
                Console.WriteLine("EXCEPTION: {0} - {1}", sender, e);

                if (ShuttingDown)
                    return;

                var senderDomain = sender as AppDomain;
                if (senderDomain != null)
                {
                    bool isComponent = senderDomain.FriendlyName.StartsWith(appDomainPrefix);

                    if (isComponent)
                    {
                        string assemblyName = senderDomain.FriendlyName.Substring(appDomainPrefix.Length);

                        Console.WriteLine();
                        Console.WriteLine("Exception from {0}:", assemblyName);
                        Console.WriteLine(e.ExceptionObject);
                        Console.WriteLine();

                        lock (_componentsLock)
                        {
                            NexusComponent toRemove = components.Values.FirstOrDefault(
                                component => component.AppDomain == senderDomain);
                            if (toRemove != null)
                            {
                                UnloadComponent(toRemove.ToString());
                                //components.Remove(toRemove.ToString());
                                //AppDomain.Unload(senderDomain);
                            }
                            else
                            {
                                // Close the whole application
                                Console.WriteLine();
                                Console.WriteLine("Exception occurred in unknown appdomain. Nexus will now close.");
                                Console.WriteLine(e.ExceptionObject);
                                Console.WriteLine();
                                Shutdown();
                                AppDomain.Unload(AppDomain.CurrentDomain);
                            }

                        }
                    }

                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Exception occurred and Nexus will now close.");
                        Console.WriteLine(e.ExceptionObject);
                        Console.WriteLine();
                        //Shutdown();
                        //AppDomain.Unload(AppDomain.CurrentDomain);

                    }
                }

        }

// public NexusCore

        /// <summary> Unload all components marked for deletion. </summary>
        private void CleanupComponents()
        {
            lock (_componentsMarkedForRemovalLock)
            {
                foreach (string componentName in componentsMarkedForRemoval)
                    UnloadComponent(componentName);
            }
        }

        /// <summary> Unloads a component. </summary>
        public override void UnloadComponent(string componentName)
        {
            AppDomain toUnload = null;
            lock (_componentsLock)
            {
                lock (_componentMethodsLock)
                {
                    // Fetch matching component to remove.
                    if (components.ContainsKey(componentName))
                    {
                        NexusComponent component = components[componentName];
                        component.Shutdown();
                        components.Remove(componentName);
                        toUnload = component.AppDomain;
                        Console.WriteLine("{1}Unloaded component {0}{1}", componentName, Environment.NewLine);


                        componentMethods = componentMethods.Where(cm => cm.Value != component).ToDictionary(cm=>cm.Key, cm=>cm.Value);
                    }
                    else
                    {
                        Console.WriteLine("{0}Tried to unload component {1} but it is not loaded.{0}",
                            Environment.NewLine, componentName);
                    }



                }
            }

            if (toUnload == null) return;

            try
            {
                AppDomain.Unload(toUnload);
            }
            catch (AppDomainUnloadedException)
            {
                Debug.WriteLine("AppDomain for {0} already unloaded.", componentName);
            }
            catch (CannotUnloadAppDomainException ex)
            {
                Debug.WriteLine("Cannot unload appdomain for {0}!", componentName);
            }

        }

        /// <summary> Unloads a component in the event that an error occurs during load. </summary>
        /// <param name="component"> The NexusComponent that failed to load. </param>
        /// <param name="reason"> The reason the component failed to load. </param>
        private void FailComponentLoad(NexusComponent component, string reason)
        {
            try
            {
                AppDomain.Unload(component.AppDomain);
            }
            catch (AppDomainUnloadedException)
            {
            }

            Debug.WriteLine("Couldn't load component {0}: {1}", component, reason);
        }
        
        /// <summary> Calls a method exposed by a NexusComponent with a return value. </summary>
        /// <typeparam name="T"> Return value type. </typeparam>
        /// <param name="source"> NexusComponent that called the method. Currently has no effect. </param>
        /// <param name="methodName"> Name of method to call. </param>
        /// <param name="returnValue"> Will be set to the method's return value. </param>
        /// <param name="parameters"> Array of objects containing method parameters. </param>
        /// <returns> true if the method has been registered, false if it has not. </returns>
        public override Exception CallMethod<T>(NexusComponent source, string methodName, out T returnValue, object[] parameters)
        {
            // Create a method descriptor matching the provided method details.
            var methodDescriptor = new MethodDescriptor(methodName, parameters, typeof(T));
            NexusComponent component;
            lock (_componentMethodsLock)
            {
                // Verify that a component has registered a method matching the given descriptor.
                if (!componentMethods.ContainsKey(methodDescriptor))
                {
                    returnValue = default(T);
                    var notFoundMessage = new NexusMethodNotFoundEvent(source.ToString(), methodName);
                    notFoundMessage.SourceComponent = coreComponentName;
                    SendMessage(notFoundMessage);
                    return new MethodNotFoundException(methodName);
                }
                // Fetch the NexusComponent that registered the method descriptor.
                component = componentMethods[methodDescriptor];
            }

            try
            {
                return component.CallMyMethod(methodName, out returnValue, parameters);
            }
            catch (Exception ex)
            {
                // TODO: change to log code.
                Console.WriteLine("Exception occurred calling {0} : {1}", methodName, ex.Message);
                returnValue = default(T);
                UnloadComponent(component.ToString());
                var message = new NexusComponentCrashedEvent(component.ToString(), ex.Message, ex.ToString());
                message.SourceComponent = coreComponentName;
                SendMessage(message);
                return ex;
            }
        }

        /// <summary> Calls a method exposed by a NexusComponent without a return value. </summary>
        /// <param name="source"> NexusComponent that called the method. Currently has no effect. </param>
        /// <param name="methodName"> Name of method to call. </param>
        /// <param name="parameters"> Array of objects containing method parameters. </param>
        /// <returns> true if the method has been registered, false if it has not. </returns>
        public override Exception CallMethod(NexusComponent source, string methodName, object[] parameters)
        {
            // Create a method descriptor matching the provided method details.
            var methodDescriptor = new MethodDescriptor(methodName, parameters);
            NexusComponent component;
            lock (_componentMethodsLock)
            {
                // Verify that a component has registered a method matching the given descriptor.
                if (!componentMethods.ContainsKey(methodDescriptor))
                {
                    var message = new NexusMethodNotFoundEvent(source.ToString(), methodName);
                    message.SourceComponent = coreComponentName;
                    SendMessage(message);
                    return new MethodNotFoundException(methodName);
                }
                // Fetch the NexusComponent that registered the method descriptor.
                component = componentMethods[methodDescriptor];
            }

            try
            {
                return component.CallMyMethod(methodName, parameters);
            }
            catch (Exception ex)
            {
                // TODO: change to log code.
                Console.WriteLine("Exception occurred calling {0} : {1}", methodName, ex.Message);
                UnloadComponent(component.ToString());
                var message = new NexusComponentCrashedEvent(component.ToString(), ex.Message, ex.ToString());
                message.SourceComponent = coreComponentName;
                SendMessage(message);
                return ex;
            }
        }

        #region Messenger

        public override void SendMessage<TMessage>(TMessage message, object token = null)
        {
            lock (_componentsLock)
            {
                foreach (NexusComponent component in components.Values)
                    component.ReceiveMessage(message, token);
            }
        }

        public override string Marco()
        {
            return "Polo";
        }

        #endregion

        public void Shutdown()
        {
            ShuttingDown = true;
            toShutdownComponentsCount = components.Count;
            foreach (var component in components)
                ThreadPool.QueueUserWorkItem(ShutdownComponent, component.Key);

            // Give plugins a small amount of time to shut down before we cut 'em off.
            DateTime shutdownStart = DateTime.Now;
            //while (toShutdownComponentsCount > 0 &&
            //    DateTime.Now < shutdownStart.AddSeconds(shutdownSeconds))
            //    Thread.Sleep(500); // 1/2 second.

            // Waited long enough, shut 'em down
            
            //foreach (NexusComponent component in components.Values)
            //    AppDomain.Unload(component.AppDomain);
        }

        private void ShutdownComponent(object componentName)
        {
            var componentString = componentName as string;
            if (componentString == null)
                throw new ArgumentException(
                    "Must provide Component name.", "componentName");

            if (!components.ContainsKey(componentString))
                throw new ArgumentException(
                    "No such component " + componentString, "componentName");

            var component = components[componentString];

            if (component==null)
                throw new InvalidOperationException("Component " + componentString + " is null.");

            component.Shutdown();

            lock (_toShutdownComponentsCountLock)
            {
                toShutdownComponentsCount -= 1;
            }
        }


    }
}
