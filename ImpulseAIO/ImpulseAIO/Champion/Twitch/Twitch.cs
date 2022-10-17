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
namespace ImpulseAIO.Champion.Twitch
{
    internal class Twitch : Base
    {
        private static Spell Q, W, E, R;
        #region 菜单选项
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static int ComboUseQMinRange => ChampionMenu["Combo"]["CQSearchRange"].GetValue<MenuSlider>().Value;
        private static int ComboUseQCountEnemy => ChampionMenu["Combo"]["CQEnemyCount"].GetValue<MenuSlider>().Value;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseWCheck => ChampionMenu["Combo"]["CWCheck"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseWAntiFlee => ChampionMenu["Combo"]["CWAntiFlee"].GetValue<MenuBool>().Enabled;

        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseEOnlyKill => ChampionMenu["Combo"]["CEOnlyKill"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseEOutRange => ChampionMenu["Combo"]["CEOutRange"].GetValue<MenuBool>().Enabled;
        private static int ComboUseEOutRangeStack => ChampionMenu["Combo"]["CEOutRangeStack"].GetValue<MenuSlider>().Value;
        private static bool ComboUseR => ChampionMenu["Combo"]["CR"].GetValue<MenuBool>().Enabled;
        private static int ComboUseRCount => ChampionMenu["Combo"]["CRCountEnemy"].GetValue<MenuSlider>().Value;
        private static bool ComboUseROnlyLine => ChampionMenu["Combo"]["CROnlyLine"].GetValue<MenuBool>().Enabled;

        private static bool HarassUseW => ChampionMenu["Harass"]["HW"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseWCheck => ChampionMenu["Harass"]["HWCheck"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseE => ChampionMenu["Harass"]["HE"].GetValue<MenuBool>().Enabled;
        private static int HarassUseEStack => ChampionMenu["Harass"]["HEStack"].GetValue<MenuSlider>().Value;
        private static bool HarassUseEOutRange => ChampionMenu["Harass"]["HEOutRange"].GetValue<MenuBool>().Enabled;
        private static int HarassEOutRangeStack => ChampionMenu["Harass"]["HEOutRangeStack"].GetValue<MenuSlider>().Value;
        private static int HarassMana => ChampionMenu["Harass"]["HarassMana"].GetValue<MenuSlider>().Value;

        private static bool LaneClearUseW => ChampionMenu["LaneClear"]["LW"].GetValue<MenuBool>().Enabled;
        private static bool LaneClearUseE => ChampionMenu["LaneClear"]["LE"].GetValue<MenuBool>().Enabled;
        private static int LaneClearUseWMinKC => ChampionMenu["LaneClear"]["LminWKC"].GetValue<MenuSlider>().Value;
        private static int LaneClearUseEMinKC => ChampionMenu["LaneClear"]["LminEKC"].GetValue<MenuSlider>().Value;
        private static int LaneClearMana => ChampionMenu["LaneClear"]["LMana"].GetValue<MenuSlider>().Value;

        private static bool JungleClearUseW => ChampionMenu["JungleClear"]["JW"].GetValue<MenuBool>().Enabled;
        private static bool JungleClearUseE => ChampionMenu["JungleClear"]["JE"].GetValue<MenuBool>().Enabled;
        private static bool JungleWCheck => ChampionMenu["JungleClear"]["JWCheck"].GetValue<MenuBool>().Enabled;
        private static int JungleClearUseWMinKC => ChampionMenu["JungleClear"]["JminWKC"].GetValue<MenuSlider>().Value;
        private static int JungleClearUseEMinKC => ChampionMenu["JungleClear"]["JminEKC"].GetValue<MenuSlider>().Value;

        private static bool Exploit => ChampionMenu["Exploit"].GetValue<MenuBool>().Enabled;
        private static bool DrawWRange => ChampionMenu["Drawing"]["DW"].GetValue<MenuBool>().Enabled;
        private static bool DrawERange => ChampionMenu["Drawing"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool DrawRRange => ChampionMenu["Drawing"]["DR"].GetValue<MenuBool>().Enabled;

        #endregion

        public static bool RActive
        {
            get { return Player.HasBuff("TwitchFullAutomatic"); }
        }
        public static bool QActive
        {
            get { return Player.HasBuff("TwitchHideInShadows"); }
        }
        public Twitch()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 100f, 1400f, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, 850f);
            W.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            OnMenuLoad();
            Game.OnUpdate += Game_OnUpdate;
            Render.OnEndScene += OnDraw;
            AIBaseClient.OnProcessSpellCast += Exploit_BasicAttack;
            AIBaseClient.OnProcessSpellCast += Exploit_OnProcessSpellCast;
            DamageIndicator.DamageToUnit += TwitchCache.GetRealVenomDamage;
            Prediction.SetPrediction("SDK Prediction");
            Game.Print("检测到图奇已加载 不要使用Q预判! 已切换为官方默认预判");
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Twitch));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo", true));
            {
                Combo.Add(new MenuBool("CQ", "Use Q",false));
                Combo.Add(new MenuSlider("CQSearchRange", Program.Chinese ? "->寻找敌人的范围" : "Find Enemy Range", 600, 0, 1800));
                Combo.Add(new MenuSlider("CQEnemyCount", Program.Chinese ? "->周围敌人 >= X时开启Q" : "Use Q if Count Enemy >= ", 3, 1, 5));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuBool("CWCheck", Program.Chinese ? "当目标有满层被动时不放W" : "Don't Cast W if Target has Max Passive Count"));
                Combo.Add(new MenuBool("CWAntiFlee", Program.Chinese ? "阻止目标逃跑即使目标被动已满" : "Force W if Target is Flee"));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuBool("CEOnlyKill", Program.Chinese ? "只有可以杀死的情况下使用E" : "Only Can Killable Use E"));
                Combo.Add(new MenuBool("CEOutRange", Program.Chinese ? "在目标离开E范围时释放E" : "Force E if Target escape E Range"));
                Combo.Add(new MenuSlider("CEOutRangeStack", Program.Chinese ? "目标离开E范围时身上的层数" : "Force Escape E if Target Passive Count >= ", 3, 1, 6));
                Combo.Add(new MenuBool("CR", "Use R",false));
                Combo.Add(new MenuSlider("CRCountEnemy", Program.Chinese ? "周围目标数>= X时释放R" : "Cast R When Count Enemy >= X", 2, 1, 5));
                Combo.Add(new MenuBool("CROnlyLine", Program.Chinese ? "仅当目标聚拢或在一条直线上时使用R" : "Only Line Rect"));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass", true));
            {
                Harass.Add(new MenuBool("HW", "Use W"));
                Harass.Add(new MenuBool("HWCheck", Program.Chinese ? "当目标有五层被动时不放W" : "Don't Cast W if Target has Max Passive Count"));
                Harass.Add(new MenuBool("HE", "Use E"));
                Harass.Add(new MenuSlider("HEStack", Program.Chinese ? "当目标身上存在X层被动时E" : "Csat E if Target Passive Count >= X", 5, 1, 6));
                Harass.Add(new MenuBool("HEOutRange", Program.Chinese ? "在目标离开E范围时释放E" : "Force E if Target escape E Range"));
                Harass.Add(new MenuSlider("HEOutRangeStack", Program.Chinese ? "目标离开E范围时身上的层数" : "Force Escape E if Target Passive Count >= ", 3, 1, 6));
                Harass.Add(new MenuSlider("HarassMana", Program.Chinese ? "当蓝量 <= X%时不骚扰" : "Don't Use Spell Harass if Mana <= X%",40,0,100));
            }
            var LanceClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LanceClear.Add(new MenuBool("LW", "Use W",false));
                LanceClear.Add(new MenuSlider("LminWKC", Program.Chinese ?  "W技能最少能感染 X 小兵时使用W" : "W min hitCount", 4, 1, 10));
                LanceClear.Add(new MenuBool("LE", "Use E"));
                LanceClear.Add(new MenuSlider("LminEKC", Program.Chinese ? "E技能最少能击杀 X 小兵时使用E" : "E min Killable Minions", 4, 1, 10));
                LanceClear.Add(new MenuSlider("LMana", Program.Chinese ? "当蓝量 < X 时不E清线野" : "Don't Use Spell LaneClear/JungleClear if Mana <= X%", 10, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JW", "Use W"));
                JungleClear.Add(new MenuBool("JWCheck", Program.Chinese ? "当目标有五层被动时不放W" : "Don't W if Target has 5 Passive"));
                JungleClear.Add(new MenuSlider("JminWKC", Program.Chinese ? "W技能最少能感染 X 野怪时使用W" : "W min hitCount Jungle", 2, 1, 10));

                JungleClear.Add(new MenuBool("JE", "Use E"));
                JungleClear.Add(new MenuSlider("JminEKC", Program.Chinese ? "E技能最少能击杀 X 野怪时使用E" : "E min Killable Jungle", 2, 1, 10));
            }
            var Drawing = ChampionMenu.Add(new Menu("Drawing", "Draw", true));
            {
                Drawing.Add(new MenuBool("DW", "W Range",false));
                Drawing.Add(new MenuBool("DE", "E Range",false));
                Drawing.Add(new MenuBool("DR", "RA Range"));
            }
            ChampionMenu.Add(new MenuBool("Exploit", Program.Chinese ? "可击杀时自动Q" : "Auto Q if Can Killable Hero",false));
        }
        private void Game_OnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnDraw(EventArgs args)
        {
            if (DrawWRange && W.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, W.Range, Color.White);
            }
            if (DrawERange && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Blue);
            }
            if (DrawRRange && R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, R.Range,Color.Red);
            }
        }
        private static void Exploit_BasicAttack(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (Exploit && Orbwalker.IsAutoAttack(args.SData.Name))
            {
                if (args.Target != null && args.Target.IsEnemy && args.Target is AIHeroClient && sender.IsAlly && sender != null)
                {
                    var target = (AIHeroClient)args.Target;
                    if (target != null && target.Buffs.Any(b => b.Name.Equals("TwitchDeadlyVenom")))
                    {
                        var death = sender.GetAutoAttackDamage(target, true) > target.GetRealHeath(DamageType.Physical);
                        if (death)
                        {
                            Player.Spellbook.CastSpell(Q.Slot);
                        }
                    }
                }
            }
        }
        private static void Exploit_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (Exploit)
            {
                if (args.Target != null && args.Target.IsEnemy && args.Target is AIHeroClient && sender.IsAlly && sender != null)
                {
                    var caster = sender as AIHeroClient;
                    var target = (AIHeroClient)args.Target;
                    if (target != null && caster != null && target.Buffs.Any(b => b.Name.Equals("TwitchDeadlyVenom")))
                    {
                        var spelldamage = caster.GetSpellDamage(target, args.Slot);
                        var damagepercent = (spelldamage / target.GetRealHeath(DamageType.Physical)) * 100;
                        var death = damagepercent >= target.HealthPercent || spelldamage >= target.GetRealHeath(DamageType.Physical) || caster.GetAutoAttackDamage(target, true) >= target.GetRealHeath(DamageType.Physical);
                        if (death)
                        {
                            Player.Spellbook.CastSpell(Q.Slot);
                        }
                    }
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= LaneClearMana)
                return;

            if (JungleClearUseW && W.IsReady() )
            {

                var minions =
                    Cache.GetJungles(Player.ServerPosition,W.Range).Where(x => x.IsValidTarget(W.Range) && (!JungleWCheck || TwitchCache.GetVenomBuffCount(x) != 6)).ToList();
                var position = W.GetCircularFarmLocation(minions);
                if (position.MinionsHit >= JungleClearUseWMinKC)
                {
                    W.Cast(position.Position);
                }
            }
            if (JungleClearUseE && E.IsReady())
            {
                var JG = TwitchCache.VenomMinions.Any(x => x.IsJungle() && TwitchCache.VenomCanKillTarget(x) &&
                            (x.Name.Contains("Baron") ||
                            x.Name.Contains("Dragon") ||
                            x.SkinName.ToLower().Contains("riftherald")));
                if (JG)
                {
                    E.Cast();
                }
                if (TwitchCache.VenomMinions.Count(x => x.IsJungle() && E.IsInRange(x) && TwitchCache.VenomCanKillTarget(x)) >= JungleClearUseEMinKC)
                {
                    E.Cast();
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= LaneClearMana)
                return;
            if (LaneClearUseW && W.IsReady())
            {
                var minions =
                    Cache.GetMinions(Player.ServerPosition, W.Range).Where(x => x.IsValidTarget(W.Range) && TwitchCache.GetVenomBuffCount(x) < 4).ToList();
                var position = W.GetCircularFarmLocation(minions);
                if (position.MinionsHit >= LaneClearUseWMinKC)
                {
                    W.Cast(position.Position);
                }
            }
            if (LaneClearUseE && E.IsReady())
            {
                if (TwitchCache.VenomMinions.Count(x => E.IsInRange(x) && TwitchCache.VenomCanKillTarget(x)) >= LaneClearUseEMinKC)
                {
                    E.Cast();
                }
            }
        }
        private void Harass()
        {
            if (Player.ManaPercent <= HarassMana)
            {
                return;
            }

            if (HarassUseW && W.IsReady())
            {
                var enemy = TargetSelector.GetTargets(W.Range, DamageType.Physical).Where(x =>
                 (!HarassUseWCheck || TwitchCache.GetVenomBuffCount(x) != 6) && !TwitchCache.VenomCanKillTarget(x)).FirstOrDefault();
                if (enemy != null && enemy.IsValidTarget())
                {
                    var pred = W.GetPrediction(enemy);
                    if (pred.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
            }
            if (HarassUseE && E.IsReady())
            {
                if (HarassUseEOutRange)
                {
                    //获取染毒目标 判断敌人是否逃跑 并且1秒后敌人跑出E范围
                    var hasThisTarget = TwitchCache.VenomHeroes.Any(x =>
                    x.IsFleeing && Prediction.GetPrediction(x, 1.0f).CastPosition.DistanceToPlayer() >= E.Range && //敌人在逃跑,并且1秒后敌人离开E范围
                    x.DistanceToPlayer() < E.Range && x.DistanceToPlayer() > E.Range - 50 && //敌人在E技能边缘
                    TwitchCache.GetVenomBuffCount(x) >= HarassEOutRangeStack); //判断层数
                    if (hasThisTarget)
                    {
                        E.Cast();
                    }
                }
                if (TwitchCache.VenomHeroes.Any(z => TwitchCache.GetVenomBuffCount(z) >= HarassUseEStack))
                {
                    E.Cast();
                }
            }
        }
        private void Combo()
        {
            if (ComboUseQ && Q.IsReady() && !QActive)
            {
                var enemiesAround = Cache.EnemyHeroes.Count(x => x.IsEnemy && x.IsValidTarget() && x.DistanceToPlayer() <= ComboUseQMinRange);
                if (enemiesAround >= ComboUseQCountEnemy)
                {
                    Q.Cast();
                }
            }
            if (ComboUseW && W.IsReady())
            {
                var enemy = TargetSelector.GetTargets(W.Range, DamageType.Physical).Where(x =>
                 ((!ComboUseWCheck || TwitchCache.GetVenomBuffCount(x) != 6) || (!ComboUseWAntiFlee || x.IsFleeing)) && !TwitchCache.VenomCanKillTarget(x)).FirstOrDefault();
                if (enemy != null && enemy.IsValidTarget())
                {
                    var pred = W.GetPrediction(enemy);
                    if (pred.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
            }
            if (ComboUseE && E.IsReady())
            {
                if (ComboUseEOutRange)
                {
                    //获取染毒目标 判断敌人是否逃跑 并且1秒后敌人跑出E范围
                    var hasThisTarget = TwitchCache.VenomHeroes.Any(x =>
                    Prediction.GetPrediction(x, 0.6f).CastPosition.DistanceToPlayer() >= E.Range && //敌人在逃跑,并且1秒后敌人离开E范围
                    x.DistanceToPlayer() < E.Range && x.DistanceToPlayer() > E.Range - 100 && //敌人在E技能边缘
                    TwitchCache.GetVenomBuffCount(x) >= ComboUseEOutRangeStack); //判断层数
                    if (hasThisTarget)
                    {
                        E.Cast();
                    }
                }

                if (ComboUseEOnlyKill)
                {
                    if (TwitchCache.VenomHeroes.Any(y => TwitchCache.VenomCanKillTarget(y)))
                    {
                        E.Cast();
                    }
                }
                else
                {
                    if (TwitchCache.VenomHeroes.Any(y => TwitchCache.GetVenomBuffCount(y) == 6))
                    {
                        E.Cast();
                    }
                }
            }
            if (ComboUseR && R.IsReady())
            {
                var enemyList = Cache.EnemyHeroes.Where(x => x.IsEnemy && R.IsInRange(x) && x.IsValidTarget()).ToList();
                if (enemyList.Count >= ComboUseRCount)
                {
                    if (!ComboUseROnlyLine)
                    {
                        R.Cast();
                    }
                    else
                    {
                        //判断目标聚拢
                        if (enemyList.Any(x => x.CountEnemyHerosInRangeFix(120f) >= 2))
                        {
                            R.Cast();
                        }
                        //判断目标穿刺
                        var AttackTarget = TargetSelector.GetTarget(R.Range, DamageType.Physical); //获取到R范围内最佳角色 判断周围目标穿刺

                        var AttackPred = Prediction.GetPrediction(AttackTarget, 0.5f); //获取目标移动0.5s后的位置

                        foreach (var champ in from champ in enemyList
                                              let polygon = new Geometry.Rectangle(
                                                  Player.ServerPosition,
                                                  Player.ServerPosition.Extend(champ.ServerPosition, R.Range), 70f)
                                              where polygon.IsInside(AttackPred.CastPosition)
                                              select champ)
                        {
                            R.Cast();
                        }
                    }
                }
            }
        }
        private class TwitchCache
        {
            public static List<AIBaseClient> VenomEntities = new List<AIBaseClient>();
            public static List<AIMinionClient> VenomMinions = new List<AIMinionClient>();
            public static List<AIHeroClient> VenomHeroes = new List<AIHeroClient>();
            static TwitchCache()
            {
                foreach (var minion in GameObjects.Get<AIBaseClient>().Where(minion => minion.IsEnemy && minion.IsValid && HasVenomBuff(minion)))
                {
                    AddMinionObject(minion);
                }
                AIBaseClient.OnBuffAdd += OnBuffAdd;
                Game.OnUpdate += Game_OnUpdate;
            }
            private static void OnBuffAdd(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
            {
                if (sender.IsValidTarget())
                {
                    if (args.Buff.Name.Equals("TwitchDeadlyVenom"))
                    {
                        AddMinionObject(sender);
                    }
                }
            }
            private static void Game_OnUpdate(EventArgs args)
            {
                VenomEntities.RemoveAll(minion => !IsValidVenom(minion));
                VenomMinions.RemoveAll(minion => !IsValidVenom(minion));
                VenomHeroes.RemoveAll(minion => !IsValidVenom(minion));
            }
            private static bool IsValidVenom(AIBaseClient minion)
            {
                if (minion == null || !minion.IsValid || minion.IsDead || !HasVenomBuff(minion))
                    return false;
                return true;
            }
            private static void AddMinionObject(AIBaseClient minion)
            {
                if (minion == null) return;

                VenomEntities.Add(minion);

                GameObjectType Type = minion.Type;
                if (Type == GameObjectType.AIHeroClient)
                {
                    var HeroObj = minion as AIHeroClient;
                    if (HeroObj != null)
                    {
                        VenomHeroes.Add(HeroObj);
                    }
                    return;
                }
                if (Type == GameObjectType.AIMinionClient)
                {
                    var MinionObj = minion as AIMinionClient;
                    if (MinionObj != null)
                    {
                        VenomMinions.Add(MinionObj);
                    }
                }
            }
            public static bool HasVenomBuff(AIBaseClient target)
            {
                return GetRendBuffInstance(target) != null;
            }
            private static BuffInstance GetRendBuffInstance(AIBaseClient target)
            {
                return target.Buffs.Find(b => b.IsValid && b.Name.Equals("TwitchDeadlyVenom"));
            }
            public static int GetVenomBuffCount(AIBaseClient t)
            {
                if (!HasVenomBuff(t))
                    return 0;
                return GetRendBuffInstance(t).Count;
            }
            public static bool VenomCanKillTarget(AIBaseClient target)
            {
                if (target == null
                    || !target.IsValidTarget(E.Range + 200)
                    || !HasVenomBuff(target)
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
                            return GetRealVenomDamage(target) > (target.GetRealHeath(DamageType.Physical) + (hero.Mana / 2));
                        }

                        if (hero.HasBuff("ManaBarrier") && !(hero.AllShield > 0))
                        {
                            return false;
                        }
                    }
                }
                return GetRealVenomDamage(target) > target.GetRealHeath(DamageType.Physical);
            }
            private static int GetKillableDragon()
            {
                return Player.Buffs.Count(x => x.Name.ToLower().StartsWith("srx_dragon"));
            }
            public static float GetRealVenomDamage(AIBaseClient target)
            {
                if (!E.IsReady() || !HasVenomBuff(target))
                    return 0;
                if (!target.IsValidTarget())
                    return 0;
                var eLevel = Player.Spellbook.GetSpell(SpellSlot.E).Level;
                if (eLevel <= 0)
                    return 0;
                int buffCount = GetVenomBuffCount(target);
                //------------------
                var baseDamage = new[] { 0, 20, 30, 40, 50, 60 }[eLevel];
                var PhysicalDamage = new[] { 0, 15, 20, 25, 30, 35 }[eLevel] + 0.35f * (Player.TotalAttackDamage - Player.BaseAttackDamage);
                var MagicDamage = 0.33f * Player.TotalMagicalDamage;
                //
                var damage = (float)Player.CalculateMixedDamage(target, baseDamage + (PhysicalDamage * buffCount), MagicDamage * buffCount);
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
                return damage;
            }
        }
        private class DamageIndicator
        {
            public static System.Drawing.Color EnemyColor = System.Drawing.Color.Lime;
            public static System.Drawing.Color JungleColor = System.Drawing.Color.White;

            private static DamageToUnitDelegate _damageToUnit;
            public delegate float DamageToUnitDelegate(AIBaseClient minion);

            public static DamageToUnitDelegate DamageToUnit
            {
                get
                {
                    return _damageToUnit;
                }

                set
                {
                    if (_damageToUnit == null)
                    {
                        Drawing.OnEndScene += OnEndScene;
                    }

                    _damageToUnit = value;
                }
            }

            private static void OnEndScene(EventArgs args)
            {
                if (_damageToUnit == null)
                    return;


                foreach (var hero in Cache.EnemyHeroes
                    .Where(x => x.IsEnemy && x.IsValidTarget()
                            && TwitchCache.HasVenomBuff(x)))
                {
                    DrawLine(hero);
                }

            }
            private static void DrawLine(AIBaseClient unit)
            {
                var damage = _damageToUnit(unit);
                if (damage <= 0)
                    return;
                int HpBarLeftX = (int)unit.HPBarPosition.X - 45;
                int HpBarLeftY = (int)unit.HPBarPosition.Y - 25;
                int HpBarHeight = 13;
                int HPBarTotalLength = ((int)unit.HPBarPosition.X - HpBarLeftX) * 2 + 16;
                var DamageCeiling = damage / unit.GetRealHeath(DamageType.Physical);
                DamageCeiling = Math.Min(DamageCeiling, 1);
                int FixedHPBarLength = (int)(DamageCeiling * HPBarTotalLength);
                PlusRender.DrawRect(HpBarLeftX, HpBarLeftY, FixedHPBarLength, HpBarHeight, new Color((int)Color.Green.R, (int)Color.Green.G, (int)Color.Green.B, 144));
            }
        }
    }
}
