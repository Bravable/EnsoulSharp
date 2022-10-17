using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using SharpDX;
namespace QSharp.Common.Evade.SpellsEvade
{
    internal class Evade
    {
        #region Static Fields

        public static List<Skillshot> DetectedSkillshots = new List<Skillshot>();

        public static int LastWardJumpAttempt = 0;

        #endregion

        #region Delegates

        public delegate void TryEvadingH(List<Skillshot> skillshot, Vector2 to);

        #endregion

        #region Public Events

        private static int ireliaWDealy = 0;


        public static event TryEvadingH TryEvading;

        #endregion

        #region Public Properties

        public static Vector2 PlayerPosition => GameObjects.Player.Position.ToVector2();

        #endregion

        #region Public Methods and Operators

        internal static void Init()
        {

            Config.CreateMenu(EvadeInit.EvadeMenu);
            Collision.Init();
            Game.OnUpdate += args =>
            {
                DetectedSkillshots.RemoveAll(i => !i.IsActive);
                foreach (var skillshot in DetectedSkillshots)
                {
                    skillshot.OnUpdate();
                }
                if (!EvadeInit.EvadeMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active)
                {
                    return;
                }
                if (GameObjects.Player.IsDead)
                {
                    return;
                }
                if (Interrupter.IsCastingImporantSpell(GameObjects.Player))
                {
                    return;
                }
                if (GameObjects.Player.HasBuffOfType(BuffType.SpellShield)
                    || GameObjects.Player.HasBuffOfType(BuffType.SpellImmunity))
                {
                    return;
                }
                var currentPath = GameObjects.Player.GetWaypoints();
                var safePoint = IsSafePoint(PlayerPosition);
                var safePath = IsSafePath(currentPath, 100);
                if (!safePath.IsSafe && !safePoint.IsSafe)
                {
                    TryEvading?.Invoke(safePoint.SkillshotList, Game.CursorPos.ToVector2());
                }
            };

            SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
            SkillshotDetector.OnDeleteSkillshot += (skillshot, missile) =>
            {
                if (skillshot.SpellData.SpellName == "VelkozQ")
                {
                    var spellData = SpellDatabase.GetByName("VelkozQSplit");
                    var direction = skillshot.Direction.Perpendicular();
                    if (DetectedSkillshots.Count(i => i.SpellData.SpellName == "VelkozQSplit") == 0)
                    {
                        for (var i = -1; i <= 1; i = i + 2)
                        {
                            var skillshotToAdd = new Skillshot(
                                DetectionType.ProcessSpell,
                                spellData,
                                Variables.TickCount,
                                missile.Position.ToVector2(),
                                missile.Position.ToVector2() + i * direction * spellData.Range,
                                skillshot.Unit);
                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                    }
                }
            };
            Drawing.OnEndScene += args =>
            {
                if (EvadeInit.EvadeMenu["Evade"]["Draw"]["Status"].GetValue<MenuBool>().Enabled)
                {
                    var active = EvadeInit.EvadeMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active;
                    var text =
                        $"ImpEvade: {(active ? (EvadeInit.EvadeMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active ? "Only Danger" : "Enable") : "Disable")}";
                    
                    var pos = Drawing.WorldToScreen(GameObjects.Player.Position);
                    Base.PlusRender.DrawText(
                        text,
                        pos.X - (float)Drawing.GetTextExtent(text).Width / 1.8f,
                        pos.Y + 25,
                        active
                            ? (EvadeInit.EvadeMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active
                                   ? Color.Yellow
                                   : Color.White)
                            : Color.Gray
                        );
                }
                if (EvadeInit.EvadeMenu["Evade"]["Draw"]["Skillshot"].GetValue<MenuBool>().Enabled)
                {
                    //Game.Print(DetectedSkillshots.Count.ToString());
                    foreach (var skillshot in DetectedSkillshots)
                    {
                        if (!skillshot.Enable || !skillshot.IsDraw)
                            continue;

                        skillshot.Draw(
                             skillshot.Enable && EvadeInit.EvadeMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active
                                ? Color.Aquamarine
                                : Color.Red, 1);
                    }
                }
            };
            TryEvading += TryToEvade;
        }
        public static bool IsAboutToHit(AIBaseClient unit, int time)
        {
            return SkillshotAboutToHit(unit, time).Count > 0;
        }
        public static List<Skillshot> GetSkillSource(AIBaseClient unit, int time)
        {
            return SkillshotAboutToHit(unit, time);
        }
        public static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
        {
            var isSafe = true;
            var intersections = new List<FoundIntersection>();
            var intersection = new FoundIntersection();
            foreach (var sResult in
                from skillshot in DetectedSkillshots
                where skillshot.Enable
                select skillshot.IsSafePath(path, timeOffset, speed, delay))
            {
                isSafe = isSafe && sResult.IsSafe;
                if (sResult.Intersection.Valid)
                {
                    intersections.Add(sResult.Intersection);
                }
            }
            if (isSafe)
            {
                return new SafePathResult(true, intersection);
            }
            var intersetion = intersections.MinOrDefault(i => i.Distance);
            return new SafePathResult(false, intersetion.Valid ? intersetion : intersection);
        }

        public static IsSafeResult IsSafePoint(Vector2 point)
        {
            var result = new IsSafeResult { SkillshotList = new List<Skillshot>() };
            foreach (var skillshot in DetectedSkillshots.Where(i => i != null && i.Enable && i.IsDanger(point)))
            {
                result.SkillshotList.Add(skillshot);

            }
            result.IsSafe = result.SkillshotList.Count == 0;
            return result;
        }

        public static bool IsSafeToBlink(Vector2 point, int timeOffset, int delay)
        {
            return DetectedSkillshots.Where(i => i.Enable).All(i => i.IsSafeToBlink(point, timeOffset, delay));
        }

        public static List<Skillshot> SkillshotAboutToHit(AIBaseClient unit, int time, bool onlyWindWall = false)
        {
            time += 150;

            return
                DetectedSkillshots.Where(
                    i =>
                    i.Enable && i.IsAboutToHit(time, unit)
                    && (!onlyWindWall || i.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)))
                    .OrderBy(i => i.DangerLevel)
                    .ToList();
        }

        #endregion
        private static AIBaseClient Closest(List<AIBaseClient> targetList, Vector2 from)
        {
            var dist = float.MaxValue;
            AIBaseClient result = null;

            foreach (var target in targetList)
            {
                var distance = Vector2.DistanceSquared(from, target.Position.ToVector2());
                if (distance < dist)
                {
                    dist = distance;
                    result = target;
                }
            }

            return result;
        }
        #region try
        private static void TryToEvade(List<Skillshot> hitBy, Vector2 to)
        {
            var dangerLevel = hitBy.Select(i => i.DangerLevel).Concat(new[] { 0 }).Max();
            foreach (var evadeSpell in EvadeSpellDatabase.Spells)
            {
                if (evadeSpell.Enable && evadeSpell.DangerLevel <= dangerLevel)
                {
                    if (evadeSpell.IsReady)
                    {

                        if (evadeSpell.IsInvulnerability)
                        {
                            if (evadeSpell.IsTargetted)
                            {
                                var targets = Evader.GetEvadeTargets(evadeSpell.ValidTargets, int.MaxValue, 0, evadeSpell.MaxRange, true, false, true);

                                if (targets.Count > 0)
                                {
                                    if (IsAboutToHit(GameObjects.Player, evadeSpell.Delay))
                                    {
                                        var closestTarget = Closest(targets, to);
                                        GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, closestTarget);
                                    }
                                }
                            }
                            else
                            {
                                if (IsAboutToHit(GameObjects.Player, evadeSpell.Delay))
                                {
                                    if (evadeSpell.SelfCast)
                                    {
                                        if (evadeSpell.Name == "Irelia W")
                                        {
                                            if (!GameObjects.Player.HasBuff("ireliawdefense") && Variables.TickCount > ireliaWDealy)
                                            {
                                                GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, GameObjects.Player.Position.Extend(hitBy.FirstOrDefault().Start, 100));
                                                ireliaWDealy = Variables.TickCount + 500;
                                                DelayAction.Add(300, () =>
                                                {
                                                    if (GameObjects.Player.HasBuff("ireliawdefense"))
                                                    {
                                                        GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, GameObjects.Player.Position.Extend(hitBy.FirstOrDefault().Start, 100));
                                                    }
                                                });
                                            }
                                        }
                                        else
                                        {
                                            GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot);
                                        }

                                    }
                                    else
                                    {
                                        GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, GameObjects.Player.Position.Extend(hitBy.FirstOrDefault().Start, 100));
                                    }

                                }
                            }
                        }
                        if (evadeSpell.IsDash)
                        {
                            if (evadeSpell.IsTargetted && !evadeSpell.Name.Equals("Irelia Q"))
                            {
                                var targets = Evader.GetEvadeTargets(evadeSpell.ValidTargets, evadeSpell.Speed, evadeSpell.Delay, evadeSpell.MaxRange, false, false).Where(x => !x.Position.ToVector2().IsUnderEnemyTurret() || EvadeInit.EvadeMenu["Evade"]["Spells"][evadeSpell.Name][evadeSpell.Slot.ToString() + "Tower"].GetValue<MenuBool>().Enabled).ToList();

                                if (targets.Count > 0)
                                {
                                    GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, targets.MinOrDefault(i => i.DistanceSquared(to)));
                                }

                            }
                            else
                            {
                                var points = Evader.GetEvadePoints(evadeSpell.Speed, evadeSpell.Delay, false, false).Where(x => CheckPointIsSafe(evadeSpell.MaxRange,x)).ToList();
                                // Remove the points out of range
                                points.RemoveAll(item => item.Distance(GameObjects.Player.Position) > evadeSpell.MaxRange);

                                if (points.Count > 0)
                                {
                                    var EvadePoint = Game.CursorPos.ToVector2().Closest(points);

                                    if (!evadeSpell.Invert)
                                    {
                                        GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, EvadePoint.ToVector3());
                                    }
                                    else
                                    {
                                        var castPoint = GameObjects.Player.Position - (EvadePoint.ToVector3() - GameObjects.Player.Position);
                                        GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, castPoint);
                                    }

                                }
                            }
                        }

                        if (evadeSpell.IsBlink && IsAboutToHit(ObjectManager.Player, evadeSpell.Delay))
                        {
                            //Targetted blinks
                            if (evadeSpell.IsTargetted)
                            {
                                var targets = Evader.GetEvadeTargets(evadeSpell.ValidTargets, int.MaxValue, evadeSpell.Delay, evadeSpell.MaxRange, true, false);

                                if (targets.Count > 0)
                                {
                                    if (IsAboutToHit(ObjectManager.Player, evadeSpell.Delay))
                                    {
                                        var closestTarget = Closest(targets, to);
                                        var EvadePoint = closestTarget.ServerPosition.ToVector2();

                                        ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, closestTarget);
                                    }
                                    return;
                                }
                                if (Variables.TickCount - LastWardJumpAttempt < 250)
                                {
                                    return;
                                }
                            }
                            //Skillshot type blinks.
                            else
                            {
                                var points = Evader.GetEvadePoints(int.MaxValue, evadeSpell.Delay, true);

                                // Remove the points out of range
                                points.RemoveAll(item => item.Distance(ObjectManager.Player.ServerPosition) > evadeSpell.MaxRange || item.IsWall());

                                //points.OrderBy(x=> x.Distance)
                                if (points.Count > 0)
                                {
                                    foreach (var extraPoint in Evader.CirclePoints(30, evadeSpell.MaxRange, PlayerPosition.ToVector3World()))
                                    {
                                        if (!extraPoint.IsWall() && IsSafeToBlink(extraPoint.ToVector2(), Config.EvadingFirstTimeOffset, evadeSpell.Delay))
                                        {
                                            points.Add(extraPoint.ToVector2());
                                        }
                                    }
                                    var EvadePoint = Game.CursorPos.ToVector2().Closest(points);
                                    GameObjects.Player.Spellbook.CastSpell(evadeSpell.Slot, EvadePoint.ToVector3());

                                }
                            }
                        }

                    }

                }
            }

        }
        #endregion
        #region Methods
        public static bool CheckPointIsSafe(float spellRange,Vector2 Pos)
        {
            float segment = spellRange / 5;
            for (int i = 1; i <= 5; i++)
            {
                if (GameObjects.Player.Position.Extend(Pos, i * segment).IsWall())
                    return false;
            }
            var enemys = GameObjects.Get<AIHeroClient>().Where(x => x.IsValidTarget() && x.IsMelee).Any(x => x.Distance(Pos) <= (x.AttackRange + GameObjects.Player.BoundingRadius + 50f));
            if(enemys)
            {
                return false;
            }

            var enemyCountDashPos = Pos.CountEnemyHerosInRangeFix(350);//获取位移终点敌人

            if (enemyCountDashPos >= 1)
            {
                return false;
            }
            var enemyCountPlayer = PlayerPosition.CountEnemyHerosInRangeFix(400);

            if (enemyCountDashPos > enemyCountPlayer)
            {
                return false;
            }

            return true;
        }
        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            //Check if the skillshot is already added.
            var alreadyAdded = false;
            foreach (var item in DetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName &&
                    (item.Unit.NetworkId == skillshot.Unit.NetworkId &&
                     (skillshot.Direction).AngleBetween(item.Direction) < 5 &&
                     (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0)))
                {
                    alreadyAdded = true;
                    break;
                }
            }
            if ((!EvadeInit.IsDebugMode && skillshot.Unit.Team == GameObjects.Player.Team))
            {
                return;
            }


            //Check if the skillshot is too far away.
            if (skillshot.Start.Distance(PlayerPosition) > (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
                return;

            //Add the skillshot to the detected skillshot list.
            if (!alreadyAdded || skillshot.SpellData.DontCheckForDuplicates)
            {
                //Multiple skillshots like twisted fate Q.
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;

                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                            i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range *
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Unit);

                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "BardR" && skillshot.End.Distance(skillshot.Start) < 850)
                    {
                        skillshot.StartTick = Variables.TickCount - skillshot.SpellData.Delay + 800;
                    }

                    if (skillshot.SpellData.SpellName == "MordekaiserE")
                    {
                        var end = skillshot.End;
                        if (skillshot.Start.Distance(skillshot.End) > 700)
                        {

                            end = skillshot.Start + skillshot.Direction * 700;
                        }

                        skillshot.End = end + skillshot.Direction * 275;
                        skillshot.Start = skillshot.End;
                        skillshot.End = skillshot.End - skillshot.Direction * 900;
                        skillshot.SpellData.Delay = 200 + 250 + (int)((skillshot.Start.Distance(skillshot.End) / skillshot.SpellData.MissileSpeed) * 1000);

                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, skillshot.End,
                            skillshot.Unit);

                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "SennaQCast" && skillshot.Unit != null)
                    {
                        skillshot.SpellData.Delay = (int)(skillshot.Unit.AttackCastDelay * 1000);
                    }

                    if (skillshot.SpellData.SpellName == "UFSlash")
                    {
                        skillshot.SpellData.MissileSpeed = 1600 + (int)skillshot.Unit.MoveSpeed;
                    }

                    if (skillshot.SpellData.SpellName == "SionR")
                    {
                        skillshot.SpellData.MissileSpeed = (int)skillshot.Unit.MoveSpeed;
                    }

                    if (skillshot.SpellData.Invert)
                    {
                        var newDirection = -(skillshot.End - skillshot.Start).Normalized();
                        var end = skillshot.Start + newDirection * skillshot.Start.Distance(skillshot.End);
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "TaricE" && (skillshot.Unit as AIHeroClient).CharacterName == "Taric")
                    {

                        var target = GameObjects.Get<AIHeroClient>().Where(y => y.IsValidTarget()).FirstOrDefault(h => h.Team == skillshot.Unit.Team && h.IsVisible && h.HasBuff("taricwleashactive"));
                        if (target != null)
                        {
                            var start = target.Position.ToVector2();
                            var direction = (skillshot.End - start).Normalized();
                            var end = start + direction * skillshot.SpellData.Range;
                            var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick,
                                    start, end, target)
                            {

                            };
                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                    }

                    if (skillshot.SpellData.SpellName == "SylasQ")
                    {
                        var sylasQLine = SpellDatabase.GetByName("SylasQLine");

                        if (sylasQLine != null)
                        {
                            var dir = skillshot.Direction.Perpendicular();
                            var leftStart = skillshot.Start + dir * 125;
                            var leftEnd = leftStart.Extend(skillshot.End, sylasQLine.Range);

                            var rightStart = skillshot.Start - dir * 125;
                            var rightEnd = rightStart.Extend(skillshot.End, sylasQLine.Range);

                            DetectedSkillshots.Add(new Skillshot(skillshot.DetectionType, sylasQLine, skillshot.StartTick, leftStart, leftEnd, skillshot.Unit));
                            DetectedSkillshots.Add(new Skillshot(skillshot.DetectionType, sylasQLine, skillshot.StartTick, rightStart, rightEnd, skillshot.Unit));
                        }
                    }

                    if (skillshot.SpellData.SpellName == "PykeR")
                    {
                        var start2 = skillshot.End + new Vector2(250, -250);
                        var end2 = skillshot.End + new Vector2(-250, 250);

                        skillshot.Start = skillshot.End - new Vector2(250, 250);
                        skillshot.End = skillshot.End + new Vector2(250, 250);

                        DetectedSkillshots.Add(new Skillshot(skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start2, end2, skillshot.Unit));
                        DetectedSkillshots.Add(new Skillshot(skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, skillshot.End, skillshot.Unit));
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        var angle = 60;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.Position.ToVector2()).Rotated(
                                -angle / 2 * (float)Math.PI / 180);
                        var edge2 = edge1.Rotated(angle * (float)Math.PI / 180);

                        var positions = new List<Vector2>();

                        var explodingQ = DetectedSkillshots.FirstOrDefault(s => s.SpellData.SpellName == "SyndraQ");

                        if (explodingQ != null)
                        {
                            var position = explodingQ.End;
                            var v = position - skillshot.Unit.Position.ToVector2();
                            if (edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                position.Distance(skillshot.Unit) < 800)
                            {
                                var start = position;
                                var end = skillshot.Unit.Position.ToVector2()
                                    .Extend(position, skillshot.Unit.Distance(position) > 200 ? 1300 : 1000);

                                var startTime = skillshot.StartTick;

                                startTime += (int)(150 + Math.Min(250, explodingQ.StartTick + explodingQ.SpellData.Delay - 150 - Variables.TickCount) + skillshot.Unit.Distance(position) / 2.5f);
                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, startTime, start, end,
                                    skillshot.Unit);
                                DetectedSkillshots.Add(skillshotToAdd);
                            }
                        }

                        foreach (var minion in GameObjects.Get<AIMinionClient>())
                        {
                            if (minion.Name == "Seed" && !minion.IsDead && (minion.Team != GameObjects.Player.Team))
                            {
                                positions.Add(minion.Position.ToVector2());
                            }
                        }

                        foreach (var position in positions)
                        {
                            var v = position - skillshot.Unit.Position.ToVector2();
                            if (edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                position.Distance(skillshot.Unit) < 800)
                            {
                                var start = position;
                                var end = skillshot.Unit.Position.ToVector2()
                                    .Extend(position, skillshot.Unit.Distance(position) > 200 ? 1300 : 1000);

                                var startTime = skillshot.StartTick;

                                startTime += (int)(150 + skillshot.Unit.Distance(position) / 2.5f);
                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, startTime, start, end,
                                    skillshot.Unit);
                                DetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "ZoeE")
                    {
                        Vector2 wall_start = Vector2.Zero;
                        float range_left = 0;
                        var range_max = skillshot.SpellData.RawRange + skillshot.SpellData.ExtraRange;

                        for (int i = 0; i < range_max; i += 10)
                        {
                            var curr_pos = skillshot.Start + skillshot.Direction * i;

                            if (curr_pos.IsWall())
                            {
                                wall_start = curr_pos;
                                range_left = range_max - i;
                                break;
                            }
                        }

                        int max = 70;
                        while (wall_start.IsWall() && max > 0)
                        {
                            wall_start = wall_start + skillshot.Direction * 35;
                            max--;
                        }

                        for (int i = 0; i < range_left; i += 10)
                        {
                            var curr_pos = wall_start + skillshot.Direction * i;

                            if (curr_pos.IsWall())
                            {
                                range_left = i;
                                break;
                            }
                        }



                        if (range_left > 0)
                        {
                            skillshot.End = wall_start + skillshot.Direction * range_left;

                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, skillshot.End,
                                skillshot.Unit);
                            DetectedSkillshots.Add(skillshotToAdd);
                            return;
                        }
                    }

                    if (skillshot.SpellData.SpellName == "MalzaharQ")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "ZyraQ")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 450;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 450;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "DianaQ")
                    {
                        var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, SpellDatabase.GetByName("DianaArcArc"), skillshot.StartTick, skillshot.Start, skillshot.End, skillshot.Unit);

                        DetectedSkillshots.Add(skillshotToAdd);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;


                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");

                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;

                        bounce1SpellData.Delay =
                            (int)(skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int)(bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);

                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Unit);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Unit);

                        DetectedSkillshots.Add(bounce1);
                        DetectedSkillshots.Add(bounce2);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }

                    if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                    {
                        var endPos = new Vector2();

                        foreach (var s in DetectedSkillshots)
                        {
                            if (s.Unit.NetworkId == skillshot.Unit.NetworkId && s.SpellData.Slot == SpellSlot.E)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100, skillshot.Unit);
                                if (!extendedE.IsSafe(s.End))
                                {
                                    endPos = s.End;
                                }
                                break;
                            }
                        }

                        foreach (var m in GameObjects.Get<AIMinionClient>())
                        {
                            if (m.SkinName == "jarvanivstandard" && m.Team == skillshot.Unit.Team)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100, skillshot.Unit);
                                if (!extendedE.IsSafe(m.Position.ToVector2()))
                                {
                                    endPos = m.Position.ToVector2();
                                }
                                break;
                            }
                        }

                        if (endPos.IsValid())
                        {
                            skillshot = new Skillshot(DetectionType.ProcessSpell, SpellDatabase.GetByName("JarvanIVEQ"), Variables.TickCount, skillshot.Start, endPos, skillshot.Unit);
                            skillshot.End = endPos + 200 * (endPos - skillshot.Start).Normalized();
                            skillshot.Direction = (skillshot.End - skillshot.Start).Normalized();
                        }
                    }
                }

                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, SpellDatabase.GetByName("OriannaQend"), skillshot.StartTick, skillshot.Start, skillshot.End,
                        skillshot.Unit);

                    DetectedSkillshots.Add(skillshotToAdd);
                }

                if (skillshot.SpellData.SpellName == "IreliaE2")
                {
                    var reg = new System.Text.RegularExpressions.Regex("Irelia_.+_E_.+_Indicator");
                    var firstE = GameObjects.Get<EffectEmitter>().Where(x => x.IsValid && reg.IsMatch(x.Name)).
                        OrderByDescending(x => x.Position.Distance(skillshot.Start.ToVector3())).FirstOrDefault();

                    if (firstE == null)
                    {
                        var firstEMissile = GameObjects.Get<MissileClient>().Where(x =>
                            x.IsValid && x.EndPosition.ToVector2().Distance(skillshot.End) > 5 && x.SData.Name == skillshot.SpellData.MissileSpellName).
                            OrderByDescending(x => x.Position.Distance(skillshot.Start.ToVector3())).FirstOrDefault();

                        if (firstEMissile != null && !skillshot.End.IsZero)
                        {
                            if (skillshot.End.Distance(skillshot.Unit) > 840)
                                return;

                            var skillshotToAdd = new Skillshot(skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, firstEMissile.EndPosition.ToVector2(), skillshot.End,
                                skillshot.Unit);
                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }

                    if (firstE != null && !skillshot.End.IsZero)
                    {
                        if (skillshot.End.Distance(skillshot.Unit) > 840)
                            return;

                        var skillshotToAdd = new Skillshot(skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, firstE.Position.ToVector2(), skillshot.End,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                    }
                    return;
                }


                //Dont allow fow detection.
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }
#if DEBUG
                Console.WriteLine(Variables.TickCount + "Adding new skillshot: " + skillshot.SpellData.SpellName);
#endif


                DetectedSkillshots.Add(skillshot);
            }
        }



        #endregion

        public struct IsSafeResult
        {
            #region Fields

            public bool IsSafe;

            public List<Skillshot> SkillshotList;

            public AIHeroClient caster;

            #endregion
        }
    }
}
