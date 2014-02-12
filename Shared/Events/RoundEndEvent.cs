﻿using MBC.Shared;
using System.Runtime.Serialization;

namespace MBC.Core.Events
{
    /// <summary>
    /// Provides information about a <see cref="GameLogic"/> that has ended.
    /// </summary>
    public class RoundEndEvent : Event
    {
        /// <summary>
        /// Passes the <paramref name="round"/> to the base constructor
        /// </summary>
        /// <param name="round"></param>
        public RoundEndEvent(IDNumber roundNumber)
        {
            RoundNumber = roundNumber;
        }

        /// <summary>
        /// Gets the round number of this event.
        /// </summary>
        public IDNumber RoundNumber
        {
            get;
            private set;
        }
    }
}