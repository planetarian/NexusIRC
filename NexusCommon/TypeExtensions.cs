﻿// ****************************************************************************
// <copyright file="TypeExtensions.cs" company="ALICERAIN">
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
using System.Reflection;

namespace Nexus
{
    public static class TypeExtensions
    {
        // It's probably overkill to make this into an extension method.
        // But what the hell, reduction of repetition ftw?
        public static MethodInfo[] GetPublicInstanceMethods(this Type type, bool inherited = true)
        {
            if (ReferenceEquals(type, null))
                throw new ArgumentNullException("type");

            // Get public methods in the specified type.
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (!inherited) flags |= BindingFlags.DeclaredOnly;

            return type.GetMethods(flags);
        }
    }
}
