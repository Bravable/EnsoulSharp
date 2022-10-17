using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;
using ImpulseAIO.Common;
namespace ImpulseAIO.Champion.Kalista
{
    internal class Kalista : Base
    {
        private static Spell Q, NonCollisionQ,W, E, R;
        private static int AATime;
        private static bool FlyHack => ChampionMenu["Combo"]["FlyHack"].GetValue<MenuBool>().Enabled;
        private static int ComboUseQ => ChampionMenu["Combo"]["QMode"].GetValue<MenuList>().Index;
        private static bool ComboUseAttackW => ChampionMenu["Combo"]["AttackW"].GetValue<MenuBool>().Enabled;
        private static int ComboGap => ChampionMenu["Combo"]["CGap"].GetValue<MenuList>().Index;
        private static int HarassMana => ChampionMenu["Combo"]["Mana"].GetValue<MenuSlider>().Value;

        private static int EMode => ChampionMenu["Eset"]["EMode"].GetValue<MenuList>().Index;
        private static bool AutobigE => ChampionMenu["Eset"]["autobigE"].GetValue<MenuBool>().Enabled;
        private static bool harassPlus => ChampionMenu["Eset"]["harassPlus"].GetValue<MenuBool>().Enabled;
        private static bool autodeadE => ChampionMenu["Eset"]["autodeadE"].GetValue<MenuBool>().Enabled;
        private static int WaveUseQMode => ChampionMenu["LaneClear"]["QMode"].GetValue<MenuList>().Index;
        private static MenuSliderButton MinE => ChampionMenu["LaneClear"]["MinE"].GetValue<MenuSliderButton>();
        private static bool secureE => ChampionMenu["LaneClear"]["secureE"].GetValue<MenuBool>().Enabled;
        private static int WaveMana => ChampionMenu["LaneClear"]["Mana"].GetValue<MenuSlider>().Value;
        public static bool DrawQ => ChampionMenu["Draw"]["DrawQ"].GetValue<MenuBool>().Enabled;
        public static bool DrawE => ChampionMenu["Draw"]["DrawE"].GetValue<MenuBool>().Enabled;
        public static bool DrawR => ChampionMenu["Draw"]["DrawR"].GetValue<MenuBool>().Enabled;
        public static bool DrawEDamage => ChampionMenu["Draw"]["DrawEPlus"].GetValue<MenuBool>().Enabled;
        private static int RMode => ChampionMenu["Rset"]["RMode"].GetValue<MenuList>().Index;
        public Kalista()
        {
            Q = new Spell(SpellSlot.Q, 1140f);
            Q.SetSkillshot(0.25f, 40f, 2400f, true, SpellType.Line);
            NonCollisionQ = new Spell(SpellSlot.Q, 1140f);
            NonCollisionQ.SetSkillshot(0.25f, 40f, 2400f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 5000f);
            E = new Spell(SpellSlot.E, 1000f);
            R = new Spell(SpellSlot.R, 1100f);
            Q.DamageType = E.DamageType = DamageType.Physical;
            OnMenuLoad();
            Game.OnUpdate += GameOnUpdate;
            RendCache.Init();
            SoulBoundSaver.Initialize();
            Orbwalker.OnAfterAttack += (s, g) =>
            {
                AATime = Variables.GameTimeTickCount;
            };
            AIBaseClient.OnPlayAnimation += (s, g) =>
            {
                if (s.IsMe)
                {
                    if (g.Animation.Equals("Spell3"))
                    {
                        Game.SendEmote(EmoteId.Dance);
                        g.Process = false;
                    }
                }
            };
            Orbwalker.OnNonKillableMinion += (s, g) => {
                if (!secureE || !E.IsReady()
                || Player.HasBuff("summonerexhaust")
                || Player.Mana - 40 < 40)
                    return;

                var Units = g.Target as AIBaseClient;
                if (Units.IsValidTarget() && RendCache.IsUnitRendKillable(Units))
                {
                    if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                    {
                        E.Cast();
                    } 
                }
            };
            Render.OnEndScene += Drawing_OnDraw;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Kalista));
            var Combo = ChampionMenu.Add(new Menu("Combo", Program.Chinese ? "连招&骚扰设置" : "Combo && Harass"));
            {
                Combo.Add(new MenuBool("FlyHack", "启用 跳跃漏洞"));
                Combo.Add(new MenuList("QMode", "Use Q", new string[] { "仅连招", "仅骚扰", "二者都", "从不" }));
                Combo.Add(new MenuBool("AttackW", Program.Chinese ? "优先攻击誓约者目标" : "Attack W Passive Target"));
                Combo.Add(new MenuList("CGap", Program.Chinese ? "普攻小兵以此靠近敌方英雄" : "Attack Minion To Gap", new string[] { "Only Combo", "Only Harass", "all", "Disable" }));
                Combo.Add(new MenuSlider("Mana", Program.Chinese ? "蓝量 <= X%时不骚扰" : "Don't Harass if Mana <= X%",40,0,100));
            }
            var Eset = ChampionMenu.Add(new Menu("Eset", Program.Chinese ? "撕裂":"E"));
            {
                Eset.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Rend));
                Eset.Add(new MenuList("EMode", "Use E Mode", new string[] { "Only Combo", "Always", "Disable" }, 1));
                Eset.Add(new MenuBool("harassPlus", Program.Chinese ? "自动使用E当能杀死小兵并且敌人至少有一层E" : "Auto Kill Minion && Any Enemy Have E Buff"));
                Eset.Add(new MenuBool("autobigE", Program.Chinese ? "抢龙" : "Kill Dragon"));
                Eset.Add(new MenuBool("autodeadE", Program.Chinese ? "濒死时释放E" : "If Will Die. Force E"));
            }
            var Rset = ChampionMenu.Add(new Menu("Rset", "命运的召唤"));
            {
                Rset.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.FateCall));
                Rset.Add(new MenuList("RMode", "Use R", new string[] { "Only Combo", "Always", "Disable" }));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", Program.Chinese ? "清线&&清野设置" : "LaneClear / JungleClear"));
            {
                LaneClear.Add(new MenuList("QMode", "Use Q", new string[] { "Only LaneClear", "Only JungleClear", "All", "Disable" }));
                LaneClear.Add(new MenuSliderButton("MinE", Program.Chinese ? "使用 E 至少击杀 X 个小兵" : "Use Kill min minion Count", 1, 1, 5));
                LaneClear.Add(new MenuBool("secureE", Program.Chinese ? "使用E杀死无法普攻状态下的小兵" : "Auto E Kill OrbNonKillable Minion"));
                LaneClear.Add(new MenuSlider("Mana", Program.Chinese ? "蓝量 <= X%时不技能清线野" : "Don't Lane/Jung if Mana <= X%", 40, 0, 100));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw", true));
            {
                Draw.Add(new MenuBool("DrawQ", "Draw Q "));
                Draw.Add(new MenuBool("DrawE", "Draw E "));
                Draw.Add(new MenuBool("DrawR", "Draw R "));
                Draw.Add(new MenuBool("DrawEPlus", "Draw E Damage"));
            }
        }
        public static float LastAATick;

        private void FlyHackLogic()
        {
            if (!FlyHack) 
                return;
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Player.AttackSpeedMod > 2.0)
            {
                var target = Orbwalker.GetTarget();
                if (target != null)
                {
                    if (Variables.GameTimeTickCount - LastAATick <= 100 + Game.Ping)
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    }
                    if (Variables.GameTimeTickCount - LastAATick >= Game.Ping)
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        LastAATick = Variables.GameTimeTickCount;
                    }
                }
            }
        }
        private void GameOnUpdate(EventArgs args)
        {
            FlyHackLogic();
            Elogic();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                case OrbwalkerMode.Harass:
                    Combo();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void Drawing_OnDraw(EventArgs args)
        {
            if (DrawEDamage)
            {
                foreach (var enemy in Cache.EnemyHeroes.Where(e => e.IsValidTarget(E.Range + 500)))
                {
                    var dmg = RendCache.GetActualDamage(enemy);
                    if (dmg <= 0f)
                        continue;
                    int HpBarLeftX = (int)enemy.HPBarPosition.X - 45;
                    int HpBarLeftY = (int)enemy.HPBarPosition.Y - 25;
                    int HpBarHeight = 13;
                    int HPBarTotalLength = ((int)enemy.HPBarPosition.X - HpBarLeftX) * 2 + 16;
                    var DamageCeiling = dmg / enemy.GetRealHeath(DamageType.Physical);
                    DamageCeiling = Math.Min(DamageCeiling, 1);
                    int FixedHPBarLength = (int)(DamageCeiling * HPBarTotalLength);
                    PlusRender.DrawRect(HpBarLeftX, HpBarLeftY, FixedHPBarLength, HpBarHeight, new Color((int)Color.Green.R, (int)Color.Green.G, (int)Color.Green.B, 144));
                }
            }
            if (DrawQ && Q.IsReady())
                PlusRender.DrawCircle(Player.Position, Q.Range, Color.White);
            if (DrawE && E.IsReady())
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Red);
            if (DrawR && R.IsReady())
                PlusRender.DrawCircle(Player.Position, R.Range, Color.Aqua);
        }
        private void LaneClear()
        {
            if (!Enable_laneclear) return;

            if (Q.IsReady() && WaveUseQMode != 3 && Player.ManaPercent > WaveMana)
            {
                if (WaveUseQMode == 1 || WaveUseQMode == 2)
                {
                    var bestJungle = Cache.GetJungles(Player.ServerPosition, Q.Range).MaxOrDefault(x => x.GetJungleType());
                    if (bestJungle.IsValidTarget())
                    {
                        var QPred = Q.GetPrediction(bestJungle, false, -1, new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.YasuoWall });
                        if (QPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(QPred.CastPosition);
                            return;
                        }
                        
                        if (GetQCollision(Player.ServerPosition, bestJungle.ServerPosition) > 0)
                        {
                            var col = QPred.CollisionObjects;
                            if (col.All(x => Q.GetDamage(x) > x.Health)) //可以被q穿死时
                            {
                                var NonColiPred = NonCollisionQ.GetPrediction(bestJungle, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                                if (NonColiPred.Hitchance >= HitChance.High)
                                {
                                    NonCollisionQ.Cast(NonColiPred.CastPosition);
                                    return;
                                }
                            }
                        }
                    }
                }
                if (WaveUseQMode == 0 || WaveUseQMode == 2)
                {
                    //Only LaneClear
                    var countMinion = 0;
                    AIBaseClient bestMinion = null;

                    foreach (var minion in Cache.GetMinions(Player.ServerPosition, Q.Range).Where(e => Q.GetDamage(e) > e.Health))
                    {
                        var poutput = Q.GetPrediction(minion);
                        var col = poutput.CollisionObjects;
                        var kill = 0;

                        if (col.Count == 0)
                        {
                            continue;
                        }

                        foreach (var colobj in col)
                        {
                            if (Q.GetDamage(colobj) > colobj.Health)
                            {
                                kill++;
                            }
                            else
                            {
                                kill = 0;
                                break;
                            }
                        }

                        if (kill > 0 && (countMinion == 0 || countMinion < kill + 1))
                        {
                            countMinion = kill + 1;
                            bestMinion = minion;
                        }
                    }

                    if (bestMinion != null && countMinion >= 1)
                    {
                        NonCollisionQ.Cast(bestMinion);
                    }
                }
            }

            if(E.IsReady())
            {
                if (MinE.Enabled && RendCache.RendMinions.Count(o => E.IsInRange(o) && o.IsMinion() && RendCache.IsUnitRendKillable(o)) >= MinE.Value && E.Cast())
                {
                    return;
                }
            }

            if (E.IsReady())
            {
                var JungleArray = RendCache.RendMinions.ToList();
                JungleArray.RemoveAll(x => x.IsMinion() || (x.IsJungle() && x.GetJungleType() >= JungleType.Legendary));


                if(JungleArray.Any(x => x.GetJungleType() >= JungleType.Large && RendCache.IsUnitRendKillable(x)) ||
                   JungleArray.Count(x => x.GetJungleType() < JungleType.Large && RendCache.IsUnitRendKillable(x)) >= 2)
                {
                    E.Cast();
                }
            }
        }
        private int GetQCollision(Vector3 from, Vector3 to)
        {
            var List = new List<Vector2>() { to.ToVector2() };
            var WCollisionList = Q.GetCollision(from.ToVector2(), List);
            return WCollisionList.Count;
        }
        private void Elogic()
        {
            if (!E.IsReady())
                return;

            if ((EMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || EMode == 1)
            {
                if(RendCache.RendHeroes.Any(o => RendCache.IsUnitRendKillable(o)))
                {
                    E.Cast();
                }
                if (AutobigE)
                {
                    if (RendCache.RendMinions.Where(o => RendCache.IsUnitRendKillable(o)).Any(m =>
                    {
                        return m.IsJungle() && m.GetJungleType() >= JungleType.Legendary;

                    }) && E.Cast())
                    {
                        return;
                    }
                }
                if (harassPlus)
                {
                    if (RendCache.RendHeroes.Any(o => E.IsInRange(o)) && RendCache.RendMinions.Any(y => E.IsInRange(y) && RendCache.IsUnitRendKillable(y)) && E.Cast())
                    {
                        return;
                    }
                }
                if (autodeadE)
                {
                    if ((Player.HealthPercent <= 7 || HealthPrediction.GetPrediction(Player,250) <= 0) && RendCache.RendHeroes.Any(o => E.IsInRange(o)) && E.Cast())
                    {
                        return;
                    }
                }
                
            }
        }
        private void Combo()
        {
            if ((ComboUseQ == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || (ComboUseQ == 1 && Orbwalker.ActiveMode == OrbwalkerMode.Harass && Player.ManaPercent > HarassMana) || (ComboUseQ == 2 && (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)))
            {
                var Target = GetBestTarget(Q.Range);
                if (Target.IsValidTarget())
                {
                    if (!Player.IsWindingUp && !Player.IsDashing())
                    {
                        var QPred = Q.GetPrediction(Target, false, -1, new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.YasuoWall });
                        if (QPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(QPred.CastPosition);
                            return;
                        }
                        if(GetQCollision(Player.ServerPosition, Target.ServerPosition) > 0)
                        {
                            var col = QPred.CollisionObjects;
                            if (col.All(x => Q.GetDamage(x) > x.Health)) //可以被q穿死时
                            {
                                var NonColiPred = NonCollisionQ.GetPrediction(Target, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                                if (NonColiPred.Hitchance >= HitChance.High)
                                {
                                    NonCollisionQ.Cast(NonColiPred.CastPosition);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if ((ComboGap == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || (ComboGap == 1 && Orbwalker.ActiveMode == OrbwalkerMode.Harass) || (ComboGap == 2 && (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)))
            {
                var AttackObj = Orbwalker.GetTarget();
                if (AttackObj == null && Player.CountEnemyHerosInRangeFix(2000) != 0)
                {
                    var minions = Cache.GetMinions(Player.ServerPosition, Player.GetRealAutoAttackRange()).MinOrDefault(x => x.Health);
                    if (minions != null && Orbwalker.CanAttack())
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, minions);
                    }
                }
            }
        }
        private AIHeroClient GetBestTarget(float Range)
        {
            if (ComboUseAttackW)
            {
                var Units = TargetSelector.GetTargets(Range, DamageType.Physical).Where(x => x.HasBuff("kalistacoopstrikemarkally")).FirstOrDefault();
                if (Units == null)
                {
                    
                    return TargetSelector.GetTarget(Range, DamageType.Physical);
                }
                return Units;
            }
            return TargetSelector.GetTarget(Range, DamageType.Physical);
        }
        private class RendCache
        {
            public static List<AIBaseClient> RendEntities = new List<AIBaseClient>();
            public static List<AIMinionClient> RendMinions = new List<AIMinionClient>();
            public static List<AIHeroClient> RendHeroes = new List<AIHeroClient>();
            private static readonly float[] RawRendDamage = { 20, 30, 40, 50, 60 };
            private static readonly float[] RawRendDamageMultiplier = { 0.7f, 0.7f, 0.7f, 0.7f, 0.7f };
            private static readonly float[] RawRendDamagePerSpear = { 10, 16, 22, 28, 34 };
            private static readonly float[] RawRendDamagePerSpearMultiplier = { 0.232f, 0.2755f, 0.319f, 0.3625f, 0.406f };
            static RendCache()
            {
                foreach (var minion in GameObjects.Get<AIBaseClient>().Where(minion => minion.IsEnemy && minion.IsValid && HasRendBuff(minion)))
                {
                    AddMinionObject(minion);
                }
                AIBaseClient.OnBuffAdd += OnBuffAdd;
                Game.OnUpdate += Game_OnUpdate;
            }
            public static void Init()
            {

            }
            private static void Game_OnUpdate(EventArgs args)
            {
                RendEntities.RemoveAll(minion => !IsValidRend(minion));
                RendMinions.RemoveAll(minion => !IsValidRend(minion));
                RendHeroes.RemoveAll(minion => !IsValidRend(minion));
            }
            private static bool IsValidRend(AIBaseClient minion)
            {
                if (minion == null || !minion.IsValid || minion.IsDead || !HasRendBuff(minion))
                    return false;
                return true;
            }
            private static bool HasRendBuff(AIBaseClient target)
            {
                return target.Buffs.Find(b => b.IsValid && b.Name.Equals("kalistaexpungemarker")) != null;
            }
            private static BuffInstance GetRendBuffInstance(AIBaseClient target)
            {
                return target.Buffs.Find(b => b.IsValid && b.Name.Equals("kalistaexpungemarker"));
            }
            private static void OnBuffAdd(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
            {
                if (sender.IsValidTarget())
                {
                    if (args.Buff.Name.Equals("kalistaexpungemarker"))
                    {
                        AddMinionObject(sender);
                    }
                }
            }
            private static void AddMinionObject(AIBaseClient minion)
            {
                if (minion == null) return;

                RendEntities.Add(minion);

                GameObjectType Type = minion.Type;
                if(Type == GameObjectType.AIHeroClient)
                {
                    var HeroObj = minion as AIHeroClient;
                    if(HeroObj != null)
                    {
                        RendHeroes.Add(HeroObj);
                    }
                    return;
                }
                if(Type == GameObjectType.AIMinionClient)
                {
                    var MinionObj = minion as AIMinionClient;
                    if(MinionObj != null)
                    {
                        RendMinions.Add(MinionObj);
                    }
                }
            }
            private static float GetRawRendDamage(AIBaseClient target)
            {
                var stacks = (HasRendBuff(target) ? GetRendBuffInstance(target).Count : 0) - 1;
                if (stacks > -1)
                {
                    var index = E.Level - 1;
                    return RawRendDamage[index] + stacks * RawRendDamagePerSpear[index] +
                           Player.TotalAttackDamage * (RawRendDamageMultiplier[index] + stacks * RawRendDamagePerSpearMultiplier[index]);
                }

                return 0;
            }
            public static bool IsUnitRendKillable(AIBaseClient target)
            {
                if (target == null
                    || !target.IsValidTarget(E.Range + 200)
                    || !HasRendBuff(target)
                    || target.GetRealHeath(DamageType.Physical) <= 0
                    || !E.IsReady())
                {
                    return false;
                }
                var hero = target as AIHeroClient;
                if (hero != null)
                {
                    //存在无敌Buff 或 技能护盾时
                    if (hero.IsInvulnerable || hero.HaveSpellShield())
                    {
                        return false;
                    }

                    if (hero.CharacterName.Equals("Blitzcrank"))
                    {
                        if (!hero.HasBuff("BlitzcrankManaBarrierCD") && !hero.HasBuff("ManaBarrier"))
                        {
                            return GetActualDamage(target) > (target.GetRealHeath(DamageType.Physical) + (hero.Mana / 2));
                        }

                        if (hero.HasBuff("ManaBarrier") && !(hero.AllShield > 0))
                        {
                            return false;
                        }
                    }
                }
                return GetActualDamage(target) > target.GetRealHeath(DamageType.Physical);
            }
            private static int GetKillableDragon()
            {
                return Player.Buffs.Count(x => x.Name.ToLower().StartsWith("srx_dragon"));
            }
            public static float GetActualDamage(AIBaseClient target)
            {
                if (!E.IsReady() || !HasRendBuff(target))
                    return 0f;

                var damage = GetRawRendDamage(target);

                if (target.IsJungle() && target.GetJungleType() >= JungleType.Legendary)
                {
                    damage = damage * 0.5f;
                }

                if (target.Name.Contains("Dragon"))
                {
                    damage = GetKillableDragon() != 0
                        ? damage * (1 - (.07f * GetKillableDragon()))
                        : damage;
                }

                if (Player.HasBuff("summonerexhaust"))
                {
                    damage = damage * 0.6f;
                }
                if (target.HasBuff("FerociousHowl"))
                {
                    damage = damage * 0.7f;
                }
                return (float)Player.CalculatePhysicalDamage(target, damage *
                       (Player.HasBuff("summonerexhaust") ? 0.6f : 1));
            }
        }
        private class SoulBoundSaver
        {
            public static AIHeroClient SoulBound { get; private set; }

            private static readonly Dictionary<float, float> IncDamage = new Dictionary<float, float>();
            private static readonly Dictionary<float, float> InstDamage = new Dictionary<float, float>();
            public static float IncomingDamage
            {
                get { return IncDamage.Sum(e => e.Value) + InstDamage.Sum(e => e.Value); }
            }

            public static void Initialize()
            {
                // Listen to related events
                Game.OnUpdate += OnTick;
                AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            }

            private static void OnTick(EventArgs args)
            {
                // SoulBound is not found yet!
                if (SoulBound == null)
                {
                    SoulBound = Cache.EnemyHeroes.Find(h => h.IsAlly && !h.IsMe && h.Buffs.Any(b => b.Name == "kalistacoopstrikeally"));
                }

                else if (((RMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || RMode == 1) && R.IsReady())
                {
                    // Ult casting
                    if (SoulBound.HealthPercent < 5 && SoulBound.CountEnemyHeroesInRange(500) > 0 ||
                        IncomingDamage > SoulBound.Health)
                        R.Cast();
                }
                // Check spell arrival
                foreach (var entry in IncDamage.Where(entry => entry.Key < Game.Time).ToArray())
                {
                    IncDamage.Remove(entry.Key);
                }

                // Instant damage removal
                foreach (var entry in InstDamage.Where(entry => entry.Key < Game.Time).ToArray())
                {
                    InstDamage.Remove(entry.Key);
                }
            }

            private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                if (sender.IsEnemy)
                {
                    // Calculations to save your souldbound
                    if (SoulBound != null && ((RMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || RMode == 1))
                    {
                        // Auto attacks
                        if (Orbwalker.IsAutoAttack(args.SData.Name) && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                        {
                            // Calculate arrival time and damage
                            IncDamage[SoulBound.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed + Game.Time] = (float)sender.GetAutoAttackDamage(SoulBound);
                        }
                        // Sender is a hero
                        else
                        {

                            var attacker = sender as AIHeroClient;
                            if (attacker != null)
                            {
                                var slot = attacker.GetSpellSlotFromName(args.SData.Name);

                                if (slot != SpellSlot.Unknown)
                                {
                                    if (slot == attacker.GetSpellSlotFromName("SummonerDot") && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                                    {
                                        // Ingite damage (dangerous)
                                        InstDamage[Game.Time + 2] = (float)attacker.GetSummonerSpellDamage(SoulBound, SummonerSpell.Ignite);
                                    }
                                    else
                                    {
                                        switch (slot)
                                        {
                                            case SpellSlot.Q:
                                            case SpellSlot.W:
                                            case SpellSlot.E:
                                            case SpellSlot.R:

                                                if ((args.Target != null && args.Target.NetworkId == SoulBound.NetworkId) || args.End.Distance(SoulBound.ServerPosition) < Math.Pow(args.SData.LineWidth, 2))
                                                {
                                                    // Instant damage to target
                                                    InstDamage[Game.Time + 2] = (float)attacker.GetSpellDamage(SoulBound, slot);
                                                }

                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
