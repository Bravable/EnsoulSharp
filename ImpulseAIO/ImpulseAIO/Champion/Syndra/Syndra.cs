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
using ImpulseAIO.Common;

namespace ImpulseAIO.Champion.Syndra
{
    class SeedInfo
    {
        public AIBaseClient Pointer { get; set; }
        public int VaildTime { get; set; }
    }
    internal class Syndra : Base 
    {
        private static Spell Q, QE, W, E, R;
        private static int delayyyy;
        private static int lastwe;
        private static int lastw;
        private static int lastqe;
        private static List<SeedInfo> SeedsInfo = new List<SeedInfo>();

        public Syndra()
        {
            Q = new Spell(SpellSlot.Q, 800);
            Q.SetSkillshot(0.6f, 70f, float.MaxValue, false, SpellType.Circle);

            W = new Spell(SpellSlot.W, 925f);
            W.SetSkillshot(0.25f, 120f, 1600f, false, SpellType.Circle);

            E = new Spell(SpellSlot.E, 700f);
            E.SetSkillshot(0.25f, (float)(45 * 0.5), 2500, false, SpellType.Cone);

            R = new Spell(SpellSlot.R, 675f);
            R.SetTargetted(0.25f, 1100f);

            QE = new Spell(SpellSlot.E, 1100);
            QE.SetSkillshot(0.5f, 55, 2500f, false, SpellType.Line);

            OnMenuLoad();
            Render.OnEndScene += OnDraw;
            AIBaseClient.OnProcessSpellCast += AIBaseClient_OnProcessSpellCast;
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += OnCreateSeedObj;
            GameObject.OnDelete += OnRemoveSeedObj;
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
        }
        private void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "SyndraQSpell")
                {
                    if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    {
                        var EndPos = args.End;
                        if (EndPos.Distance(Player) <= 1400 && lastw < Variables.TickCount)
                        {
                            DelayAction.Add(100, () => E.Cast(EndPos));

                        }
                    }
                    if (ChampionMenu["combo"]["qe"].GetValue<MenuKeyBind>().Active)
                    {
                        var EndPos = args.End;
                        if (lastw < Variables.TickCount)
                        {
                            DelayAction.Add(100, () => E.Cast(EndPos));
                        }
                    }
                }
            }
        }
        private void OnDraw(EventArgs args)
        {
            if (ChampionMenu["drawings"]["drawq"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, Q.Range, Color.White);
            }
            if (ChampionMenu["drawings"]["drawqe"].GetValue<MenuBool>().Enabled && E.IsReady() && Q.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, QE.Range, Color.Orange);
            }
            if (ChampionMenu["drawings"]["draww"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, W.Range, Color.PaleGreen);
            }
            if (ChampionMenu["drawings"]["drawe"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Red);
            }
            if (ChampionMenu["drawings"]["drawr"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, R.Range, Color.MediumPurple);
            }
            if (ChampionMenu["drawings"]["drawdamage"].GetValue<MenuBool>().Enabled)
            {
                foreach (var enemy in Cache.EnemyHeroes.Where(e => e.IsValidTarget(QE.Range)))
                {
                    var dmg = AllDmg(enemy);
                    if (dmg <= 0d)
                        continue;
                    int HpBarLeftX = (int)enemy.HPBarPosition.X - 45;
                    int HpBarLeftY = (int)enemy.HPBarPosition.Y - 25;
                    int HpBarHeight = 13;
                    int HPBarTotalLength = ((int)enemy.HPBarPosition.X - HpBarLeftX) * 2 + 16;
                    var DamageCeiling = dmg / enemy.GetRealHeath(DamageType.Magical);
                    DamageCeiling = Math.Min(DamageCeiling, 1);
                    int FixedHPBarLength = (int)(DamageCeiling * HPBarTotalLength);
                    PlusRender.DrawRect(HpBarLeftX, HpBarLeftY, FixedHPBarLength, HpBarHeight, new Color((int)Color.Green.R, (int)Color.Green.G, (int)Color.Green.B, 144));
                }
            }
            foreach(var orb in SeedsInfo)
            {
                var owTime = (orb.VaildTime - Variables.GameTimeTickCount) / 1000f;
                if (owTime < 0)
                    continue;
                var pos = Drawing.WorldToScreen(orb.Pointer.Position);
                PlusRender.DrawText(owTime.ToString(), pos.X, pos.Y, Color.White);
            }
        }
        private void OnCreateSeedObj(GameObject sender, EventArgs args)
        {
            var RealObj = sender as AIBaseClient;
            if (RealObj == null)
            {
                return;
            }
            if(RealObj.Name == "Seed" && RealObj.MaxHealth == 1 && RealObj.IsAlly)
            {
                SeedsInfo.Add(new SeedInfo() { Pointer = RealObj, VaildTime = Variables.GameTimeTickCount + 6000 });
            }
        }
        private void OnRemoveSeedObj(GameObject sender, EventArgs args)
        {
            var RealObj = sender as AIBaseClient;
            if (RealObj == null)
            {
                return;
            }
            var FindNetwork = SeedsInfo.Find(x => x.Pointer.NetworkId == RealObj.NetworkId);
            if (FindNetwork != null)
            {
                SeedsInfo.RemoveAll(x => x.Pointer.NetworkId == FindNetwork.Pointer.NetworkId);
            }
        }
        private void Game_OnUpdate(EventArgs args)
        {
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level == 3)
            {
                R.Range = 750;
            }
            if (Objects() != null)
            {
                W.From = Objects().Position;
            }
            Killsteal();
            QEEvent();
            AutoDashQ();
            if (ChampionMenu["harass"]["qtoggle"].GetValue<MenuKeyBind>().Active && Orbwalker.ActiveMode != OrbwalkerMode.Combo)
            {
                Harass();
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    if (!ChampionMenu["harass"]["qtoggle"].GetValue<MenuKeyBind>().Active)
                    {
                        Harass();
                    }
                    break;
                case OrbwalkerMode.LaneClear:
                    JungleClear();
                    LaneClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnBeforeAttack(object obj, BeforeAttackEventArgs args)
        {
            if (ChampionMenu["aa"]["disable"].GetValue<MenuBool>().Enabled)
            {
                if(Player.Level >= ChampionMenu["aa"]["level"].GetValue<MenuSlider>().Value)
                {
                    if (ChampionMenu["aa"]["enaifcd"].GetValue<MenuBool>().Enabled)
                    {
                        if(!Q.IsReady() && !W.IsReady() && !E.IsReady())
                        {
                            args.Process = true;
                            return;
                        }
                    }
                    if (ChampionMenu["aa"]["enaifmana"].GetValue<MenuBool>().Enabled)
                    {
                        if(Player.ManaPercent <= 20)
                        {
                            args.Process = true;
                            return;
                        }
                    }
                    args.Process = false;
                    return;
                }
            }
        }
        private void OnMenuLoad()
        {
            var ComboMenu = ChampionMenu.Add(new Menu("combo", "连招设置"));
            {
                ComboMenu.Add(new MenuBool("useq", "Q"));
                ComboMenu.Add(new MenuBool("useqe", "QE"));
                ComboMenu.Add(new MenuSlider("range", "^- QE 距离", 1100, 800, 1150));
                ComboMenu.Add(new MenuBool("usew", "W"));
                ComboMenu.Add(new MenuBool("usee", "E"));

                var EngageMenu = new Menu("engage", "R 设置");
                EngageMenu.Add(new MenuList("rmode", "R 模式:", new[] { "伤害优先", "速度优先" }, 0));
                EngageMenu.Add(new MenuBool("user", "连招用R"));
                EngageMenu.Add(new MenuSlider("waster", "^- 不要保留R如果敌人血量 <=", 0, 0, 500));
                EngageMenu.Add(new MenuSlider("orb", "最小球数使用快速R", 5, 3, 6));
                EngageMenu.Add(new MenuBool("kill", "仅仅在可击杀时才快速R"));
                ComboMenu.Add(EngageMenu);

                ComboMenu.Add(new MenuKeyBind("qe", "手动 QE热键", Keys.T, KeyBindType.Press)).AddPermashow();
                ComboMenu.Add(new MenuList("qemode", "QE方式", new[] { "目标", "鼠标", "智能" }, 0));
            }
            var BlackList = ChampionMenu.Add(new Menu("blacklist", "R 黑名单"));
            {
                foreach (var target in GameObjects.EnemyHeroes)
                {
                    BlackList.Add(new MenuBool(target.CharacterName.ToLower(), target.CharacterName,false));
                }
            }
            var HarassMenu = ChampionMenu.Add(new Menu("harass", "骚扰设置"));
            {
                HarassMenu.Add(new MenuKeyBind("qtoggle", "自动骚扰", Keys.K, KeyBindType.Toggle)).AddPermashow();
                HarassMenu.Add(new MenuSlider("mana", "当蓝量 >= X%才骚扰", 50));
                HarassMenu.Add(new MenuBool("dashing", "突进自动q"));

                HarassMenu.Add(new MenuBool("useq", "使用 Q 骚扰"));
                HarassMenu.Add(new MenuBool("usew", "使用 W 骚扰"));
                HarassMenu.Add(new MenuBool("usee", "使用 E 骚扰"));

            }
            var AAMenu = ChampionMenu.Add(new Menu("aa", "普攻禁止"));
            {
                AAMenu.Add(new MenuBool("disable", "连招时 关闭 AA", false));
                AAMenu.Add(new MenuSlider("level", "如果等级 >= X时才关闭AA", 6, 1, 18));
                AAMenu.Add(new MenuBool("enaifcd", "当技能全部CD时 启动AA"));
                AAMenu.Add(new MenuBool("enaifmana", "当自己蓝量不足时 启动AA"));
            }
            var FarmMenu = ChampionMenu.Add(new Menu("farming", "清线设置"));
            var LaneClear = FarmMenu.Add(new Menu("lane", "清线"));
            {
                LaneClear.Add(new MenuSlider("mana", "当蓝量 >= X时才清线", 50));
                LaneClear.Add(new MenuBool("useq", "用 Q 清线"));
                LaneClear.Add(new MenuSlider("hitq", "^- 如果能命中 X 小兵", 2, 1, 6));
                LaneClear.Add(new MenuBool("usew", "用 W 清线"));
                LaneClear.Add(new MenuSlider("hitw", "^- 如果能命中 X 小兵", 3, 1, 6));
            }
            var JungleClear = FarmMenu.Add(new Menu("jungle", "清野设置"));
            {
                JungleClear.Add(new MenuSlider("mana", "当蓝量 >= X时才清野", 50));
                JungleClear.Add(new MenuBool("useq", "用 Q 清野"));
                JungleClear.Add(new MenuBool("usew", "用 W 清野"));
            }
            var KSMenu = ChampionMenu.Add(new Menu("killsteal", "击杀设置"));
            {
                KSMenu.Add(new MenuBool("ksq", "击杀 Q"));
                KSMenu.Add(new MenuBool("ksw", "击杀 W"));
                KSMenu.Add(new MenuBool("ksr", "击杀 R"));
                KSMenu.Add(new MenuSlider("waster", "^- 不要保留R如果敌人血量 <=", 0, 0, 500));
            }
            var DrawMenu = ChampionMenu.Add(new Menu("drawings", "绘制设置"));
            {
                DrawMenu.Add(new MenuBool("drawq", "Draw Q Range"));
                DrawMenu.Add(new MenuBool("drawqe", "Draw QE Range"));
                DrawMenu.Add(new MenuBool("draww", "Draw W Range", false));
                DrawMenu.Add(new MenuBool("drawe", "Draw E Range", false));
                DrawMenu.Add(new MenuBool("drawr", "Draw R Range"));
                DrawMenu.Add(new MenuBool("drawdamage", "Draw R Damage"));
            }
        }
        private void AutoDashQ()
        {
            if (Player.IsRecalling() || MenuGUI.IsShopOpen)
                return;
            if (Player.ManaPercent <= 15)
                return;
            if (Q.IsReady() && ChampionMenu["harass"]["dashing"].GetValue<MenuBool>().Enabled)
            {
                foreach(var obj in Cache.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.Dash);
                }
            }
        }
        private void Harass()
        {
            if (Player.ManaPercent >= ChampionMenu["harass"]["mana"].GetValue<MenuSlider>().Value)
            {
                if (ChampionMenu["harass"]["usew"].GetValue<MenuBool>().Enabled && W.IsReady())
                {
                    var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);

                    if (target.IsValidTarget() && target.Distance(Player) < W.Range)
                    {
                        if (target != null)
                        {
                            if (!Player.HasBuff("syndrawtooltip"))
                            {
                                if (target.IsValidTarget(W.Range) && W.IsReady())
                                {
                                    if (delayyyy <= Variables.TickCount)
                                    {
                                        if (Objects() != null)
                                        {
                                            if (W.Cast(Objects().Position))
                                            {

                                                lastw = Variables.TickCount + Game.Ping + 20;
                                                lastwe = Variables.TickCount + Game.Ping + 200;
                                                delayyyy = 1000 + Variables.TickCount;
                                                return;
                                            }
                                        }

                                    }
                                }
                            }

                            if (Player.HasBuff("syndrawtooltip"))
                            {

                                if (!target.HasBuff("SyndraEDebuff"))
                                {



                                    if (lastqe < Variables.TickCount)
                                    {
                                        var predN = W.GetPrediction(target, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                                        if (predN.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(predN.CastPosition);
                                        }
                                    }
                                }

                            }
                        }
                    }
                }



                if (ChampionMenu["harass"]["useq"].GetValue<MenuBool>().Enabled && Q.IsReady())
                {
                    var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                    if (target.IsValidTarget() && target.Distance(Player) < Q.Range)
                    {
                        if (target != null)
                        {
                            if (target.IsValidTarget(Q.Range))
                            {
                                var predN = Q.GetPrediction(target, true);
                                if (predN.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(predN.CastPosition);
                                }
                            }
                        }
                    }
                }

                if (ChampionMenu["harass"]["usee"].GetValue<MenuBool>().Enabled && E.IsReady())
                {
                    if (E.IsReady() && lastwe <= Variables.TickCount)
                    {
                        var target = TargetSelector.GetTarget(QE.Range, DamageType.Magical);

                        if (target.IsValidTarget() && target.Distance(Player) < Q.Range)
                        {
                            if (target.IsValidTarget(QE.Range))
                            {
                                if (target != null)
                                {
                                    foreach (var orb in SeedsInfo.Where(x => x.Pointer.Distance(Player) < 1000))
                                    {
                                        if (orb.Pointer.Distance(Player) <= E.Range && Player.Distance(orb.Pointer.Position) >= 100 && target.Distance(Player) <= 1100)
                                        {
                                            var enemyPred = QE.GetPrediction(target);
                                            var test = Player.Distance(enemyPred.CastPosition);
                                            var miau = Player.Position.Extend(orb.Pointer.Position, test);
                                            if (miau.Distance(enemyPred.CastPosition) <
                                                QE.Width + target.BoundingRadius - 60)
                                            {
                                                E.Cast(orb.Pointer.Position);
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
        private void Combo()
        {
            if (ChampionMenu["combo"]["useq"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range,DamageType.Magical);

                if (target.IsValidTarget())
                {
                    var predN = Q.GetPrediction(target);
                    if (predN.Hitchance >= HitChance.High)
                    {
                        Q.Cast(predN.CastPosition);
                    }
                }
            }
            if (ChampionMenu["combo"]["usew"].GetValue<MenuBool>().Enabled && W.IsReady())
            {

                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);

                if (target.IsValidTarget() && target.Distance(Player) <= W.Range)
                {
                    if (target != null)
                    {

                        if (!Player.HasBuff("syndrawtooltip"))
                        {
                            if (target.IsValidTarget(W.Range) && W.IsReady())
                            {
                                if (delayyyy <= Variables.TickCount)
                                {
                                    if (Objects() != null)
                                    {
                                        if (W.Cast(Objects().Position))
                                        {

                                            lastw = Variables.TickCount + Game.Ping + 20;
                                            lastwe = Variables.TickCount + Game.Ping + 200;
                                            delayyyy = 1000 + Variables.TickCount;
                                            return;
                                        }
                                    }

                                }
                            }
                        }
                        if (Player.HasBuff("syndrawtooltip"))
                        {

                            if (!target.HasBuff("SyndraEDebuff"))
                            {
                                if (Objects() != null)
                                {
                                    if (lastqe < Variables.TickCount)
                                    {
                                        var predN = W.GetPrediction(target, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                                        if (predN.Hitchance >= HitChance.High)
                                        {
                                            W.Cast(predN.CastPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
            if (E.IsReady() && ChampionMenu["combo"]["usee"].GetValue<MenuBool>().Enabled)
            {
                var target = TargetSelector.GetTarget(QE.Range, DamageType.Magical);

                if (target.IsValidTarget())
                {
                    foreach (var orb in SeedsInfo.Where(x => x.Pointer.Distance(Player) < 1000))
                    {
                        if (orb.Pointer.Distance(Player) <= E.Range && Player.Distance(orb.Pointer.Position) >= 100 && target.Distance(Player) <= 1100)
                        {

                            var enemyPred = QE.GetPrediction(target);
                            var test = Player.Distance(enemyPred.CastPosition);
                            var miau = Player.Position.Extend(orb.Pointer.Position, test);
                            if (miau.Distance(enemyPred.CastPosition) < QE.Width + target.BoundingRadius - 60)
                            {
                                E.Cast(orb.Pointer.Position);
                                lastqe = Variables.TickCount + Game.Ping + 100;

                            }
                        }
                    }
                }
            }
            if (E.IsReady() && ChampionMenu["combo"]["useqe"].GetValue<MenuBool>().Enabled)
            {
                var target = TargetSelector.GetTarget(QE.Range, DamageType.Magical);

                if (target.IsValidTarget() && target.Distance(Player) < ChampionMenu["combo"]["range"].GetValue<MenuSlider>().Value)
                {
                    if (target.Distance(Player) > E.Range)
                    {

                        QE.Delay = E.Delay + Q.Range / E.Speed;

                        QE.From = Player.Position.Extend(target.Position, Q.Range);

                        var pred = QE.GetPrediction(target);

                        if (pred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(Player.Position.Extend(pred.CastPosition, Q.Range - 100));
                        }
                    }
                }
            }
            if (R.IsReady() && ChampionMenu["combo"]["engage"]["user"].GetValue<MenuBool>().Enabled)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                if (target.IsValidTarget())
                {
                    if (target.GetRealHeath(DamageType.Magical) >= ChampionMenu["combo"]["engage"]["waster"].GetValue<MenuSlider>().Value)
                    {
                        switch (ChampionMenu["combo"]["engage"]["rmode"].GetValue<MenuList>().Index)
                        {
                            case 0:
                                if (target != null)
                                {
                                    if (target.GetRealHeath(DamageType.Magical) < GetR(target))
                                    {
                                        if (!ChampionMenu["blacklist"][target.CharacterName.ToLower()].GetValue<MenuBool>().Enabled)
                                        {
                                            R.CastOnUnit(target);
                                        }
                                    }
                                }
                                break;
                            case 1:
                                if (target != null)
                                {
                                    if (!ChampionMenu["blacklist"][target.CharacterName.ToLower()].GetValue<MenuBool>().Enabled)
                                    {
                                        if (Player.Spellbook.GetSpell(SpellSlot.R).Ammo >=
                                            ChampionMenu["combo"]["engage"]["orb"].GetValue<MenuSlider>().Value)
                                        {
                                            if (!ChampionMenu["combo"]["engage"]["kill"].GetValue<MenuBool>().Enabled)
                                            {
                                                R.CastOnUnit(target);
                                            }
                                            if (ChampionMenu["combo"]["engage"]["kill"].GetValue<MenuBool>().Enabled)
                                            {
                                                double QDamage = Player.GetSpellDamage(target, SpellSlot.Q);
                                                double WDamage = Player.GetSpellDamage(target, SpellSlot.W);
                                                double EDamage = Player.GetSpellDamage(target, SpellSlot.E);
                                                double RDamage = GetR(target);

                                                if (target.GetRealHeath(DamageType.Magical) <= QDamage + WDamage + EDamage + RDamage)
                                                {
                                                    R.CastOnUnit(target);
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                        }
                    }
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear)
                return;
            bool useQ = ChampionMenu["farming"]["jungle"]["useq"].GetValue<MenuBool>().Enabled;
            bool useW = ChampionMenu["farming"]["jungle"]["usew"].GetValue<MenuBool>().Enabled;
            float manapercent = ChampionMenu["farming"]["jungle"]["mana"].GetValue<MenuSlider>().Value;

            var jungles = GameObjects.Jungle.Where(m => m.IsValidTarget(W.Range) && m.IsTargetable);
            var predW = FarmPrediction.GetBestCircularFarmLocation(jungles.Select(x => Q.GetPrediction(x).CastPosition.ToVector2()).ToList(), 180f, W.Range);
            var Balls = Objects();

            if (useW && Balls != null && W.IsReady())
            {
                if (predW.MinionsHit >= 1)
                {
                    if (Balls != null || Player.HasBuff("syndrawtooltip"))
                    {
                        if (!Player.HasBuff("syndrawtooltip") && delayyyy < Variables.TickCount)
                        {
                            W.Cast(Balls.Position);
                            delayyyy = Variables.TickCount + 1000;
                        }
                        if (Player.HasBuff("syndrawtooltip"))
                        {
                            W.Cast(predW.Position);
                        }
                    }
                }
            }
            if (useQ && Q.IsReady())
            {
                var junglesQ = jungles.Where(x => x.IsJungle() && x.IsValidTarget(Q.Range) && x.IsTargetable).Select(x => Q.GetPrediction(x).CastPosition.ToVector2()).ToList();
                var predq = FarmPrediction.GetBestCircularFarmLocation(junglesQ, 120f, Q.Range);
                if (predq.MinionsHit >= 1)
                {
                    Q.Cast(predq.Position);
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear)
                return;

            bool useQ = ChampionMenu["farming"]["lane"]["useq"].GetValue<MenuBool>().Enabled;
            bool useW = ChampionMenu["farming"]["lane"]["usew"].GetValue<MenuBool>().Enabled;
            float manapercent = ChampionMenu["farming"]["lane"]["mana"].GetValue<MenuSlider>().Value;
            if (manapercent < Player.ManaPercent)
            {
                if (useQ && Q.IsReady())
                {
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                    {
                        if (Cache.GetMinions(Player.Position,Q.Range).Count(h => h.IsValidTarget(200, false,
                                minion.Position)) >= ChampionMenu["farming"]["lane"]["hitq"].GetValue<MenuSlider>().Value)
                        {
                            if (minion.IsValidTarget(Q.Range) && minion != null)
                            {
                                Q.Cast(minion);
                            }
                        }
                    }
                }
                if (useW && W.IsReady())
                {
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(W.Range))
                    {
                        if (Cache.GetMinions(Player.Position, W.Range).Count(h => h.IsValidTarget(225, false,
                                minion.Position)) >= ChampionMenu["farming"]["lane"]["hitw"].GetValue<MenuSlider>().Value)
                        {

                            if (minion.IsValidTarget(W.Range) && minion != null)
                            {
                                if (!Player.HasBuff("syndrawtooltip") && delayyyy < Variables.TickCount)
                                {
                                    var grab =
                                        Cache.GetMinions(Player.Position, W.Range).FirstOrDefault();

                                    if (Cache.GetMinions(Player.Position, W.Range).Count(h => h.IsValidTarget(200, false,
                                            minion.Position)) >=
                                        ChampionMenu["farming"]["lane"]["hitw"].GetValue<MenuSlider>().Value)
                                    {
                                        if (minion != null)
                                        {
                                            W.CastOnUnit(grab);
                                            delayyyy = Variables.TickCount + 1000;
                                        }
                                    }

                                }
                                if (Player.HasBuff("syndrawtooltip"))
                                {
                                    if (Cache.GetMinions(Player.Position,W.Range).Count(h => h.IsValidTarget(200, false,
                                            minion.Position)) >=
                                        ChampionMenu["farming"]["lane"]["hitw"].GetValue<MenuSlider>().Value)
                                    {
                                        if (minion != null)
                                        {
                                            W.Cast(minion);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
        private void Killsteal()
        {
            if (Q.IsReady() &&
                ChampionMenu["killsteal"]["ksq"].GetValue<MenuBool>().Enabled)
            {
                
                var bestTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.Q) >= bestTarget.GetRealHeath(DamageType.Magical) &&
                    bestTarget.IsValidTarget(Q.Range))
                {
                    var predN = Q.GetPrediction(bestTarget, true);
                    if (predN.Hitchance >= HitChance.High)
                    {
                        Q.Cast(predN.CastPosition);
                    }
                }
            }
            if (W.IsReady() &&
                ChampionMenu["killsteal"]["ksw"].GetValue<MenuBool>().Enabled)
            {
                var bestTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.W) >= bestTarget.GetRealHeath(DamageType.Magical) &&
                    bestTarget.IsValidTarget(W.Range))
                {

                    if (!Player.HasBuff("syndrawtooltip"))
                    {

                        if (delayyyy <= Variables.TickCount)
                        {
                            if (Objects() != null)
                            {
                                if (W.Cast(Objects().Position))
                                {

                                    lastw = Variables.TickCount + Game.Ping + 20;
                                    lastwe = Variables.TickCount + Game.Ping + 200;
                                    delayyyy = 1000 + Variables.TickCount;
                                    return;
                                }
                            }

                        }
                    }
                    if (Player.HasBuff("syndrawtooltip"))
                    {

                        if (!bestTarget.HasBuff("SyndraEDebuff"))
                        {
                            if (lastqe < Variables.TickCount)
                            {
                                var predN = W.GetPrediction(bestTarget, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                                if (predN.Hitchance >= HitChance.High)
                                {
                                    W.Cast(predN.CastPosition);
                                }
                            }

                        }
                    }
                }
            }
            if (R.IsReady() &&
                ChampionMenu["killsteal"]["ksr"].GetValue<MenuBool>().Enabled)
            {
                var bestTarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                if (bestTarget != null &&
                    GetR(bestTarget) > bestTarget.GetRealHeath(DamageType.Magical) &&
                    bestTarget.IsValidTarget(R.Range) && bestTarget.GetRealHeath(DamageType.Magical) >=
                    ChampionMenu["killsteal"]["waster"].GetValue<MenuSlider>().Value)
                {
                    if (!ChampionMenu["blacklist"][bestTarget.CharacterName.ToLower()].GetValue<MenuBool>().Enabled)
                    {
                        R.CastOnUnit(bestTarget);
                    }
                }
            }
        }
        private void QEEvent()
        {
            if (ChampionMenu["combo"]["qe"].GetValue<MenuKeyBind>().Active && E.IsReady() && Q.IsReady())
            {
                var pos = (Game.CursorPos - Player.Position).Normalized();
                var target = TargetSelector.GetTarget(QE.Range, DamageType.Magical);
                switch (ChampionMenu["combo"]["qemode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        {
                            if (!target.IsValidTarget())
                            {
                                return;
                            }

                            if (target.Distance(Player) > E.Range)

                            {
                                QE.Delay = E.Delay + Q.Range / E.Speed;

                                QE.From = Player.Position.Extend(target.Position, Q.Range);

                                var pred = QE.GetPrediction(target);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(Player.Position.Extend(pred.CastPosition, Q.Range - 100));
                                }
                            }

                        }
                        break;
                    case 1:

                        if (Player.Distance(Game.CursorPos) < 800)
                        {
                            Q.Cast(Game.CursorPos);
                        }
                        if (Player.Distance(Game.CursorPos) > 800)
                        {
                            Q.Cast(Player.Position + pos * 800);
                        }

                        break;
                    case 2:

                        if (Player.CountEnemyHeroesInRange(ChampionMenu["combo"]["range"].GetValue<MenuSlider>().Value) == 0)
                        {

                            if (Player.Distance(Game.CursorPos) < 800)
                            {
                                Q.Cast(Game.CursorPos);
                            }
                            if (Player.Distance(Game.CursorPos) > 800)
                            {
                                Q.Cast(Player.Position + pos * 800);
                            }
                        }
                        if (Player.CountEnemyHeroesInRange(ChampionMenu["combo"]["range"].GetValue<MenuSlider>().Value) > 0)
                        {
                            if (!target.IsValidTarget())
                            {
                                return;
                            }

                            if (target.Distance(Player) > E.Range)
                            {
                                QE.Delay = E.Delay + Q.Range / E.Speed;

                                QE.From = Player.Position.Extend(target.Position, Q.Range);

                                var pred = QE.GetPrediction(target);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(Player.Position.Extend(pred.CastPosition, Q.Range - 100));
                                }
                            }
                        }
                        break;
                }
            }
        }
        private double AllDmg(AIBaseClient unit)
        {
            if (unit == null || !unit.IsValid || unit.IsDead)
                return 0d;

            double damage = 0d;
            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(unit, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(unit, SpellSlot.W);
            }
            if (E.IsReady() )
            {
                damage += Player.GetSpellDamage(unit, SpellSlot.E);
            }
            if (R.IsReady())
            {
                damage += GetR(unit);
            }
            return damage;
        }
        private List<AIBaseClient> GetAllGenericMinionsTargets()
        {
            return GetAllGenericMinionsTargetsInRange(float.MaxValue);
        }
        private List<AIBaseClient> GetAllGenericMinionsTargetsInRange(float range)
        {
            return GetEnemyLaneMinionsTargetsInRange(range).Concat(GetGenericJungleMinionsTargetsInRange(range))
                .ToList();
        }
        private List<AIBaseClient> GetAllGenericUnitTargets()
        {
            return GetAllGenericUnitTargetsInRange(float.MaxValue);
        }
        private List<AIBaseClient> GetAllGenericUnitTargetsInRange(float range)
        {
            return GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(range))
                .Concat<AIBaseClient>(GetAllGenericMinionsTargetsInRange(range)).ToList();
        }
        private List<AIBaseClient> GetEnemyLaneMinionsTargets()
        {
            return GetEnemyLaneMinionsTargetsInRange(float.MaxValue);
        }
        private List<AIBaseClient> GetEnemyLaneMinionsTargetsInRange(float range)
        {
            return Cache.GetMinions(Player.Position,range).ToList();
        }
        private List<AIMinionClient> GetGenericJungleMinionsTargets()
        {
            return GetGenericJungleMinionsTargetsInRange(float.MaxValue);
        }
        private List<AIMinionClient> GetGenericJungleMinionsTargetsInRange(float range)
        {
            return GameObjects.Jungle.Where(m => !GameObjects.JungleSmall.Contains(m) && m.IsValidTarget(range))
                .ToList();
        }
        private AIBaseClient Objects()
        {
            var orb = SeedsInfo.FirstOrDefault(
                x => x.Pointer.IsValid && x.Pointer.Distance(Player) < W.Range);
            var minion =
                Cache.GetMinions(Player.Position,W.Range).FirstOrDefault();
            var jungle =
                Cache.GetJungles(Player.Position,W.Range).FirstOrDefault();
            if (orb != null)
            {
                return orb.Pointer;
            }
            if (minion != null)
            {
                return minion;
            }
            if (jungle != null)
            {
                return jungle;
            }
            return null;
        }
        private double GetR(AIBaseClient target)
        {
            double meow = 0;
            double extra = 0;
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level == 1)
            {
                meow = 90;
            }
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level == 2)
            {
                meow = 135;
            }
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level == 3)
            {
                meow = 180;
            }
            double ap = Player.TotalMagicalDamage * 0.2;
            double main = (ap + meow) * 4;
            if (Player.Spellbook.GetSpell(SpellSlot.R).Ammo > 3)
            {
                extra = (ap + meow) * (Player.Spellbook.GetSpell(SpellSlot.R).Ammo - 3);
            }
            double together = main + extra;
            double damage = Player.CalculateDamage(target, DamageType.Magical, together);
            return damage;

        }
    }
}
