﻿// ****************************************************************************
// <copyright file="MessageRegistrationInfo.cs" company="ALICERAIN">
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

namespace Nexus
{
    public struct MessageRegistrationInfo
    {
        public WeakAction WeakAction { get; private set; }
        public object Token { get; private set; }
        public bool IncludeDerived { get; private set; }

        public MessageRegistrationInfo(WeakAction weakAction, object token, bool includeDerived) : this()
        {
            WeakAction = weakAction;
            Token = token;
            IncludeDerived = includeDerived;
        }
    }
}
