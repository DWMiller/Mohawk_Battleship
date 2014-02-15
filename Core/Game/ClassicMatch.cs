﻿using MBC.Core.Threading;
using MBC.Core.Util;
using MBC.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBC.Core.Game
{
    /// <summary>
    /// A type of match that uses the standard rules of battleship as logic.
    /// </summary>
    public class ClassicMatch : MatchCore
    {
        private const string REASON_PLACEMENT = "The player controller placed an invalid ship formation.";
        private const string REASON_SHOT = "The player controller made an invalid shot.";
        private const string REASON_TIMEOUT = "The player controller timed out.";
        private List<Player> activePlayers;
        private int currentIteration;

        /// <summary>
        /// Constructs a match with a specific configuration.
        /// </summary>
        /// <param name="conf"></param>
        public ClassicMatch(Configuration conf)
            : base(conf)
        {
            CurrentPhase = Phase.Init;
        }

        /// <summary>
        /// Constructs a match with the application-wide configuration.
        /// </summary>
        public ClassicMatch()
            : this(Configuration.Global)
        {
        }

        /// <summary>
        /// Defines all of the states that are possible within the match.
        /// </summary>
        public enum Phase
        {
            Init,
            Placement,
            Turn,
            End
        }

        public IEnumerable<Player> ActivePlayers
        {
            get
            {
                return activePlayers;
            }
        }

        /// <summary>
        /// Gets the current phase of this type of match.
        /// </summary>
        public Phase CurrentPhase
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the player that has the turn.
        /// </summary>
        public Player CurrentPlayer
        {
            get;
            protected set;
        }

        public override bool EndRound()
        {
            CurrentPhase = Phase.End;
            return base.EndRound();
        }

        /// <summary>
        /// Makes a player lose the round and removes them from the list of active players.
        /// </summary>
        /// <param name="plr"></param>
        /// <returns></returns>
        public override bool MakePlayerLose(Player plr)
        {
            if (!activePlayers.Remove(plr))
            {
                return false;
            }
            plr.Active = false;
            plr.Controller.RoundLost();
            foreach (Player activePlayer in activePlayers)
            {
                try
                {
                    activePlayer.Controller.OpponentDestroyed(plr.ID);
                }
                catch (ControllerTimeoutException ex)
                {
                    PlayerDisqualified(FindPlayerFromController(ex.Controller), REASON_TIMEOUT);
                }
            }
            return base.MakePlayerLose(plr);
        }

        public override bool NewRound()
        {
            CurrentPhase = Phase.Init;
            return base.NewRound();
        }

        /// <summary>
        /// Switches the current player to the next player in the iteration. Returns true if the
        /// round may continue, false if there is a winner, or no active players left.
        /// </summary>
        /// <returns></returns>
        public bool SwitchNextPlayer()
        {
            if (activePlayers.Count > 1)
            {
                if (CurrentPlayer == activePlayers[currentIteration])
                {
                    currentIteration = ++currentIteration % activePlayers.Count;
                }
                CurrentPlayer = activePlayers[currentIteration];
                return true;
            }
            else if (activePlayers.Count == 1)
            {
                CurrentPhase = Phase.End;
            }
            return false;
        }

        /// <summary>
        /// Adds a player to the match, and ensures that it is inactive until the next round.
        /// </summary>
        /// <param name="plr"></param>
        /// <returns></returns>
        protected override bool AddPlayer(Player plr)
        {
            plr.Active = false;
            return base.AddPlayer(plr);
        }

        /// <summary>
        /// Disqualifies a player from the match and removes it from the active players list.
        /// </summary>
        /// <param name="plr"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected override bool PlayerDisqualified(Player plr, string reason)
        {
            if (!activePlayers.Remove(plr))
            {
                return false;
            }
            plr.Active = false;
            foreach (Player player in activePlayers)
            {
                try
                {
                    player.Controller.OpponentDestroyed(plr.ID);
                }
                catch (ControllerTimeoutException ex)
                {
                    PlayerDisqualified(FindPlayerFromController(ex.Controller), REASON_TIMEOUT);
                }
            }
            return base.PlayerDisqualified(plr, reason);
        }

        /// <summary>
        /// Plays through the current state of the match, in one iteration.
        /// </summary>
        protected override bool PlayLogic()
        {
            try
            {
                switch (CurrentPhase)
                {
                    case Phase.Init:
                        return Initialization();

                    case Phase.Placement:
                        return Placement();

                    case Phase.Turn:
                        return Turn();

                    case Phase.End:
                        return End();
                }
                return false;
            }
            catch (ControllerTimeoutException ex)
            {
                PlayerDisqualified(FindPlayerFromController(ex.Controller), REASON_TIMEOUT);
                return SwitchNextPlayer();
            }
        }

        /// <summary>
        /// Logic that deals with ending the round due to the number of players reaching 1 or 0.
        /// </summary>
        /// <returns></returns>
        private bool End()
        {
            if (activePlayers.Count == 1)
            {
                Player winner = activePlayers[0];
                activePlayers.Clear();
                winner.Controller.RoundWon();
                MakePlayerWin(winner);
            }
            return false;
        }

        /// <summary>
        /// Finds the player with a specific controller interface.
        /// </summary>
        /// <param name="playerController"></param>
        /// <returns></returns>
        private Player FindPlayerFromController(IController playerController)
        {
            foreach (var player in Players)
            {
                if (player.Controller == playerController)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// The initialization phase.
        /// </summary>
        /// <returns></returns>
        private bool Initialization()
        {
            if (Players.Count() < 2)
            {
                return false;
            }
            currentIteration = 0;
            activePlayers = new List<Player>();
            foreach (var plr in Players)
            {
                try
                {
                    plr.Active = true;
                    plr.Ships = StartingShips;
                    activePlayers.Add(plr);
                    plr.Controller.NewRound();
                }
                catch (ControllerTimeoutException ex)
                {
                    PlayerDisqualified(FindPlayerFromController(ex.Controller), REASON_TIMEOUT);
                }
            }
            CollectionUtils.RandomizeList(activePlayers);
            CurrentPhase = Phase.Placement;
            return SwitchNextPlayer();
        }

        /// <summary>
        /// The ship placement phase for one iteration of a player.
        /// </summary>
        /// <returns></returns>
        private bool Placement()
        {
            if (ShipList.AreShipsPlaced(CurrentPlayer.Ships))
            {
                CurrentPhase = Phase.Turn;
                return true;
            }
            else
            {
                CurrentPlayer.Ships = new HashSet<Ship>(CurrentPlayer.Controller.PlaceShips().ToList());

                if (!AreShipsValid(CurrentPlayer))
                {
                    PlayerDisqualified(CurrentPlayer, REASON_PLACEMENT);
                }
            }
            if (!AreShipsValid(CurrentPlayer))
            {
                PlayerDisqualified(CurrentPlayer, REASON_PLACEMENT);
            }
            if (activePlayers.Count > 0 && ShipList.AreShipsPlaced(activePlayers[currentIteration + 1].Ships))
            {
                CurrentPhase = Phase.Turn;
            }
            return SwitchNextPlayer();
        }

        /// <summary>
        /// The player shot phase for one iteration of a player.
        /// </summary>
        /// <returns></returns>
        private bool Turn()
        {
            var shotMade = CurrentPlayer.Controller.MakeShot();
            var shipHit = ShipList.GetShipAt(shotMade.ReceiverPlr.Ships, shotMade.Coordinates);
            MakePlayerShot(CurrentPlayer, shotMade, shipHit);
            if (!IsShotValid(shotMade))
            {
                PlayerDisqualified(CurrentPlayer, REASON_SHOT);
                return SwitchNextPlayer();
            }
            shotMade.ReceiverPlr.Controller.OpponentShot(shotMade);

            shipHit.SetShotHit(shotMade.Coordinates, true);

            if (shipHit != null)
            {
                var sunk = shipHit.IsSunk();
                CurrentPlayer.Controller.ShotHit(shotMade, sunk);
                if (sunk)
                {
                    MakePlayerShipDestroyed(shotMade.ReceiverPlr, shipHit);
                    if (shotMade.ReceiverPlr.Ships.All(ship => ship.IsSunk()))
                    {
                        MakePlayerLose(shotMade.ReceiverPlr);
                    }
                }
            }
            else
            {
                CurrentPlayer.Controller.ShotMiss(shotMade);
            }
            return SwitchNextPlayer();
        }
    }
}