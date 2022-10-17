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
using ImpulseAIO.Common.Evade;
namespace ImpulseAIO.Champion.Orianna
{

    internal class Orianna : Base
    {
        private static Spell Q, W, E, R;
        #region 菜单选项
        private static bool Combo_UseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool Combo_UseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool Combo_UseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static int UseR => ChampionMenu["Rset"]["UseR"].GetValue<MenuList>().Index;
        private static int UseRKillable => ChampionMenu["Rset"]["UseRKillable"].GetValue<MenuList>().Index;
        private static int UseRHits => ChampionMenu["Rset"]["RHits"].GetValue<MenuSlider>().Value;
        private static int UseRImp => ChampionMenu["Rset"]["UseRImportant"].GetValue<MenuSlider>().Value;

        private static bool Harass_UseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool Harass_UseW => ChampionMenu["Harass"]["HW"].GetValue<MenuBool>().Enabled;
        private static int Harass_Mana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;
        private static bool AutoHarass => ChampionMenu["Harass"]["AutoH"].GetValue<MenuKeyBind>().Active;

        private static bool LaneClear_UseQ => ChampionMenu["Farm"]["LQ"].GetValue<MenuBool>().Enabled;
        private static bool LaneClear_UseW => ChampionMenu["Farm"]["LW"].GetValue<MenuBool>().Enabled;
        private static bool LaneClear_UseE => ChampionMenu["Farm"]["LE"].GetValue<MenuBool>().Enabled;
        private static int LaneClear_Mana => ChampionMenu["Farm"]["LaneClearManaCheck"].GetValue<MenuSlider>().Value;

        private static bool JungleClear_UseQ => ChampionMenu["JungleFarm"]["UseQJFarm"].GetValue<MenuBool>().Enabled;
        private static bool JungleClear_UseW => ChampionMenu["JungleFarm"]["UseWJFarm"].GetValue<MenuBool>().Enabled;
        private static bool JungleClear_UseE => ChampionMenu["JungleFarm"]["UseEJFarm"].GetValue<MenuBool>().Enabled;

        private static bool DrawQRange => ChampionMenu["Drawings"]["QRange"].GetValue<MenuBool>().Enabled;
        private static bool DrawWRange => ChampionMenu["Drawings"]["WRange"].GetValue<MenuBool>().Enabled;
        private static bool DrawERange => ChampionMenu["Drawings"]["ERange"].GetValue<MenuBool>().Enabled;
        private static bool DrawRRange => ChampionMenu["Drawings"]["RRange"].GetValue<MenuBool>().Enabled;
        private static bool DrawBallRange => ChampionMenu["Drawings"]["QOnBallRange"].GetValue<MenuBool>().Enabled;

        private static bool SheildE => ChampionMenu["Misc"]["SheildE"].GetValue<MenuBool>().Enabled;
        private static int AutoW_ifHits => ChampionMenu["Misc"]["AutoW"].GetValue<MenuSlider>().Value;
        private static bool AutoEInitiators => ChampionMenu["Misc"]["AutoEInitiators"].GetValue<MenuBool>().Enabled;
        private static bool RInterrupt => ChampionMenu["Misc"]["InterruptSpells"].GetValue<MenuBool>().Enabled;
        #endregion

        #region 初始化
        public Orianna()
        {
            Q = new Spell(SpellSlot.Q, 825f);
            W = new Spell(SpellSlot.W, 225f);
            E = new Spell(SpellSlot.E, 1120f);
            R = new Spell(SpellSlot.R, 400f);

            Q.SetSkillshot(0f, 80f, 1400f, false, SpellType.Line);
            W.SetSkillshot(0f, 250f, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(0f, 80f, 1700f, false, SpellType.Line);
            R.SetSkillshot(0.5f, 375f, float.MaxValue, false, SpellType.Circle);
            OnMenuLoad();
            MissileManager.Initialize();
            Game.OnUpdate += OnGameUpdate;
            Render.OnEndScene += OnDraw;
            AIBaseClient.OnDoCast += OnProcessSpellCast;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
            AntiGapcloser.OnAllGapcloser += OnAllGapcloser;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Orianna));
            var combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                combo.Add(new MenuBool("CQ", "Use Q"));
                combo.Add(new MenuBool("CW", "Use W"));
                combo.Add(new MenuBool("CE", "Use E"));
            }
            var Rset = ChampionMenu.Add(new Menu("Rset", Program.Chinese ? "指令:冲击波" : "R Set"));
            {
                Rset.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.OriannaR));
                Rset.Add(new MenuList("UseR", "Use R", new string[] { "Only Combo", "Always", "Disable" }, 0));
                Rset.Add(new MenuList("UseRKillable", Program.Chinese ? "对可击杀的敌人使用R" : "对可击杀的敌人使用R", new string[] { "Always", "Only 1v1", "Disable" }, 1));
                Rset.Add(new MenuSlider("RHits", Program.Chinese ? "R至少打中 X 个敌人" : "R min HitCount", 3, 2, 5));
                Rset.Add(new MenuSlider("UseRImportant", Program.Chinese ? "-> 或者目标优先级 >= X(请在目标选择器中修改权重 6为放弃此选项)" : "Or target Priority >= X (In TargetSelector Change Priority. 6 is give up)", 5, 1, 6)); // 5 for e.g adc's
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HW", "Use W", false));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "当蓝量 <= x%时不骚扰" : "Don't Harass if Mana <= X%", 40, 0, 100));
                Harass.Add(new MenuKeyBind("AutoH", "Auto Harass", Keys.Y, KeyBindType.Toggle)).AddPermashow();
            }
            var Farm = ChampionMenu.Add(new Menu("Farm", "LaneClear"));
            {
                Farm.Add(new MenuBool("LQ", "Use Q"));
                Farm.Add(new MenuBool("LW", "Use W"));
                Farm.Add(new MenuBool("LE", "Use E", false));
                Farm.Add(new MenuSlider("LaneClearManaCheck", Program.Chinese ? "当蓝量 <= x%时不清线野" : "Don't Lane/Jung if Mana <= X%", 40, 0, 100));
            }
            var JungleFarm = ChampionMenu.Add(new Menu("JungleFarm", "JungleClear"));
            {
                JungleFarm.Add(new MenuBool("UseQJFarm", "Use Q"));
                JungleFarm.Add(new MenuBool("UseWJFarm", "Use W"));
                JungleFarm.Add(new MenuBool("UseEJFarm", "Use E"));
            }
            var Drawings = ChampionMenu.Add(new Menu("Drawings", "Draw"));
            {
                Drawings.Add(new MenuBool("QRange", "Draw Q"));
                Drawings.Add(new MenuBool("WRange", "Draw W"));
                Drawings.Add(new MenuBool("ERange", "Draw E"));
                Drawings.Add(new MenuBool("RRange", "Draw R"));
                Drawings.Add(new MenuBool("QOnBallRange", "Draw Ball"));
            }
            var Misc = ChampionMenu.Add(new Menu("Misc", "Misc"));
            {
                Misc.Add(new MenuBool("SheildE", Program.Chinese ? "受击自动E保护自己" : "Auto E Protect Me"));
                Misc.Add(new MenuSlider("AutoW", Program.Chinese ? "如果能打中x个人则自动释放W" : "Auto W if Can Hit >= X", 2, 1, 5));
                Misc.Add(new MenuBool("AutoEInitiators", Program.Chinese ? "自动释放E如果队友突进到人堆里" : "Auto E if Enemy is Gap"));
                Misc.Add(new MenuBool("InterruptSpells", Program.Chinese ? "使用R打断技能" : "Use R Interrupt"));
                var InterruptList = Misc.Add(new Menu("InterruptList", "Interrupt List"));
                {
                    foreach (var obj in GameObjects.EnemyHeroes)
                    {
                        InterruptList.Add(new MenuBool("rupt." + obj.CharacterName, obj.CharacterName));
                    }
                }
            }
        }
        #endregion

        #region 类方法hook
        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || OriannaBallManager.BallPosition == Vector3.Zero)
            {
                return;
            }
            Q.From = OriannaBallManager.BallPosition;
            Q.RangeCheckFrom = Player.ServerPosition;
            W.From = OriannaBallManager.BallPosition;
            W.RangeCheckFrom = OriannaBallManager.BallPosition;
            E.From = OriannaBallManager.BallPosition;
            R.From = OriannaBallManager.BallPosition;
            R.RangeCheckFrom = OriannaBallManager.BallPosition;
            if (AutoHarass && Orbwalker.ActiveMode != OrbwalkerMode.Combo)
            {
                Harass();
            }
            RLogic();
            AutoWLogic();
            if (E.IsReady() && SheildE)
            {

                if (MissileManager.MissileWillHitMyHero)
                {
                    E.CastOnUnit(Player);
                }
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    if (!AutoHarass)
                    {
                        Harass();
                    }
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
        private void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!RInterrupt)
            {
                return;
            }

            if (args.DangerLevel != Interrupter.DangerLevel.High)
            {
                return;
            }

            if (sender == null || sender.IsAlly)
            {
                return;
            }
            var CanInterrupt = ChampionMenu["Misc"]["InterruptList"]["rupt." + sender.CharacterName].GetValue<MenuBool>();
            if (CanInterrupt != null && CanInterrupt.Enabled)
            {
                if (R.IsReady())
                {
                    Q.Cast(sender);
                    if (OriannaBallManager.BallPosition.DistanceSquared(sender.ServerPosition) < R.Range * R.Range && ((Game.Time + 1.5 + R.Delay) >= args.EndTime))
                    {
                        R.Cast(Player.ServerPosition);
                    }
                }
            }
        }
        private void OnAllGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (!AutoEInitiators || !E.IsReady())
            {
                return;
            }
            if (!(sender is AIHeroClient))
            {
                return;
            }
            if (args.Type == AntiGapcloser.GapcloserType.UnknowDash)
            {
                return;
            }
            if (sender.IsAlly && E.IsInRange(sender) && sender.CountEnemyHerosInRangeFix(1000) > 0)
            {
                E.CastOnUnit(sender);
            }
        }
        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if(sender.IsEnemy && SheildE && E.IsReady())
            {
                if (args.Target != null && args.Target.IsMe && (sender.Type == GameObjectType.AIHeroClient || sender.Type == GameObjectType.AITurretClient))
                {
                    if (!Orbwalker.IsAutoAttack(args.SData.Name) || sender.IsMelee)
                    {
                        E.CastOnUnit(Player);
                    }
                }
            }
        }
        private void OnDraw(EventArgs args)
        {
            
            if (DrawQRange && Q.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, Q.Range, Color.DodgerBlue);
            }

            if (DrawWRange && W.IsReady())
            {

                PlusRender.DrawCircle(OriannaBallManager.BallPosition, W.Range, Color.Peru);
            }

            if (DrawERange && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Green);
            }

            if (DrawRRange && R.IsReady())
            {
                PlusRender.DrawCircle(OriannaBallManager.BallPosition, R.Range, Color.Red);
            }

            if (DrawBallRange)
            {
                var WorldToScreen = Drawing.WorldToScreen(OriannaBallManager.BallPosition);
                MyDraw(WorldToScreen, 50);
            }
        }
        #endregion

        #region Methods
        private void AutoWLogic()
        {
            if (W.IsReady())
            {
                CastW(AutoW_ifHits);
            }
        }
        private Vector2 Rotate72(Vector2 pt, Vector2 center)
        {
            int x = (int)(center.X + (pt.X - center.X) * Math.Cos(72.0 * Math.PI / 180) - (pt.Y - center.Y) * Math.Sin(72.0 * Math.PI / 180)),
            y = (int)(center.Y + (pt.X - center.X) * Math.Sin(72.0 * Math.PI / 180) + (pt.Y - center.Y) * Math.Cos(72.0 * Math.PI / 180));
            return new Vector2(x, y);
        }
        private void MyDraw(Vector2 center, int radius)
        {
            Vector2[] pts = new Vector2[5];
            //获取五角星5个顶点
            pts[0] = new Vector2(center.X, center.Y - radius);
            pts[1] = Rotate72(pts[0], center);
            pts[2] = Rotate72(pts[1], center);
            pts[3] = Rotate72(pts[2], center);
            pts[4] = Rotate72(pts[3], center);
            //简单地拉5条线
            Drawing.DrawLine(pts[0], pts[2], 3, System.Drawing.Color.Yellow);
            Drawing.DrawLine(pts[0], pts[3], 3, System.Drawing.Color.Yellow);
            Drawing.DrawLine(pts[1], pts[3], 3, System.Drawing.Color.Yellow);
            Drawing.DrawLine(pts[1], pts[4], 3, System.Drawing.Color.Yellow);
            Drawing.DrawLine(pts[2], pts[4], 3, System.Drawing.Color.Yellow);
        }
        private bool CanCastSpell(Spell spl,AIHeroClient obj)
        {
            if (HealthPrediction.GetPrediction(obj, (int)(spl.Delay * 1000)) <= 0 || (obj.HealthPercent <= 10 && obj.CountAllysHerosInRangeFix(400f) - 1 >= 1))
            {
                return false;
            }
            return true;
        }
        private bool CastR(int minTargets, bool prioriy = false)
        {
            if (GetHits(R).Item1 >= minTargets || prioriy && GetHits(R)
                    .Item2.Any(
                        hero =>
                            (int)TargetSelector.GetPriority(hero) >= UseRImp))
            {
                R.Cast(Player.ServerPosition);
                return true;
            }

            return false;
        }
        private void RLogic()
        {
            if (UseR == 2 || !R.IsReady())
            {
                return;
            }
            if ((UseR == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || UseR == 1)
            {
                //总是对可击杀的英雄放R
                var EnemiesInQR = Player.CountEnemyHerosInRangeFix((int)(Q.Range + R.Width));
                if (UseRKillable != 2 && (UseRKillable == 0 || (EnemiesInQR <= 1 && UseRKillable == 1)))
                {
                    //遍历在球体附近的英雄
                    foreach (var obj in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(R.Range,true, OriannaBallManager.BallPosition)))
                    {
                        var HealthPreds = HealthPrediction.GetPrediction(obj, 500);
                        //浪费技能的角色 直接越过循环
                        if (!CanCastSpell(R,obj))
                        {
                            continue;
                        }
                        if (GetComboDamage(obj) < HealthPreds)
                        {
                            continue;
                        }
                        var predmove = Prediction.GetPrediction(obj, R.Delay);
                        if(predmove != null && (predmove.Hitchance >= HitChance.High || predmove.CastPosition.IsValid()))
                        if (predmove.CastPosition.DistanceSquared(OriannaBallManager.BallPosition) <= R.Width * R.Width)
                        {
                            R.Cast(obj.ServerPosition);
                            break;
                        }
                    }
                }
                CastR(UseRHits, true);
            }
        }
        private Tuple<int, List<AIHeroClient>> GetEHits(Vector3 to)
        {
            var hits = new List<AIHeroClient>();
            var oldERange = E.Range;
            E.Range = 10000; //avoid the range check
            foreach (var enemy in Cache.EnemyHeroes.Where(h => h.NewIsValidTarget(2000)))
            {
                if (E.WillHit(enemy, to))
                {
                    hits.Add(enemy);
                }
            }
            E.Range = oldERange;
            return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
        }
        private bool CastE(AIHeroClient target, int minTargets)
        {
            if (GetEHits(target.ServerPosition).Item1 >= minTargets)
            {
                E.CastOnUnit(target);
                return true;
            }
            return false;
        }
        private Tuple<int, List<AIHeroClient>> GetHits(Spell spell)
        {
            var hits = new List<AIHeroClient>();
            var range = spell.Range * spell.Range;
            foreach (var enemy in Cache.EnemyHeroes.Where(h => h.NewIsValidTarget() && OriannaBallManager.BallPosition.Distance(h.ServerPosition) < range))
            {
                if(!CanCastSpell(spell, enemy))
                {
                    continue;
                }
                if (spell.WillHit(enemy, OriannaBallManager.BallPosition) && OriannaBallManager.BallPosition.DistanceSquared(enemy.ServerPosition) < (spell.Width * spell.Width))
                {
                    hits.Add(enemy);
                }
            }
            return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
        }
        private bool CastW(int minTargets)
        {
            var hits = GetHits(W);
            if (hits.Item1 >= minTargets)
            {
                W.Cast(Player.ServerPosition);
                return true;
            }
            return false;
        }
        private Tuple<int, Vector3> GetBestQLocation(AIHeroClient mainTarget)
        {
            var points = new List<Vector2>();
            var qPrediction = Q.GetPrediction(mainTarget);
            if (qPrediction.Hitchance < HitChance.VeryHigh)
            {
                return new Tuple<int, Vector3>(1, Vector3.Zero);
            }
            points.Add(qPrediction.UnitPosition.ToVector2());

            foreach (var enemy in Cache.EnemyHeroes.Where(h => h.NewIsValidTarget(Q.Range + R.Range)))
            {
                var prediction = Q.GetPrediction(enemy);
                if (prediction.Hitchance >= HitChance.High)
                {
                    points.Add(prediction.UnitPosition.ToVector2());
                }
            }

            for (int j = 0; j < 5; j++)
            {
                var mecResult = Mec.GetMec(points);

                if (mecResult.Radius < (R.Range - 75) && points.Count >= 3 && R.IsReady())
                {
                    return new Tuple<int, Vector3>(3, mecResult.Center.ToVector3());
                }

                if (mecResult.Radius < (W.Range - 75) && points.Count >= 2 && W.IsReady())
                {
                    return new Tuple<int, Vector3>(2, mecResult.Center.ToVector3());
                }

                if (points.Count == 1)
                {
                    return new Tuple<int, Vector3>(1, mecResult.Center.ToVector3());
                }

                if (mecResult.Radius < Q.Width && points.Count == 2)
                {
                    return new Tuple<int, Vector3>(2, mecResult.Center.ToVector3());
                }

                float maxdist = -1;
                var maxdistindex = 1;
                for (var i = 1; i < points.Count; i++)
                {
                    var distance = Vector2.DistanceSquared(points[i], points[0]);
                    if (distance > maxdist || maxdist.CompareTo(-1) == 0)
                    {
                        maxdistindex = i;
                        maxdist = distance;
                    }
                }
                points.RemoveAt(maxdistindex);
            }

            return new Tuple<int, Vector3>(1, points[0].ToVector3());
        }
        private bool CastQ(AIBaseClient target)
        {
            var qPrediction = Q.GetPrediction(target,true);

            if (qPrediction.Hitchance < HitChance.High)
            {
                return false;
            }
            if (!qPrediction.CastPosition.InRange(Player.ServerPosition, Q.Range)) return false;
            if (!target.IsFacing(Player) && target.Path.Count() >= 1) // target is running
            {
                var targetBehind = Player.ServerPosition.Extend(qPrediction.CastPosition, Player.Distance(target) + (target.MoveSpeed / 2));
                Q.Cast(targetBehind);
                return true;
            }

            Q.Cast(qPrediction.CastPosition);
            return true;
        }
        private void JungleClear()
        {
            var mobs = Cache.GetJungles(Player.ServerPosition, Q.Range).OrderBy(x => x.MaxHealth).Cast<AIBaseClient>().ToList();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                var conditionUseW = JungleClear_UseW && W.IsReady() && W.WillHit(mob.ServerPosition, OriannaBallManager.BallPosition);

                if (conditionUseW)
                {
                    W.Cast(Player.ServerPosition);
                }
                if (JungleClear_UseQ && Q.IsReady())
                {
                    Q.Cast(mob, true);
                }
                if (JungleClear_UseE && E.IsReady() && !conditionUseW)
                {
                    var closestAlly = Cache.AlliesHeroes
                        .Where(h => h.NewIsValidTarget(E.Range, false))
                        .MinOrDefault(h => h.Distance(mob));
                    if (closestAlly != null)
                    {
                        E.CastOnUnit(closestAlly);
                    }
                    else
                    {
                        E.CastOnUnit(Player);
                    }
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear || Player.ManaPercent < LaneClear_Mana)
                return;
            var allMinions = Cache.GetMinions(Player.ServerPosition, Q.Range + W.Width).Cast<AIBaseClient>().ToList();
            var rangedMinions = Cache.GetMinions(Player.ServerPosition, Q.Range + W.Width).Where(x => x.NewIsValidTarget(Q.Range + W.Width) && x.IsRanged).Cast<AIBaseClient>().ToList();

            if (LaneClear_UseQ && Q.IsReady())
            {
                if (LaneClear_UseW)
                {
                    var qLocation = Q.GetCircularFarmLocation(allMinions, W.Range);
                    var q2Location = Q.GetCircularFarmLocation(rangedMinions, W.Range);
                    var bestLocation = (qLocation.MinionsHit > q2Location.MinionsHit + 1) ? qLocation : q2Location;

                    if (bestLocation.MinionsHit > 0)
                    {
                        Q.Cast(bestLocation.Position);
                        return;
                    }
                }
                else
                {
                    foreach (var minion in allMinions.Where(m => !m.InAutoAttackRange()))
                    {
                        if (HealthPrediction.GetPrediction(minion, Math.Max((int)(minion.ServerPosition.Distance(OriannaBallManager.BallPosition) / Q.Speed * 1000) - 100, 0)) < 50)
                        {
                            Q.Cast(minion.ServerPosition);
                            return;
                        }
                    }
                }
            }

            if (LaneClear_UseW && W.IsReady())
            {
                var n = 0;
                var d = 0;
                foreach (var m in allMinions)
                {
                    if (m.Distance(OriannaBallManager.BallPosition) <= W.Range)
                    {
                        n++;
                        if (W.GetDamage(m) > m.GetRealHeath(DamageType.Magical))
                        {
                            d++;
                        }
                    }
                }
                if (n >= 3 || d >= 2)
                {
                    W.Cast(Player.ServerPosition);
                    return;
                }
            }

            if (LaneClear_UseE && E.IsReady())
            {
                if (E.GetLineFarmLocation(allMinions).MinionsHit >= 3)
                {
                    E.CastOnUnit(Player);
                    return;
                }
            }
        }
        private void Harass()
        {
            if (Player.ManaPercent < Harass_Mana)
                return;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target != null)
            {
                if (Harass_UseQ && Q.IsReady())
                {
                    CastQ(target);
                    return;
                }

                if (Harass_UseW && W.IsReady())
                {
                    CastW(1);
                }
            }
        }
        private void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (Combo_UseW && W.IsReady())
            {
                CastW(1);
            }

            var EnemiesInQR = Player.CountEnemyHerosInRangeFix((int)(Q.Range + R.Width));
            if (EnemiesInQR <= 1)
            {
                if (Combo_UseE && E.IsReady())
                {
                    foreach (var ally in Cache.AlliesHeroes.Where(h => h.NewIsValidTarget(E.Range, false)))
                    {
                        if (ally.ServerPosition.CountEnemyHerosInRangeFix(300) >= 1)
                        {
                            E.CastOnUnit(ally);
                        }

                        CastE(ally, 1);
                    }
                }

                if (Combo_UseQ && Q.IsReady())
                {
                    CastQ(target);
                    return;
                }
            }
            else
            {
                if (Combo_UseE && E.IsReady())
                {
                    if (OriannaBallManager.BallPosition.CountEnemyHerosInRangeFix(800) <= 2)
                    {
                        CastE(Player, 1);
                    }
                    else
                    {
                        CastE(Player, 2);
                    }

                    foreach (var ally in Cache.AlliesHeroes.Where(h =>  h.NewIsValidTarget(E.Range, false)))
                    {
                        if (ally.ServerPosition.CountEnemyHerosInRangeFix(300) >= 2)
                        {
                            E.CastOnUnit(ally);
                        }
                    }
                }
                if (!Q.IsReady() && !W.IsReady() && !R.IsReady() && E.IsReady() && Player.HealthPercent < 15 && EnemiesInQR > 0)
                {
                    CastE(Player, 0);
                }

                if (Combo_UseQ && Q.IsReady())
                {
                    var qLoc = GetBestQLocation(target);
                    if (qLoc.Item1 > 1)
                    {
                        Q.Cast(qLoc.Item2);
                        return;
                    }
                    else
                    {
                        CastQ(target);
                        return;
                    }
                }

                
            }

        }
        private float GetComboDamage(AIHeroClient target)
        {
            var result = 0f;
            if (Q.IsReady())
            {
                result += 2 * Q.GetDamage(target);
            }

            if (W.IsReady())
            {
                result += W.GetDamage(target);
            }

            if (R.IsReady())
            {
                result += R.GetDamage(target);
            }

            result += 2 * (float)Player.GetAutoAttackDamage(target);

            return result;
        }
        #endregion

        private class OriannaBallManager
        {
            public static Vector3 BallPosition { get; private set; }
            private static int _sTick = Variables.GameTimeTickCount;

            static OriannaBallManager()
            {
                BallPosition = Player.Position;
                Game.OnUpdate += Game_OnGameUpdate;
                AIBaseClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
            }

            static void AIBaseClientProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                if (sender.IsMe)
                {
                    switch (args.SData.Name)
                    {
                        case "OrianaIzunaCommand":
                            BallPosition = args.To;
                            _sTick = Variables.GameTimeTickCount;
                            break;

                        case "OrianaRedactCommand":
                            BallPosition = Vector3.Zero;
                            _sTick = Variables.GameTimeTickCount;
                            break;
                    }
                }
            }

            static void Game_OnGameUpdate(EventArgs args)
            {
                if (Variables.GameTimeTickCount - _sTick > 300 && Player.HasBuff("orianaghostself"))
                {
                    BallPosition = Player.Position;
                }

                foreach (var ally in Cache.AlliesHeroes)
                {
                    if (ally.HasBuff("orianaghost"))
                    {
                        BallPosition = ally.Position;
                    }
                }
            }
        }
    }
}
