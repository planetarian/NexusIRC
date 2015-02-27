﻿// ****************************************************************************
// <copyright file="MethodSignature.cs" company="ALICERAIN">
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
using System.Reflection;
using System.Text;

namespace Nexus
{
    [Serializable]
    public struct MethodSignature
    {
        #region Properties

        private readonly string _methodName;
        private readonly Type[] _parameterTypes;

        /// <summary> System.String representing the name of the method. </summary>
        public string MethodName
        {
            get { return _methodName; }
        }

        /// <summary> System.Type array representing the method parameter types. </summary>
        public Type[] ParameterTypes
        {
            get { return _parameterTypes; }
        }

        #endregion

        /// <summary> Initializes a new instance of the Nexus.MethodSignature struct
        /// using the specified method name and parameter types. </summary>
        /// <param name="methodName"> System.String representing the method's name. </param>
        /// <param name="parameterTypes"> System.Type array representing the method parameter types. </param>
        public MethodSignature(string methodName, Type[] parameterTypes = null)
        {
            if (String.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName");

            _methodName = methodName;
            _parameterTypes = parameterTypes ?? new Type[0];
        }

        /// <summary> Initializes a new instance of the Nexus.MethodSignature struct
        /// using the specified method name and parameter types. </summary>
        /// <param name="methodName"> System.String representing the method's name. </param>
        /// <param name="parameters"> IList containing the method parameters to fetch types from. </param>
        public MethodSignature(string methodName, IList<object> parameters)
        {
            if (String.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName");

            Type[] paramTypes = null;

            if (parameters != null)
            {
                paramTypes = new Type[parameters.Count];

                for (int i = 0; i < paramTypes.Length; i++)
                    paramTypes[i] = parameters[i].GetType();
            }

            _methodName = methodName;
            _parameterTypes = paramTypes ?? new Type[0];
        }
        
        /// <summary> Initializes a new instance of the Nexus.MethodSignature struct
        /// using the provided System.Reflection.MethodBase object. </summary>
        /// <param name="methodBase"> System.Reflection.MethodBase to fetch a MethodSignature from. </param>
        public MethodSignature(MethodBase methodBase)
        {
            if (ReferenceEquals(methodBase.DeclaringType, null))
                throw new InvalidOperationException("This method has no declaring type?");

            _methodName = String.Format("{0}.{1}", methodBase.DeclaringType.Name, methodBase.Name);
            ParameterInfo[] parameters = methodBase.GetParameters();
            _parameterTypes = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                _parameterTypes[i] = parameters[i].ParameterType;
        }

        #region Overrides

        /// <summary> Converts the value of this instance to a System.String. </summary>
        /// <returns>
        /// Nexus.MethodDescriptor: ReturnType MethodName(ParameterType0, ParameterType1, ... ParameterTypeN);
        /// </returns>
        public override string ToString()
        {
            return ToString(true);
        }

        public string ToString(bool prefix)
        {
            var sb = new StringBuilder();
            //sb.Append(base.ToString());
            if (prefix) sb.Append("MethodSignature: ");
            sb.Append(MethodName);
            sb.Append("(");
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(ParameterTypes[i].Name);
            }
            sb.Append(")");

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodSignature)
                return Equals((MethodSignature) obj);
            return false;
        }

        public bool Equals(MethodSignature ms)
        {
            if (MethodName != ms.MethodName) return false;
            if (ParameterTypes.Length != ms.ParameterTypes.Length) return false;
            for (int i = 0; i < ParameterTypes.Length; i++)
                if (!ReferenceEquals(ParameterTypes[i], ms.ParameterTypes[i])) return false;
            return true;
        }

        public static bool operator ==(MethodSignature a, MethodSignature b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(MethodSignature a, MethodSignature b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return (ToString() + " HashCode").GetHashCode();
        }

        #endregion
    }
}
