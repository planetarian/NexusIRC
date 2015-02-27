﻿// ****************************************************************************
// <copyright file="SomeOtherComponent.cs" company="ALICERAIN">
// Copyright © ALICERAIN 2012
// </copyright>
// ****************************************************************************
// <author>Ash Wolford</author>
// <email>piro@live.com</email>
// <date>2012-03-26</date>
// <project>NexusIRC.NexusComponents2</project>
// <web>http://pirocast.net/</web>
// <license>
// All rights reserved, until I decide on an appropriate license.
// </license>
// ****************************************************************************

using System;
using System.Threading;
using Nexus;
using Nexus.Messages;

namespace NexusComponents2
{
    public class SomeOtherComponent : NexusComponent
    {
        public SomeOtherComponent()
        {

            //RegisterListener<IRCInviteEvent>(@event => { throw new InvalidOperationException("you didn't say the magic word!"); });
        }

        public override void Startup()
        {
        }
    }
}
