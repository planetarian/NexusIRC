﻿// ****************************************************************************
// <copyright file="MethodDescriptor.cs" company="ALICERAIN">
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
using System.Reflection;
using System.Text;

namespace Nexus
{
    [Serializable]
    public struct MethodDescriptor
    {
        #region Properties

        private readonly Type _returnType;
        private readonly MethodSignature _signature;

        /// <summary> System.Type representing the method's return type. </summary>
        public Type ReturnType
        {
            get { return _returnType; }
        }

        /// <summary> Nexus.MethodSignature representing the method's signature. </summary>
        public MethodSignature Signature
        {
            get { return _signature; }
        }

        #endregion

        /// <summary> Initializes a new instance of the Nexus.MethodDescriptor struct
        /// using the specified method name, parameter types, and return Type.</summary>
        /// <param name="methodName"> Method name in Type.Method format. </param>
        /// <param name="parameterTypes"> Array of Type objects representing the method's parameter types. </param>
        /// <param name="returnType"> Type object representing the method's return type. </param>
        public MethodDescriptor(string methodName, Type[] parameterTypes = null, Type returnType = null)
        {
            if (String.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName");

            _signature = new MethodSignature(methodName, parameterTypes);
            _returnType = returnType ?? typeof(void);
        }

        /// <summary> Initializes a new instance of the Nexus.MethodDescriptor struct
        /// using the specified method name, parameter types, and return Type.</summary>
        /// <param name="methodName"> Method name in Type.Method format. </param>
        /// <param name="parameters"> IList of parameter objects to retrieve types from. </param>
        /// <param name="returnType"> Type object representing the method's return type. </param>
        public MethodDescriptor(string methodName, IList<object> parameters, Type returnType = null)
        {
            if (String.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName");

            _signature = new MethodSignature(methodName, parameters);
            _returnType = returnType ?? typeof (void);
        }

        /// <summary> Initializes a new instance of the Nexus.MethodDescriptor struct
        /// using the specified Nexus.MethodSignature and return Type. </summary>
        /// <param name="signature"> Nexus.MethodSignature representing the method's signature. </param>
        /// <param name="returnType"> System.Type representing the type returned by the method. </param>
        public MethodDescriptor(MethodSignature signature, Type returnType = null)
        {
            _signature = signature;
            _returnType = returnType ?? typeof(void);
        }

        /// <summary> Initializes a new instance of the Nexus.MethodDescriptor struct
        /// using the provided System.Reflection.MethodInfo object. </summary>
        /// <param name="methodInfo">
        /// System.Reflection.MethodInfo object to convert to a MethodDescriptor.
        /// </param>
        /// <returns> Nexus.MethodDescriptor converted from the given MethodInfo. </returns>
        public MethodDescriptor(MethodInfo methodInfo)
        {
            _signature = new MethodSignature(methodInfo);
            _returnType = methodInfo.ReturnType;
        }

        /// <summary> Generates an Array of MethodDescriptor objects from a given type. </summary>
        /// <param name="type"> Type to fetch method information from. </param>
        /// <param name="inherited"> Whether to include methods inherited from base types. </param>
        /// <returns> A System.Array of Nexus.MethodDescriptor objects
        /// representing the methods in the given object. </returns>
        public static MethodDescriptor[] ArrayFromType(Type type, bool inherited = false)
        {
            MethodInfo[] methods = type.GetPublicInstanceMethods(false);

            // NexusCore can't handle MethodInfo from assemblies it hasn't loaded.
            // Prepare MethodDescriptors instead.
            var descriptors = new MethodDescriptor[methods.Length];

            for (int i = 0; i < methods.Length; i++)
                descriptors[i] = new MethodDescriptor(methods[i]);

            return descriptors;
        }
        
        /// <summary> Generates a Dictionary representing the methods in the given Type. </summary>
        /// <param name="type"> Type to retrieve method information for. </param>
        /// <param name="inherited"> Boolean specifying whether to include inherited methods. </param>
        /// <returns> Dictionary in which keys are MethodDescriptor and values are MethodInfo. </returns>
        public static Dictionary<MethodDescriptor, MethodInfo> DictionaryFromType(Type type, bool inherited = false)
        {
            MethodInfo[] methods = type.GetPublicInstanceMethods(false);
            var dic = new Dictionary<MethodDescriptor, MethodInfo>();

            foreach (MethodInfo t in methods)
                dic[new MethodDescriptor(t)] = t;

            return dic;
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
            if (prefix) sb.Append("MethodDescriptor: ");
            sb.Append(ReturnType.Name);
            sb.Append(" ");
            sb.Append(Signature.MethodName);
            sb.Append("(");
            for (int i = 0; i < Signature.ParameterTypes.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(Signature.ParameterTypes[i].Name);
            }
            sb.Append(")");

            return sb.ToString();
        }
        
        public override bool Equals(object obj)
        {

            if (obj is MethodDescriptor)
                return Equals((MethodDescriptor) obj);
            return false;
        }

        public bool Equals(MethodDescriptor md, bool compareReturnType = true)
        {
            if (compareReturnType && !ReferenceEquals(ReturnType, md.ReturnType)) return false;
            return Signature == md.Signature;
        }

        public static bool operator ==(MethodDescriptor a, MethodDescriptor b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(MethodDescriptor a, MethodDescriptor b)
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
