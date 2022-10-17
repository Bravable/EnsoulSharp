using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using SharpDX;
namespace QSharp.Common.Evade.SpellsEvade
{
    public static class SkillshotDetector
    {
        #region Constructors and Destructors
        static SkillshotDetector()
        {
            AIBaseClient.OnDoCast +=
                 OnProcessSpellCast;

            GameObject.OnCreate += (sender, args) => { DelayAction.Add(0, () => MissionOnCreate(sender)); };
            GameObject.OnDelete += MissileOnDelete;
            GameObject.OnDelete += (sender, args) =>
            {

                    //DEBUGPORT
                    if (sender == null || !sender.IsValid || (!EvadeInit.IsDebugMode && sender.Team == GameObjects.Player.Team))
                    {
                        return;
                    }
                    for (var i = Evade.DetectedSkillshots.Count - 1; i >= 0; i--)
                    {
                        var skillshot = Evade.DetectedSkillshots[i];
                    if (skillshot.SpellData.ToggleParticleName != ""
                            && new Regex(skillshot.SpellData.ToggleParticleName).IsMatch(sender.Name))
                        {
                            Evade.DetectedSkillshots.RemoveAt(i);
                        }
                    }

            };
        }

        #endregion

        #region Delegates

        public delegate void OnDeleteSkillshotH(Skillshot skillshot, MissileClient missile);

        public delegate void OnDetectSkillshotH(Skillshot skillshot);

        #endregion

        #region Public Events

        public static event OnDeleteSkillshotH OnDeleteSkillshot;

        public static event OnDetectSkillshotH OnDetectSkillshot;

        #endregion

        #region Methods

        private static void MissileOnDelete(GameObject sender, EventArgs args)
        {

            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValid)
            {
                return;
            }
            var unit = missile.SpellCaster as AIHeroClient;
            //DEBUGPORT
            if (unit == null || !unit.IsValid || (!EvadeInit.IsDebugMode && unit.Team == GameObjects.Player.Team))
            {
                return;
            }

            var spellName = missile.SData.Name;
            if (OnDeleteSkillshot != null)
            {
                foreach (var skillshot in
                    Evade.DetectedSkillshots.Where(
                        i =>
                        i.SpellData.MissileSpellName == spellName && i.Unit.NetworkId == unit.NetworkId
                        && (missile.EndPosition.ToVector2() - missile.StartPosition.ToVector2()).AngleBetween(
                            i.Direction) < 10 && i.SpellData.CanBeRemoved))
                {

                    OnDeleteSkillshot(skillshot, missile);
                    break;
                }
            }
            Evade.DetectedSkillshots.RemoveAll(
                i =>
                (i.SpellData.MissileSpellName == spellName || i.SpellData.ExtraMissileNames.Contains(spellName))
                && (i.Unit.NetworkId == unit.NetworkId
                    && (missile.EndPosition.ToVector2() - missile.StartPosition.ToVector2()).AngleBetween(i.Direction)
                    < 10 && i.SpellData.CanBeRemoved || i.SpellData.ForceRemove));
        }

        private static void MissionOnCreate(GameObject sender)
        {
            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValid)
            {
                return;
            }
            var unit = missile.SpellCaster as AIHeroClient;
            //DEBUGPORT
            if (unit == null || !unit.IsValid || (!EvadeInit.IsDebugMode && unit.Team == GameObjects.Player.Team))
            {
                return;
            }

            
            var spellData = SpellDatabase.GetByMissileName(missile.SData.Name);
            if (spellData == null)
            {
                return;
            }
            

            var missilePosition = missile.Position.ToVector2();
            var unitPosition = missile.StartPosition.ToVector2();
            var endPos = missile.EndPosition.ToVector2();
            var direction = (endPos - unitPosition).Normalized();
            if (unitPosition.Distance(endPos) > spellData.Range || spellData.FixedRange)
            {
                endPos = unitPosition + direction * spellData.Range;
            }
            if (spellData.ExtraRange != -1)
            {
                endPos = endPos
                         + Math.Min(spellData.ExtraRange, spellData.Range - endPos.Distance(unitPosition)) * direction;
            }
            var castTime = Variables.TickCount - Game.Ping / 2 - (spellData.MissileDelayed ? 0 : spellData.Delay)
                           - (int)(1000f * missilePosition.Distance(unitPosition) / spellData.MissileSpeed);

            if (spellData.NOTDASH)
            {
                if (!unit.IsDashing())
                {
                    TriggerOnDetectSkillshot(DetectionType.RecvPacket, spellData, castTime, unitPosition, endPos, unit);
                    return;
                }
            }
            else
                TriggerOnDetectSkillshot(DetectionType.RecvPacket, spellData, castTime, unitPosition, endPos, unit);
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var unit = sender as AIHeroClient;
            //DEBUGPORT
            if (unit == null || !unit.IsValid || (!EvadeInit.IsDebugMode && unit.Team == GameObjects.Player.Team))
            {
                return;
            }

            if (args.SData.Name == "dravenrdoublecast")
            {
                Evade.DetectedSkillshots.RemoveAll(
                    i => i.Unit.NetworkId == unit.NetworkId && i.SpellData.SpellName == "DravenRCast");
            }
            if (args.SData.Name == "LissandraE")
            {
                if (Evade.DetectedSkillshots.Any(i => i.Unit.NetworkId == unit.NetworkId && i.SpellData.SpellName == "LissandraE"))
                {
                    Evade.DetectedSkillshots.RemoveAll(i => i.Unit.NetworkId == unit.NetworkId && i.SpellData.SpellName == "LissandraE");
                    return;
                }
            }
            //Game.Print(args.SData.Name);
            var spellData = SpellDatabase.GetByName(args.SData.Name);
            if (spellData == null)
            {
                return;
            }

            var startPos = Vector2.Zero;
            if (spellData.FromObject != "")
            {
                foreach (var obj in GameObjects.AllGameObjects.Where(i => i.Name.Contains(spellData.FromObject)))
                {
                    startPos = obj.Position.ToVector2();
                }
            }
            else
            {

                startPos = unit.Position.ToVector2();
            }
            if (spellData.FromObjects != null && spellData.FromObjects.Length > 0)
            {

                foreach (var obj in
                    GameObjects.AllGameObjects.Where(i => spellData.FromObjects.Contains(i.Name) && !i.IsDead))
                {
                    var start = obj.Position.ToVector2();
                    var endPosGC = args.To.ToVector2();
                    var directionGC = (endPosGC - start).Normalized();
                    if (start.Distance(endPosGC) > spellData.Range || spellData.FixedRange)
                    {
                        endPosGC = startPos + directionGC * spellData.Range;
                    }


                    var end = start + spellData.Range * (args.To.ToVector2() - obj.Position.ToVector2()).Normalized();
                    TriggerOnDetectSkillshot(
                        DetectionType.ProcessSpell,
                        spellData,
                        Variables.TickCount - Game.Ping / 2,
                        start,
                        endPosGC,
                        unit);
                }
                return;
            }
            if (!startPos.IsValid())
            {
                return;
            }



            var endPos = args.To.ToVector2();


            if (endPos.IsZero)
            {
                endPos = args.End.ToVector2();
            }
            if (args.SData.Name == "DariusAxeGrabCone" || args.SData.Name == "Volley")
            {
                endPos = args.End.ToVector2();
            }



            if (spellData.SpellName == "LucianQ" && args.Target != null
                && args.Target.NetworkId == GameObjects.Player.NetworkId)
            {
                return;
            }

            var direction = (endPos - startPos).Normalized();
            if (startPos.Distance(endPos) > spellData.Range || spellData.FixedRange)
            {
                endPos = startPos + direction * spellData.Range;
            }
            if (spellData.ExtraRange != -1)
            {
                endPos = endPos
                         + Math.Min(spellData.ExtraRange, spellData.Range - endPos.Distance(startPos)) * direction;
            }

            #region 技能生成
            if (spellData.NOTDASH)
            {
                if (!unit.IsDashing())
                {
                    TriggerOnDetectSkillshot(
                DetectionType.ProcessSpell,
                spellData,
                Variables.TickCount - Game.Ping / 2,
                startPos,
                endPos,
                unit);
                    return;
                }
            }
            else
                TriggerOnDetectSkillshot(
                DetectionType.ProcessSpell,
                spellData,
                Variables.TickCount - Game.Ping / 2,
                startPos,
                endPos,
                unit);
            #endregion
        }

        public static void TriggerOnDetectSkillshot(
            DetectionType detectionType,
            SpellData spellData,
            int startT,
            Vector2 start,
            Vector2 end,
            AIBaseClient unit)
        {
            OnDetectSkillshot?.Invoke(new Skillshot(detectionType, spellData, startT, start, end, unit));
        }

        #endregion
    }
}
