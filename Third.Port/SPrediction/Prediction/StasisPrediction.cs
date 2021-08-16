﻿/*
 Copyright 2015 - 2015 SPrediction
 StasisPrediction.cs is part of SPrediction
 
 SPrediction is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 SPrediction is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with SPrediction. If not, see <http://www.gnu.org/licenses/>.
*/

namespace SPrediction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using EnsoulSharp;
    using EnsoulSharp.SDK;

    /// <summary>
    /// StasisPrediction class
    /// </summary>
    public static class StasisPrediction
    {
        private static List<Tuple<string, int>> s_StasisBuffs;
        private static List<Spell> s_RegisteredSpells;
        private static List<Stasis> s_DetectedStasises;

        public static EventHandler<StasisResult> OnGuaranteedHit;

        /// <summary>
        /// Initializes stasis prediction
        /// </summary>
        public static void Initialize()
        {
            s_StasisBuffs = new List<Tuple<string, int>>
            {
                new Tuple<string, int>("bardrstasis", 2500),
                new Tuple<string, int>("LissandraRSelf", 2500),
                new Tuple<string, int>("LissandraREnemy2", 1500),
                new Tuple<string, int>("ChronoRevive", 3000),
                new Tuple<string, int>("zhonyasringshield", 2500)
            };

            s_RegisteredSpells = new List<Spell>();
            s_DetectedStasises = new List<Stasis>();

            Game.OnUpdate += Game_OnUpdate;
            AIBaseClient.OnBuffAdd += AIBaseClient_OnBuffGain;
        }

        /// <summary>
        /// OnUpdate event
        /// </summary>
        /// <param name="args">The args.</param>
        private static void Game_OnUpdate(EventArgs args)
        {
            s_DetectedStasises.RemoveAll(p => Variables.TickCount - p.StartTick > p.Duration + 500);
            foreach (var stasis in s_DetectedStasises)
            {
                if (!stasis.Processed)
                {
                    foreach (var spell in s_RegisteredSpells)
                    {
                        stasis.Process(spell);
                    }
                }
            }
        }

        /// <summary>
        /// Registers the spell for stasis calculations
        /// </summary>
        /// <param name="s">The spell.</param>
        public static void RegisterSpell(Spell s)
        {
            if (!s_RegisteredSpells.Contains(s))
            {
                s_RegisteredSpells.Add(s);
            }
        }

        /// <summary>
        /// Unregisters spell
        /// </summary>
        /// <param name="s"></param>
        public static void UnregisterSpell(Spell s)
        {
            if (s_RegisteredSpells.Contains(s))
            {
                s_RegisteredSpells.Remove(s);
            }
        }

        /// <summary>
        /// OnBuffAdd Event
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private static void AIBaseClient_OnBuffGain(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
        {
            if (sender.Type == GameObjectType.AIHeroClient && sender.IsValid && sender.IsEnemy)
            {
                var stasis = s_StasisBuffs.FirstOrDefault(p => args.Buff.Name.Contains(p.Item1));
                if (stasis != null)
                {
                    s_DetectedStasises.Add(new Stasis { Unit = sender, StartTick = Variables.TickCount, Duration = stasis.Item2, Name = stasis.Item1, Processed = false });
                }
            }
        }

        /// <summary>
        /// Stasis class
        /// </summary>
        internal class Stasis
        {
            /// <summary>
            /// The Unit
            /// </summary>
            internal AIBaseClient Unit;

            /// <summary>
            /// The start tick of stasis
            /// </summary>
            internal int StartTick;

            /// <summary>
            /// The duration of stasis
            /// </summary>
            internal int Duration;

            /// <summary>
            /// The name of stasis
            /// </summary>
            internal string Name;

            /// <summary>
            /// 
            /// </summary>
            internal bool Processed;

            /// <summary>
            /// Stasis calculations
            /// </summary>
            /// <param name="spell">The spell.</param>
            internal void Process(Spell spell)
            {
                var arrivalT = PredictionExtensions.GetArrivalTime(spell.From.Distance(this.Unit.PreviousPosition), spell.Delay, spell.Speed) * 1000f;
                if (Variables.TickCount - this.StartTick >= this.Duration - arrivalT)
                {
                    var pred = new PredictionResult
                    {
                        Input = new PredictionInput(this.Unit, spell.Delay, spell.Speed, spell.Width, spell.Range, spell.Collision, spell.Type, spell.From, spell.RangeCheckFrom),
                        Unit = this.Unit,
                        CastPosition = this.Unit.PreviousPosition.ToVector2(),
                        UnitPosition = this.Unit.PreviousPosition.ToVector2(),
                        HitChance = HitChance.VeryHigh
                    };
                    pred.Lock(false);

                    var result = new StasisResult {Spell = spell, Prediction = pred};

                    if (OnGuaranteedHit != null && pred.HitChance != HitChance.Collision &&
                        pred.HitChance != HitChance.OutOfRange)
                    {
                        OnGuaranteedHit(MethodBase.GetCurrentMethod().DeclaringType, result);
                    }

                    this.Processed = true;
                }
            }
        }
    }
}
