﻿/*
 Copyright 2015 - 2015 SPrediction
 VectorPrediction.cs is part of SPrediction
 
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
    using System.Collections.Generic;
    using System.Linq;

    using SharpDX;

    using EnsoulSharp;
    using EnsoulSharp.SDK;

    /// <summary>
    /// Vector prediction class
    /// </summary>
    public static class VectorPrediction
    {
        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="input">Neccesary inputs for prediction calculations</param>
        /// <param name="vectorLenght">Vector Lenght</param>
        /// <returns>Prediction result as <see cref="VectorResult"/></returns>
        public static VectorResult GetPrediction(PredictionInput input, float vectorLenght)
        {
            return GetPrediction(input.Target, input.SpellWidth, input.SpellDelay, input.SpellMissileSpeed, input.SpellRange, vectorLenght, input.Path, input.AvgReactionTime, input.LastMovChangeTime, input.AvgPathLenght, input.RangeCheckFrom.ToVector2());
        }

        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Vector width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="vectorSpeed">Vector speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="vectorLenght">Vector lenght</param>
        /// <returns>Prediction result as <see cref="VectorResult"/></returns>
        public static VectorResult GetPrediction(AIHeroClient target, float width, float delay, float vectorSpeed, float range, float vectorLenght)
        {
            return GetPrediction(target, width, delay, vectorSpeed, range, vectorSpeed, target.GetWaypoints(), target.AvgMovChangeTime(), target.LastMovChangeTime(), target.AvgPathLenght(), ObjectManager.Player.PreviousPosition.ToVector2());
        }

        /// <summary>
        /// Gets prediction result
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="width">Vector width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="vectorSpeed">Vector speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="vectorLenght">Vector lenght</param>
        /// <param name="path">Waypoints of target</param>
        /// <param name="avgt">Average reaction time (in ms)</param>
        /// <param name="movt">Passed time from last movement change (in ms)</param>
        /// <param name="avgp">Average Path Lenght</param>
        /// <param name="rangeCheckFrom"></param>
        /// <returns>Prediction result as <see cref="VectorResult"/></returns>
        public static VectorResult GetPrediction(AIBaseClient target, float width, float delay, float vectorSpeed, float range, float vectorLenght, List<Vector2> path, float avgt, float movt, float avgp, Vector2 rangeCheckFrom)
        {
            var result = new VectorResult();

            //auto aoe hit (2 hits with using one target as from position)
            if (target.Type == GameObjectType.AIHeroClient && target.IsValid) //do these calcs if champion kappa
            {
                if (ObjectManager.Player.CountEnemyHeroesInRange(range) > 0 && ObjectManager.Player.CountEnemyHeroesInRange(range + vectorLenght) > 1) //if there is at least 1 enemy in range && at least 2 enemy which laser can hit
                {
                    var predPos1 = PredictionExtensions.GetFastUnitPosition(target, delay); //get target unit position after delay
                    foreach (var enemy in GameObjects.EnemyHeroes) //loop all enemies
                    {
                        if (enemy.NetworkId != target.NetworkId && enemy.Distance(rangeCheckFrom) < range + vectorLenght) //if enemy is not given target and enemy is hitable by laser
                        {
                            var predPos2 = PredictionExtensions.GetFastUnitPosition(enemy, delay); //get enemy unit position after delay
                            if (predPos1.Distance(rangeCheckFrom) < range) //if target is in range 
                            {
                                var predRes = LinePrediction.GetLinePrediction(enemy, width, delay, vectorSpeed, vectorLenght, false, enemy.GetWaypoints(), enemy.AvgMovChangeTime(), enemy.LastMovChangeTime(), enemy.AvgPathLenght(), 360, predPos1 - (predPos1 - rangeCheckFrom).Normalized().Perpendicular() * 30, predPos1 - (predPos1 - rangeCheckFrom).Normalized().Perpendicular() * 30); //get enemy prediciton with from = target's position (a bit backward)
                                if (predRes.HitChance >= HitChance.Low)
                                {
                                    var f = predPos1 - (predPos1 - rangeCheckFrom).Normalized().Perpendicular() * 30;
                                    return new VectorResult
                                    {
                                        CastSourcePosition = f,
                                        CastTargetPosition = predRes.CastPosition,
                                        UnitPosition = predRes.UnitPosition,
                                        HitChance = predRes.HitChance,
                                        CollisionResult = predRes.CollisionResult,
                                    };
                                }
                            }
                            else if (predPos2.Distance(rangeCheckFrom) < range) //if enemy is in range
                            {
                                var predRes = LinePrediction.GetLinePrediction(target, width, delay, vectorSpeed, vectorLenght, false, path, avgt, movt, avgp, 360, predPos2 - (predPos2 - rangeCheckFrom).Normalized().Perpendicular() * 30, predPos2 - (predPos2 - rangeCheckFrom).Normalized().Perpendicular() * 30); //get target prediction with from = enemy's position (a bit backward)
                                if (predRes.HitChance >= HitChance.Low)
                                {
                                    var f = predPos2 - (predPos2 - rangeCheckFrom).Normalized().Perpendicular() * 30;
                                    return new VectorResult
                                    {
                                        CastSourcePosition = f,
                                        CastTargetPosition = predRes.CastPosition,
                                        UnitPosition = predRes.UnitPosition,
                                        HitChance = predRes.HitChance,
                                        CollisionResult = predRes.CollisionResult,
                                    };
                                }
                            }
                        }
                    }
                }
            }

            var immobileFrom = rangeCheckFrom + (target.PreviousPosition.ToVector2() - rangeCheckFrom).Normalized() * range;

            if (path.Count <= 1) //if target is not moving, easy to hit
            {
                result.HitChance = HitChance.VeryHigh;
                result.CastSourcePosition = immobileFrom;
                result.CastTargetPosition = target.PreviousPosition.ToVector2();
                result.UnitPosition = result.CastTargetPosition;
                result.CollisionResult = Collision.GetCollisions(immobileFrom, result.CastTargetPosition, range, width, delay, vectorSpeed);

                if (immobileFrom.Distance(result.CastTargetPosition) > vectorLenght - PredictionExtensions.GetArrivalTime(immobileFrom.Distance(result.CastTargetPosition), delay, vectorSpeed) * target.MoveSpeed)
                {
                    result.HitChance = HitChance.OutOfRange;
                }

                return result;
            }

            if (target is AIHeroClient hero)
            {
                if (hero.IsCastingImporantSpell())
                {
                    result.HitChance = HitChance.VeryHigh;
                    result.CastSourcePosition = immobileFrom;
                    result.CastTargetPosition = hero.PreviousPosition.ToVector2();
                    result.UnitPosition = result.CastTargetPosition;
                    result.CollisionResult = Collision.GetCollisions(immobileFrom, result.CastTargetPosition, range, width, delay, vectorSpeed);

                    //check if target can dodge with moving backward
                    if (immobileFrom.Distance(result.CastTargetPosition) > range - PredictionExtensions.GetArrivalTime(immobileFrom.Distance(result.CastTargetPosition), delay, vectorSpeed) * hero.MoveSpeed)
                    {
                        result.HitChance = HitChance.OutOfRange;
                    }

                    return result;
                }

                //to do: find a fuking logic
                if (avgp < 400 && movt < 100)
                {
                    result.HitChance = HitChance.High;
                    result.CastTargetPosition = hero.PreviousPosition.ToVector2();
                    result.CastSourcePosition = immobileFrom;
                    result.UnitPosition = result.CastTargetPosition;
                    result.CollisionResult = Collision.GetCollisions(immobileFrom, result.CastTargetPosition, range, width, delay, vectorSpeed);

                    //check if target can dodge with moving backward
                    if (immobileFrom.Distance(result.CastTargetPosition) > range - PredictionExtensions.GetArrivalTime(immobileFrom.Distance(result.CastTargetPosition), delay, vectorSpeed) * hero.MoveSpeed)
                    {
                        result.HitChance = HitChance.OutOfRange;
                    }

                    return result;
                }
            }

            if (target.IsDashing())
            {
                var dashPred = PredictionExtensions.GetDashingPrediction(target, width, delay, vectorSpeed, range, false, SpellType.Line, immobileFrom, rangeCheckFrom);
                return new VectorResult
                {
                    CastSourcePosition = immobileFrom,
                    CastTargetPosition = dashPred.CastPosition,
                    UnitPosition = dashPred.UnitPosition,
                    HitChance = dashPred.HitChance,
                    CollisionResult = dashPred.CollisionResult,
                };
            }

            if (target.IsImmobileTarget())
            {
                var immoPred = PredictionExtensions.GetImmobilePrediction(target, width, delay, vectorSpeed, range, false, SpellType.Line, immobileFrom, rangeCheckFrom);
                return new VectorResult
                {
                    CastSourcePosition = immobileFrom,
                    CastTargetPosition = immoPred.CastPosition,
                    UnitPosition = immoPred.UnitPosition,
                    HitChance = immoPred.HitChance,
                    CollisionResult = immoPred.CollisionResult,
                };
            }

            for (var i = 0; i < path.Count - 1; i++)
            {
                var point = Geometry.ClosestCirclePoint(rangeCheckFrom, range, path[i]);
                if (path[i].Distance(ObjectManager.Player.PreviousPosition) < range)
                {
                    point = path[i];
                }

                var res = PredictionExtensions.WaypointAnlysis(target, width, delay, vectorSpeed, vectorLenght, false, SpellType.Line, path, avgt, movt, avgp, 360, point);
                res.Input = new PredictionInput(target, delay, vectorSpeed, width, range, false, SpellType.Line, rangeCheckFrom.ToVector3World(), rangeCheckFrom.ToVector3World());
                res.Lock();
                if (res.HitChance >= HitChance.Low)
                {
                    return new VectorResult
                    {
                        CastSourcePosition = point,
                        CastTargetPosition = res.CastPosition,
                        UnitPosition = res.UnitPosition,
                        HitChance = res.HitChance,
                        CollisionResult = res.CollisionResult,
                    };
                }
            }

            result.CastSourcePosition = immobileFrom;
            result.CastTargetPosition = target.PreviousPosition.ToVector2();
            result.HitChance = HitChance.None;
            return result;
        }

        /// <summary>
        /// Gets Aoe Prediction result
        /// </summary>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="vectorSpeed">Vector speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="vectorLenght">Vector lenght</param>
        /// <param name="rangeCheckFrom"></param>
        /// <returns>Prediction result as <see cref="SpellAoeResult"/></returns>
        public static VectorAoeResult GetAoePrediction(float width, float delay, float vectorSpeed, float range, float vectorLenght, Vector2 rangeCheckFrom)
        {
            var result = new VectorAoeResult();
            var enemies = GameObjects.EnemyHeroes.Where(p => p.IsValidTarget() && PredictionExtensions.GetFastUnitPosition(p, delay, 0, rangeCheckFrom).Distance(rangeCheckFrom) < range);

            foreach (var enemy in enemies)
            {
                var path = enemy.GetWaypoints();
                if (path.Count <= 1)
                {
                    var from = rangeCheckFrom + (enemy.PreviousPosition.ToVector2() - rangeCheckFrom).Normalized() * range;
                    var to = from + (enemy.PreviousPosition.ToVector2() - from).Normalized() * vectorLenght;
                    var colResult = Collision.GetCollisions(from, to, range, width, delay, vectorSpeed);

                    if (colResult.Objects.HasFlag(CollisionFlags.EnemyChampions))
                    {
                        var collisionCount = colResult.Units.Count(p => p.IsEnemy && p.Type == GameObjectType.AIHeroClient && p.IsValid);
                        if (collisionCount > result.HitCount)
                        {
                            result = new VectorAoeResult
                            {
                                CastSourcePosition = from,
                                CastTargetPosition = enemy.PreviousPosition.ToVector2(),
                                HitCount = collisionCount,
                                CollisionResult = colResult
                            };
                        }
                    }
                }
                else
                {
                    if (!enemy.IsDashing())
                    {
                        for (var i = 0; i < path.Count - 1; i++)
                        {
                            var point = Geometry.ClosestCirclePoint(rangeCheckFrom, range, path[i]);
                            var prediction = PredictionExtensions.GetPrediction(enemy, width, delay, vectorSpeed, vectorLenght, false, SpellType.Line, path, enemy.AvgMovChangeTime(), enemy.LastMovChangeTime(), enemy.AvgPathLenght(), enemy.LastAngleDiff(), point, rangeCheckFrom);
                            if (prediction.HitChance > HitChance.Medium)
                            {
                                var to = point + (prediction.CastPosition - point).Normalized() * vectorLenght;
                                var colResult = Collision.GetCollisions(point, to, range, width, delay, vectorSpeed);
                                if (colResult.Objects.HasFlag(CollisionFlags.EnemyChampions))
                                {
                                    var collisionCount = colResult.Units.Count(p => p.IsEnemy && p.Type == GameObjectType.AIHeroClient && p.IsValid);
                                    if (collisionCount > result.HitCount)
                                    {
                                        result = new VectorAoeResult
                                        {
                                            CastSourcePosition = point,
                                            CastTargetPosition = prediction.CastPosition,
                                            HitCount = collisionCount,
                                            CollisionResult = colResult
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
