﻿// ****************************************************************************
// <copyright file="ComponentLoader.cs" company="ALICERAIN">
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
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Nexus
{
    public class ComponentLoader : MarshalByRefObject
    {
#if DEBUG
        private const int constructorTimeoutSeconds = 30;
#else
        private const int constructorTimeoutSeconds = 1;
#endif

        /// <summary> NexusComponent being loaded. Shared between loader and constructor thread. </summary>
        private NexusComponent component;

        /// <summary> Lock object for the LoadComponent method. </summary>
        private readonly object _loaderLock = new object();
        
        /// <summary> Used to signal the loader when the constructor finishes. </summary>
        private readonly AutoResetEvent autoEvent = new AutoResetEvent(false);
        
        /// <summary> Stores an exception that occurs during construction. </summary>
        private Exception constructorException;

        /// <summary> Lock object for constructorException. </summary>
        private readonly object _constructorExceptionLock = new object();

        /// <summary> Loads and returns a NexusComponent from the named assembly. </summary>
        /// <param name="assemblyName"> Name of the assembly to load.
        /// Must match the dll's filename. </param>
        /// <returns> NexusComponent loaded from the provided assembly. </returns>
        public NexusComponent LoadComponent(string assemblyName)
        {
            lock (_loaderLock)
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

                Assembly componentAssembly = AppDomain.CurrentDomain.Load(assemblyName);

                // Iterate through all the types found in the assembly.
                int componentFoundAt = -1;
                Type[] types = componentAssembly.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    // If the type is NexusComponent, we're scanning NexusCommon. Don't need that.
                    if (ReferenceEquals(typeof (NexusComponent), types[i])) break;

                    // This always confuses me.
                    // baseClass .IsAssignableFrom( inheritingClass )
                    if (!typeof (NexusComponent).IsAssignableFrom(types[i])) continue;

                    // Can't have more than one NexusComponent in an assembly.
                    if (componentFoundAt >= 0)
                        throw new InvalidOperationException(
                            "Assembly " + assemblyName + " contains more than one instance of NexusComponent.");

                    // Found a NexusComponent.
                    // Check to make sure its namespace matches its assembly name.
                    if (types[i].Namespace != assemblyName)
                        throw new InvalidOperationException(
                            String.Format("Component {0}'s namespace does not match its assembly name {1}.",
                            types[i].FullName, assemblyName));
                    componentFoundAt = i;
                }

                if (componentFoundAt < 0)
                    throw new EntryPointNotFoundException(
                        "Could not find an instance of NexusComponent in assembly " + assemblyName);

                // Create the instance in another thread.
                var t = new Thread(() => CreateInstance(types[componentFoundAt]))
                            {Name = types[componentFoundAt].FullName + "()"};
                t.Start();

                // Wait a moment for the constructor to complete.
                autoEvent.WaitOne(TimeSpan.FromSeconds(constructorTimeoutSeconds));

                // If the plugin's constructor takes longer than one second, abort.
                // Plugin constructors should not be long-running.
                if (component == null)
                {
                    if (t.IsAlive)
                    {
                        t.Abort();
                        throw new TimeoutException(
                            "Component " + types[componentFoundAt] + " took too long to construct.");
                    }
                    lock (_constructorExceptionLock)
                    {
                        if (constructorException != null)
                            throw new TypeLoadException(
                                "Unhandled Exception occurred in a component's constructor during load.",
                                constructorException);
                    }
                }
                return component;
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            throw new NotImplementedException();
        }

        /// <summary> Attempt to create an instance of the given NexusComponent-inheriting Type. </summary>
        /// <param name="type">System.Type for which to create an instance.
        /// Must be a Type which inherits from NexusComponent.</param>
        private void CreateInstance(Type type)
        {
            // Pass the NexusCore object to the type's constructor.
            try
            {
                component = Activator.CreateInstance(type)
                            as NexusComponent;
            }
            catch (TargetInvocationException ex)
            {
                // Cancel loading, save the exception to be reported.
                component = null;
                lock (_constructorExceptionLock)
                {
                    constructorException = ex.InnerException;
                }
            }

            // Signal that the constructor has finished executing.
            autoEvent.Set();
        }
    }

}
