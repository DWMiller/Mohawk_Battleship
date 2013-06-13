﻿using MBC.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBC.Core
{
    public class ClassicRound : Round
    {
        public ClassicRound(MatchInfo info, params Controller[] controllers)
            : base(info, controllers) { }

        private bool ShotOutOfBounds(Shot shot)
        {
            return (shot.Coordinates > MatchInfo.FieldSize) ||
                    (shot.Coordinates < MatchInfo.FieldSize);
        }

        private bool ShotSuicidal(Shot shot)
        {
            return CurrentTurnID == shot.Receiver;
        }

        private bool ShotRepeated(Shot shot)
        {
            return Controllers[CurrentTurnID].Shots.Contains(shot);
        }

        private bool ShotDestroyed(Shot shot)
        {
            return !RemainingControllers.Contains(Controllers[shot.Receiver]);
        }

        /// <summary>
        /// Ends the Round. Fires ControllerWonEvent and ControllerLostEvent depending on the existance of remaning
        /// Player objects.
        /// </summary>
        public override void End()
        {
            if (RemainingControllers.Count <= 1)
            {
                //There is a winner (or two losers).
                foreach (var controller in Controllers)
                {
                    if (RemainingControllers.Contains(controller))
                    {
                        MakeEvent(new ControllerWonEvent(controller, this));
                    }
                    else
                    {
                        MakeEvent(new ControllerLostEvent(controller, this));
                    }
                }
            }
            base.End();
        }

        /// <summary>
        /// Does the game logic for the current Player turn. Iterates to the next Player in the remaining
        /// list of Player objects. Ends the game when only one Player remains.
        /// </summary>
        protected override void Main()
        {
            if (CurrentTurnID == NextRemaining() || CurrentTurnID == -1)
            {
                //0 or 1 controllers remain.

                End();
                return;
            }

            var sender = Controllers[CurrentTurnID];
            var receiver = Controllers[NextRemaining()];

            var shotsFromSender = sender.Shots;

            try
            {
                Shot shot = sender.MakeShot(receiver.ControllerID);
                MakeEvent(new ControllerShotEvent(sender, this, shot.Coordinates, receiver));
                sender.Shots.Add(shot);

                if (ShotOutOfBounds(shot) || ShotSuicidal(shot) || ShotRepeated(shot) || ShotDestroyed(shot))
                {
                    //The shot violated one of the rules of the round.

                    PlayerLose(sender);
                    NextTurn();
                    return;
                }

                //Get the actual receiver of the shot
                receiver = Controllers[shot.Receiver];

                Ship shotShip = receiver.Ships.ShipAt(shot.Coordinates);
                if (shotShip != null)
                {
                    //The shot made by the sender hit a ship.

                    MakeEvent(new ControllerHitShipEvent(sender, this, shot.Coordinates, receiver));
                    bool sunk = shotShip.IsSunk(sender.Shots.ShotsToReceiver(shot.Receiver));
                    sender.NotifyShotHit(shot, sunk);
                    if (sunk)
                    {
                        //The last shot sunk a receiver's ship.

                        MakeEvent(new ControllerDestroyedShipEvent(sender, this, shotShip, receiver));
                        if (receiver.Ships.GetSunkShips(sender.Shots.ShotsToReceiver(receiver.ControllerID)).Count == receiver.Ships.Count)
                        {
                            //All of the receiving controller's ships have been destroyed.
                            PlayerLose(receiver);
                        }
                    }
                }
                else
                {
                    sender.NotifyShotMiss(shot);
                }

                if (RemainingControllers.Contains(receiver))
                {
                    //The receiver is still in the round.

                    receiver.NotifyOpponentShot(shot);
                }
            }
            catch (ControllerTimeoutException ex)
            {
                //An aforementioned controller timed out.

                MakeEvent(new ControllerTimeoutEvent(this, ex));
                PlayerLose(ex.Controller);
            }

            NextTurn();
        }
    }
}
