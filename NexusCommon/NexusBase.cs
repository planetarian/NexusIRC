﻿// ****************************************************************************
// <copyright file="NexusBase.cs" company="ALICERAIN">
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

namespace Nexus
{
    public abstract class NexusBase : MarshalByRefObject
    {
        public abstract void UnloadComponent(string componentName);

        /// <summary> Calls a method exposed by a NexusComponent with a return value. </summary>
        /// <typeparam name="T"> Return value type. </typeparam>
        /// <param name="source"> NexusComponent that called the method. Currently has no effect. </param>
        /// <param name="methodName"> Name of method to call. </param>
        /// <param name="returnValue"> Will be set to the method's return value. </param>
        /// <param name="parameters"> Array of objects containing method parameters. </param>
        /// <returns> null if the method has been registered and executed successfully.
        /// MethodNotFoundException if the method has not been registered.
        /// An Exception object if the method throws an unhandled exception. </returns>
        public abstract Exception CallMethod<T>(NexusComponent source, string methodName, out T returnValue, object[] parameters);

        /// <summary> Calls a method exposed by a NexusComponent without a return value. </summary>
        /// <param name="source"> NexusComponent that called the method. Currently has no effect. </param>
        /// <param name="methodName"> Name of method to call. </param>
        /// <param name="parameters"> Array of objects containing method parameters. </param>
        /// <returns> null if the method has been registered and executed successfully.
        /// MethodNotFoundException if the method has not been registered.
        /// An Exception object if the method throws an unhandled exception. </returns>
        public abstract Exception CallMethod(NexusComponent source, string methodName, object[] parameters);

        public abstract void SendMessage<TMessage>(TMessage message, object token = null);

        public abstract string Marco();

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
