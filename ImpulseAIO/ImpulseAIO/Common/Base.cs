using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Rendering.Caches;
using Newtonsoft.Json;
using SharpDX;
namespace ImpulseAIO.Common
{
    internal class Base
    {
        public static string PassiveKey;
        public static Menu CommonMenu;
        public static Menu DashMenu;
        public static Menu InvisibilityEvadeMenu;
        public static Menu ChampionMenu;
        public static Menu ActivatorMenu;
        public static string[] HookList = new string[] { "Blitzcrank","Thresh","Pyke", "Nautilus" };
        public static Dictionary<int, bool> HasHookFlag = new Dictionary<int, bool>();
        public static bool HasHookChamp = false;
        private static int CheckTime = 0;
        public static bool Enable_laneclear => ChampionMenu["LT"].GetValue<MenuKeyBind>().Active;
        public static SpellSlot Flash = SpellSlot.Unknown;
        public string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }
        public static int AaIndicator(AIHeroClient enemy,DamageType type = DamageType.Physical,float Damage = 0)
        { 
            if(Damage == 0)
            {
                switch (type)
                {
                    case DamageType.Physical:
                        Damage = Player.TotalAttackDamage;
                        break;
                    default:
                        Damage = 200;
                        break;
                }
            }
            var aCalculator = Player.CalculateDamage(enemy, type, Damage);
            var killableAaCount = enemy.GetRealHeath(type) / aCalculator;
            var totalAa = (int)Math.Ceiling(killableAaCount);
            return totalAa;
        }
        public static void InitCCTracker()
        {
            HasHookChamp = Base.Cache.AlliesHeroes.Where(x => HookList.Any(y => y.Equals(x.CharacterName))).Any();
            foreach (var enemy in Base.Cache.EnemyHeroes)
            {
                HasHookFlag.Add(enemy.NetworkId, false);
            }
            AIBaseClient.OnBuffAdd += OnBuffAdd_ByCC;
            AIBaseClient.OnBuffRemove += OnBuffRemove_ByCC;
        }
        static int www;
        private static void OnBuffAdd_ByCC(AIBaseClient sender,AIBaseClientBuffAddEventArgs args)
        {
            if (sender.IsEnemy && HasHookChamp)
            {
                if(args.Buff.Name == "Stun")
                {
                    www = Variables.GameTimeTickCount;
                }
                if(args.Buff.Name == "rocketgrab2")
                {
                    HasHookFlag[sender.NetworkId] = true;
                }
                if (args.Buff.Name == "ThreshQ")
                {
                    HasHookFlag[sender.NetworkId] = true;
                }
            }
        }
        private static void OnBuffRemove_ByCC(AIBaseClient sender, AIBaseClientBuffRemoveEventArgs args)
        {
            if (sender.IsEnemy && HasHookChamp)
            {
                if (args.Buff.Name == "rocketgrab2")
                {
                    HasHookFlag[sender.NetworkId] = false;
                }
                if (args.Buff.Name == "ThreshQ")
                {
                    HasHookFlag[sender.NetworkId] = false;
                }
            }
        }
        public IMPTargetInfo IMPGetTarGet(Spell spell,bool OnlyGetObj = false,HitChance minHitChance = HitChance.Medium,float extraRange = 0f)
        {
            var List = TargetSelector.GetTargets(spell.Range + extraRange, spell.DamageType,true,spell.From);
            var defaultinfo = new IMPTargetInfo
            {
                CastPosition = Vector3.Zero,
                UnitPosition = Vector3.Zero,
                Obj = null,
                SuccessFlag = false
            };
            AIHeroClient predThisObj = null;
            if (List.Count > 0)
            {
                if (spell.Collision)//如果技能存在碰撞
                {
                    foreach(var obj in List)//从优先级开始判断
                    {
                        var pred = spell.GetPrediction(obj);

                        if(pred.Hitchance >= minHitChance)  //第一个目标永远是最好的
                        {
                            defaultinfo.CastPosition = pred.CastPosition;
                            defaultinfo.UnitPosition = pred.UnitPosition;
                            defaultinfo.Obj = obj;
                            defaultinfo.SuccessFlag = true;
                            break;
                        }
                        else
                        {
                            if(pred.Hitchance > HitChance.OutOfRange)
                            {
                                predThisObj = obj;
                                break;
                            }
                        }
                    }
                    if(predThisObj != null)
                    {
                        var pred = spell.GetPrediction(predThisObj);
                        if(pred.Hitchance >= minHitChance)
                        {
                            defaultinfo.CastPosition = pred.CastPosition;
                            defaultinfo.UnitPosition = pred.UnitPosition;
                            defaultinfo.Obj = predThisObj;
                            defaultinfo.SuccessFlag = true;
                        }
                    }
                    return defaultinfo;
                }
                var t = List.FirstOrDefault();
                if(t != null)
                {
                    if (OnlyGetObj)
                    {
                        defaultinfo.Obj = t;
                        defaultinfo.SuccessFlag = true;
                        return defaultinfo;
                    }
                    var pred = spell.GetPrediction(t);
                    if(pred.Hitchance >= minHitChance)
                    {
                        defaultinfo.CastPosition = pred.CastPosition;
                        defaultinfo.UnitPosition = pred.UnitPosition;
                        defaultinfo.Obj = t;
                        defaultinfo.SuccessFlag = true;
                        return defaultinfo;
                    }
                }
            }
            return defaultinfo;
        }
        public static Vector2 GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25)
        {
            var direction = (to - from).Normalized();

            for (float d = 0; d < from.Distance(to); d = d + step)
            {
                var testPoint = from + d * direction;
                var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
                if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
                {
                    return from + (d - step) * direction;
                }
            }
            return Vector2.Zero;
        }
        public static AIHeroClient Player
        {
            get { return GameObjects.Player; }
        }
        public static bool InMelleAttackRange(Vector3 point)
        {
            //获取敌对近战英雄

            var Melees = Base.Cache.EnemyHeroes.Where(x => x.IsValidTarget() && !x.IsDead &&
                                                                x.CombatType == GameObjectCombatType.Melee).ToList();
            return Cache.EnemyHeroes.Where(x => x.IsValidTarget() && x.CombatType == GameObjectCombatType.Melee).Any(x => x.Distance(point) <= (x.AttackRange + x.BoundingRadius + Player.BoundingRadius + 120f) && GetCCBuffPos(x) == Vector3.Zero);
        }
        public static List<Vector3> GetRotatedFlashPositions(AIBaseClient uns,float ExtraWidth)
        {
            const int currentStep = 60;
            var direction = uns.Direction.ToVector2().Perpendicular();

            var list = new List<Vector3>();
            for (var i = 60; i <= 360; i += currentStep)
            {
                var angleRad = Geometry.DegreeToRadian(i);
                var rotatedPosition = uns.Position.ToVector2() + ((uns.BoundingRadius + ExtraWidth) * direction.Rotated(angleRad));
                list.Add(rotatedPosition.ToVector3());
            }
            return list;
        }

        public static Vector3 GetCCBuffPos(AIBaseClient target)
        {
            var nomalret = target.IsDashing() ? target.GetDashInfo().EndPos.ToVector3World() : target.ServerPosition;
            if (target.HasBuffOfType(BuffType.Stun) && Variables.GameTimeTickCount > CheckTime)
            {
                if (!HasHookChamp)
                {
                    return nomalret;
                }

                if (!target.IsDashing())
                {
                    CheckTime = Variables.GameTimeTickCount + 250 + Game.Ping;
                    return Vector3.Zero;
                }
                if (target.IsDashing())
                {
                    goto tag1;
                }
                else
                {
                    return nomalret;
                }
            }
            tag1:
            BuffType[] types =
                {
                    BuffType.Knockup,//击飞
                   BuffType.Knockback,//击退
                   BuffType.Suppression, //压制
                   BuffType.Snare, //陷阱
                    BuffType.Charm,//魅惑
                    BuffType.Taunt, //嘲讽
                    BuffType.Fear //害怕
                };
            if (types.Any(x => target.HasBuffOfType(x)))
            {
                return nomalret;
            }
            return Vector3.Zero;
        }
        public class IMPTargetInfo
        {
            public Vector3 UnitPosition;
            public Vector3 CastPosition;
            public bool SuccessFlag;
            public AIBaseClient Obj;
        }
        public class Dash
        {
            private static Spell DashSpell;
            private static int colorindex = 0;
            private static int DashMode => DashMenu["DashMode"].GetValue<MenuList>().Index;
            public static int EnemyCheck => DashMenu["EnemyCheck"].GetValue<MenuSlider>().Value;
            public static int CheckRange => DashMenu["CheckRange"].GetValue<MenuSlider>().Value;
            private static bool WallCheck => DashMenu["WallCheck"].GetValue<MenuBool>().Enabled;
            public static bool TurretCheck => DashMenu["TurretCheck"].GetValue<MenuKeyBind>().Active;
            private static bool AAcheck => DashMenu["AAcheck"].GetValue<MenuBool>().Enabled;
            private static bool notDashAARange => DashMenu["notDashAARange"].GetValue<MenuBool>().Enabled;
            public Dash(Spell arg)
            {
                if(DashMenu == null)
                {
                    DashSpell = arg;
                    DashMenu = ChampionMenu.Add(new Menu("QDash", Program.Chinese ? "突进技能设置" : "Dash Spell Setting", true));
                    DashMenu.Add(new MenuList("DashMode", Program.Chinese ? "突进模式" : "Dash Mode", new string[] { "Mouse", "Side", "Safe" }, 2));
                    DashMenu.Add(new MenuSlider("EnemyCheck", Program.Chinese ? "突进终点敌人数最多有X个" : "Dash EndPosition Max EnemyHero Count", 3, 1, 5));
                    DashMenu.Add(new MenuSlider("CheckRange", Program.Chinese ? "-> 侦测距离" : "-> Check Range", 450, 100, 800));
                    DashMenu.Add(new MenuBool("WallCheck", Program.Chinese ?"禁止向墙边突进" : "Not Dash To Wall"));
                    DashMenu.Add(new MenuKeyBind("TurretCheck", Program.Chinese ? "禁止向防御塔内突进" : "Not Dash To Enemy Turrent", Keys.A, KeyBindType.Toggle, true)).AddPermashow();
                    DashMenu.Add(new MenuBool("AAcheck", Program.Chinese ?  "确保突进终点可以普攻到敌人" : "Dash End Position Can Attack Any EnemyHero"));
                    DashMenu.Add(new MenuBool("notDashAARange", Program.Chinese ? "不突进到敌对近战英雄的攻击距离" : "Not Dash To Melle Enemy Hero Attack Range"));
                    Render.OnEndScene += OnDrawendScene;
                }
            }
            private void OnDrawendScene(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
            }
            public bool InAARange(Vector3 point)
            {
                if (!AAcheck)
                    return true;

                var Base = Orbwalker.GetTarget() as AIHeroClient;
                if(Base == null)
                {
                    return point.CountEnemyHerosInRangeFix(Player.AttackRange + Player.BoundingRadius + 50f) > 0;
                }
                return point.Distance(Base.ServerPosition) < Player.AttackRange + Player.BoundingRadius + Base.BoundingRadius;
            }
            public bool IsGoodPosition(Vector3 dashPos)
            {
                if (WallCheck)
                {
                    float segment = DashSpell.Range / 5;
                    for (int i = 1; i <= 5; i++)
                    {
                        if (Player.ServerPosition.Extend(dashPos, i * segment).IsWall())
                            return false;
                    }
                }

                if (TurretCheck)
                {
                    if (dashPos.IsUnderEnemyTurret())
                        return false;
                }

                if (InMelleAttackRange(dashPos))
                {
                    return false;
                }
                    
                var enemyCountDashPos = dashPos.CountEnemyHerosInRangeFix(CheckRange);//获取位移终点敌人

                if (enemyCountDashPos <= EnemyCheck)
                    return true;

                var enemyCountPlayer = Player.CountEnemyHerosInRangeFix(400);

                if (enemyCountDashPos <= enemyCountPlayer)
                    return true;
                return false;
            }
            public bool InMelleAttackRange(Vector3 point)
            {
                if (!notDashAARange)
                    return false;
                //获取敌对近战英雄

                var Melees = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && 
                                                                    x.CombatType == GameObjectCombatType.Melee).ToList();
                return Melees.Any(x => point.Distance(x) <= (x.AttackRange + x.BoundingRadius + Player.BoundingRadius + 10f));
            }
            public Vector3 CastDash(bool asap = false)
            {
                Vector3 bestpoint = Vector3.Zero;
                if (DashMode == 0)
                {
                    bestpoint = Player.ServerPosition.Extend(Game.CursorPos, DashSpell.Range);
                }
                else if (DashMode == 1)
                {
                    var orbT = Orbwalker.GetTarget();
                    if (orbT != null && orbT is AIHeroClient)
                    {
                        var neworbT = orbT as AIHeroClient;
                        Vector2 start = Player.ServerPosition.ToVector2();
                        Vector2 end = neworbT.ServerPosition.ToVector2();
                        var dir = (end - start).Normalized();
                        var pDir = dir.Perpendicular();

                        var rightEndPos = end + pDir * Player.Distance(orbT);
                        var leftEndPos = end - pDir * Player.Distance(orbT);

                        var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, Player.ServerPosition.Z);
                        var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, Player.ServerPosition.Z);

                        if (Game.CursorPos.Distance(rEndPos) < Game.CursorPos.Distance(lEndPos))
                        {
                            bestpoint = Player.ServerPosition.Extend(rEndPos, DashSpell.Range);
                        }
                        else
                        {
                            bestpoint = Player.ServerPosition.Extend(lEndPos, DashSpell.Range);
                        }
                    }
                }
                else if (DashMode == 2)
                {
                    var points = new Geometry.Circle(Player.ServerPosition, DashSpell.Range).Points.Select(x => x.ToVector3());
                    bestpoint = Player.ServerPosition.Extend(Game.CursorPos, DashSpell.Range);
                    int enemies = bestpoint.CountEnemyHerosInRangeFix(350);
                    foreach (var point in points)
                    {
                        int count = point.CountEnemyHerosInRangeFix(350);
                        if (!InAARange(point))
                            continue;
                        if (InMelleAttackRange(point))
                            continue;
                        if (point.IsUnderAllyTurret())
                        {
                            bestpoint = point;
                            enemies = count - 1;
                        }
                        else if (count < enemies)
                        {
                            enemies = count;
                            bestpoint = point;
                        }
                        else if (count == enemies && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestpoint))
                        {
                            enemies = count;
                            bestpoint = point;
                        }
                    }
                }

                var isGoodPos = IsGoodPosition(bestpoint);

                if (asap && isGoodPos)
                {
                    return bestpoint;
                }
                else if (isGoodPos && InAARange(bestpoint))
                {
                    return bestpoint;
                }
                return Vector3.Zero;
            }
        }
        public class Cache
        {
            public static List<AIBaseClient> AllEnemyMinionsObj = new List<AIBaseClient>();
            public static List<AIHeroClient> EnemyHeroes = new List<AIHeroClient>();
            public static List<AIHeroClient> AlliesHeroes = new List<AIHeroClient>();
            public static List<AIBaseClient> MinionsListEnemy = new List<AIBaseClient>();
            public static List<AIBaseClient> MinionsListAlly = new List<AIBaseClient>();
            public static List<AIBaseClient> MinionsListJungle = new List<AIBaseClient>();
            public static List<AIBaseClient> AllyEyeList = new List<AIBaseClient>();
            private static int ChampTime = 0;
            static Cache()
            {
                foreach (var minion in GameObjects.Get<AIMinionClient>().Where(minion => minion.IsValid))
                {
                    AddMinionObject(minion);
                    if (!minion.IsAlly)
                        AllEnemyMinionsObj.Add(minion);
                }
                AddChamp();
                GameObject.OnCreate += Obj_AI_Base_OnCreate;
                Game.OnUpdate += Game_OnUpdate;
            }
            private static void AddChamp()
            {
                EnemyHeroes.Clear();
                AlliesHeroes.Clear();
                foreach (var obj in GameObjects.Get<AIHeroClient>())
                {
                    if (obj.IsEnemy)
                    {
                        EnemyHeroes.Add(obj);
                        continue;
                    }
                    if (obj.IsAlly)
                    {
                        AlliesHeroes.Add(obj);
                    }
                }
            }
            private static void ResetChamp()
            {
                if(Variables.TickCount > ChampTime)
                {
                    AddChamp();
                    ChampTime = Variables.TickCount + 1000 * 30;
                }
            }
            private static void Game_OnUpdate(EventArgs args)
            {
                MinionsListEnemy.RemoveAll(minion => !IsValidMinion(minion));
                MinionsListJungle.RemoveAll(minion => !IsValidMinion(minion));
                MinionsListAlly.RemoveAll(minion => !IsValidMinion(minion));
                AllEnemyMinionsObj.RemoveAll(minion => !IsValidMinion(minion));
                AllyEyeList.RemoveAll(minion => !IsValidMinion(minion));
                ResetChamp();
            }
            private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
            {
                var minion = sender as AIMinionClient;
                if (minion != null)
                {
                    AddMinionObject(minion);
                    if (!minion.IsAlly)
                        AllEnemyMinionsObj.Add(minion);
                }
            }
            public static List<AIBaseClient> GetTrinket(Vector3 from, float range = float.MaxValue)
            {
                return
                    AllyEyeList.Where(minion => CanReturn(minion, @from, range)).ToList();
            }
            public static List<AIBaseClient> GetJungles(Vector3 from, float range = float.MaxValue)
            {
                return
                    MinionsListJungle.Where(minion => CanReturn(minion, @from, range))
                        .OrderByDescending(minion => minion.MaxHealth)
                        .ToList();
            }
            public static List<AIBaseClient> GetMinions(Vector3 from, float range = float.MaxValue,
            MinionTeam team = MinionTeam.Enemy)
            {
                if (team == MinionTeam.Enemy)
                {
                    return MinionsListEnemy.FindAll(minion => CanReturn(minion, from, range));
                }
                if (team == MinionTeam.Ally)
                {
                    return MinionsListAlly.FindAll(minion => CanReturn(minion, @from, range));
                }
                return AllEnemyMinionsObj.FindAll(minion => CanReturn(minion, @from, range));
            }
            private static void AddMinionObject(AIMinionClient minion)
            {
                if (minion.MaxHealth >= 225)
                {
                    //野怪单位
                    if (minion.Team == GameObjectTeam.Neutral)
                    {
                        MinionsListJungle.Add(minion);
                    }
                    else
                    {
                        if (minion.Team == GameObjectTeam.Unknown)
                        {
                            return;
                        }
                            
                        if (minion.Team != Player.Team)
                        {
                            MinionsListEnemy.Add(minion);
                        }
                           
                        else if (minion.Team == Player.Team)
                        {
                            MinionsListAlly.Add(minion);
                        }
                    }
                }
                if((minion.MaxHealth == 3 || minion.MaxHealth == 4))
                {
                    AllyEyeList.Add(minion);
                }
            }
            private static bool IsValidMinion(AIBaseClient minion)
            {
                if (minion == null || !minion.IsValid || minion.IsDead)
                    return false;
                return true;
            }
            private static bool CanReturn(AIBaseClient minion, Vector3 from, float range)
            {
                if (minion != null && minion.IsValid && !minion.IsDead && minion.IsVisible && minion.IsTargetable)
                {
                    if (range == float.MaxValue)
                        return true;
                    if (range == 0)
                    {
                        if (minion.InAutoAttackRange())
                            return true;
                        return false;
                    }
                    if (Vector2.DistanceSquared(@from.ToVector2(), minion.ServerPosition.ToVector2()) < range * range)
                        return true;
                    return false;
                }
                return false;
            }
        }      
        public class PlusRender
        {
            private static FontCache TextPen = null;
            private static LineCache LinePen = null;
            static PlusRender()
            {
                TextPen = TextRender.CreateFont(20);
                LinePen = LineRender.CreateLine(1, false, true);
            }
            public static void DrawText(String text, float posx, float posy, ColorBGRA color)
            {
                TextPen.Draw(text, new Vector2(posx, posy), color);
            }
            public static void DrawRect(int x, int y, int w , int h,ColorBGRA Color)
            {
                //var Rect = new Render.Rectangle(x, y, w, h, Color);
                //Rect.Draw();
                //Rect.Dispose();
            }
            public static void DrawLine(Vector2 start, Vector2 end, int width, ColorBGRA color)
            {
                LinePen.Draw(start,end,color,width);
            }
            public static void DrawCircle(Vector3 pos,float radius,ColorBGRA color)
            {
                CircleRender.Draw(pos, radius, color);
            }
            public static List<Color> GetSingleColorList(System.Drawing.Color srcColor, System.Drawing.Color desColor, int count)
            {
                List<Color> colorFactorList = new List<Color>();
                int redSpan = desColor.R - srcColor.R;
                int greenSpan = desColor.G - srcColor.G;
                int blueSpan = desColor.B - srcColor.B;
                for (int i = 0; i < count; i++)
                {
                    Color color = new Color(
                        srcColor.R + (int)((double)i / count * redSpan),
                        srcColor.G + (int)((double)i / count * greenSpan),
                        srcColor.B + (int)((double)i / count * blueSpan)
                    );
                    colorFactorList.Add(color);
                }
                return colorFactorList;
            }
            /// <summary>
            /// 获取从红到紫的颜色段的颜色集合
            /// </summary>
            /// <param name="totalCount">分度数</param>
            /// <param name="redToPurple">是否从红到紫色渐变</param>
            /// <returns>返回颜色集合</returns>
            public static List<Color> GetFullColorList(int totalCount, bool redToPurple = true)
            {
                List<Color> colorList = new List<Color>();
                if (totalCount > 0)
                {
                    if (redToPurple)
                    {
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Red, System.Drawing.Color.Yellow, totalCount / 5 + (totalCount % 5 > 0 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Yellow, System.Drawing.Color.Lime, totalCount / 5 + (totalCount % 5 > 1 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Lime, System.Drawing.Color.Cyan, totalCount / 5 + (totalCount % 5 > 2 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Cyan, System.Drawing.Color.Blue, totalCount / 5 + (totalCount % 5 > 3 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Blue, System.Drawing.Color.Magenta, totalCount / 5 + (totalCount % 5 > 4 ? 1 : 0)));
                    }
                    else
                    {
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Magenta, System.Drawing.Color.Blue, totalCount / 5 + (totalCount % 5 > 0 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Blue, System.Drawing.Color.Cyan, totalCount / 5 + (totalCount % 5 > 1 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Cyan, System.Drawing.Color.Lime, totalCount / 5 + (totalCount % 5 > 2 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Lime, System.Drawing.Color.Yellow, totalCount / 5 + (totalCount % 5 > 3 ? 1 : 0)));
                        colorList.AddRange(GetSingleColorList(System.Drawing.Color.Yellow, System.Drawing.Color.Red, totalCount / 5 + (totalCount % 5 > 4 ? 1 : 0)));
                    }
                }
                return colorList;
            }
        }
    }
    public static class FixedMethods
    {
        private static readonly HashSet<BuffType> BlockedMovementBuffTypes = new HashSet<BuffType>
        {
            BuffType.Knockup,
            BuffType.Knockback,
            BuffType.Charm,
            BuffType.Fear,
            BuffType.Flee,
            BuffType.Taunt,
            BuffType.Snare,
            BuffType.Stun,
            BuffType.Suppression,
        };

        public static int GetStunDuration(this AIBaseClient target)
        {
            return (int)(target.Buffs.Where(b => b.IsActive && Game.Time < b.EndTime &&
                                                 (b.Type == BuffType.Charm ||
                                                  b.Type == BuffType.Knockback ||
                                                  b.Type == BuffType.Stun ||
                                                  b.Type == BuffType.Suppression ||
                                                  b.Type == BuffType.Snare)).Aggregate(0f, (current, buff) => Math.Max(current, buff.EndTime)) - Game.Time) * 1000;
        }
        public static float GetMovementBlockedDebuffDuration(this AIBaseClient target)
        {
            return
                target.Buffs.Where(b => b.IsActive && Game.Time < b.EndTime && BlockedMovementBuffTypes.Contains(b.Type))
                    .Aggregate(0f, (current, buff) => Math.Max(current, buff.EndTime)) -
                Game.Time;
        }
        public static float Distance(this Vector2 point, Vector2 segmentStart, Vector2 segmentEnd, bool onlyIfOnSegment = false, bool squared = false)
        {
            var objects = point.ProjectOn(segmentStart, segmentEnd);

            if (objects.IsOnSegment || onlyIfOnSegment == false)
            {
                return squared
                    ? Vector2.DistanceSquared(objects.SegmentPoint, point)
                    : Vector2.Distance(objects.SegmentPoint, point);
            }
            return float.MaxValue;
        }
        public static int CountEnemyHerosInRangeFix(this AIBaseClient obj, float Range)
        {
            return obj.ServerPosition.CountEnemyHerosInRangeFix(Range);
        }
        public static float GetRealHeath(this AIBaseClient Unit,DamageType Type)
        {
            float ExtraShield = 0f;
            switch (Type)
            {
                case DamageType.Physical:
                    ExtraShield = Unit.PhysicalShield;
                    break;
                case DamageType.Magical:
                    ExtraShield = Unit.MagicalShield;
                    break;
                case DamageType.Mixed:
                    ExtraShield = Unit.PhysicalShield + Unit.MagicalShield;
                    break;
                case DamageType.True:
                    ExtraShield = 0f;
                    break;
                default:
                    ExtraShield = 0f;
                    break;
            }
            return Unit.Health + ExtraShield + (Unit.HPRegenRate * 2f) + Unit.AllShield;
        }
        public static int CountEnemyHerosInRangeFix(this Vector3 obj, float Range)
        {
            return Base.Cache.EnemyHeroes.Count(x => x.IsValidTarget(Range, true, obj) && x.IsTargetable);
        }
        public static int CountEnemyHerosInRangeFix(this Vector2 obj, float Range)
        {
            return obj.ToVector3().CountEnemyHerosInRangeFix(Range);
        }
        public static int CountAllysHerosInRangeFix(this AIBaseClient obj, float Range)
        {
            return obj.ServerPosition.CountAllysHerosInRangeFix(Range);
        }
        public static int CountAllysHerosInRangeFix(this Vector3 obj, float Range)
        {
            return Base.Cache.AlliesHeroes.Count(x => x.IsValidTarget(Range, false, obj));
        }
        public static int CountAllysHerosInRangeFix(this Vector2 obj, float Range)
        {
            return obj.ToVector3().CountAllysHerosInRangeFix(Range);
        }
        public static int CountMinionsInRangeFix(this Vector3 pos, float range)
        {
            return Base.Cache.GetMinions(pos, range).Count;
        }
        public static bool IsCollisionable(this Vector3 pos)
        {
            return NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall) || Orbwalker.ActiveMode == OrbwalkerMode.Combo && NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Building);
        }
        public static bool NewIsValidTarget(
            this AttackableUnit unit,
            float range = float.MaxValue,
            bool checkTeam = true,
            Vector3 from = new Vector3())
        {
            
            if (unit == null || !unit.IsValid || unit.IsDead || !unit.IsVisible)
            {
                return false;
            }
            if (checkTeam && GameObjects.Player.Team == unit.Team)
            {
                return false;
            }

            var @base = unit as AIBaseClient;


            if (@base == null)
            {
                return false;
            }

            if (!@base.HasBuff("zhonyasringshield") && !@base.IsTargetableToTeam(GameObjects.Player.Team) && !@base.IsHPBarRendered)
            {
                return false;
            }

             



            var unitPosition = @base?.ServerPosition ?? unit.Position;

            return @from.IsValid()
                       ? @from.DistanceSquared(unitPosition) < range * range
                       : GameObjects.Player.DistanceSquared(unitPosition) < range * range;
        }
    }
}
