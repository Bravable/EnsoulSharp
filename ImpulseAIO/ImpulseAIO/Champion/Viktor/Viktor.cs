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

namespace ImpulseAIO.Champion.Viktor
{

    internal class Viktor : Base
    {
        private static Spell Q, W, E,R;

        private static readonly int maxRangeE = 1200;
        private static readonly int lengthE = 700;
        private static readonly int SpeedE = 1050;
        private static readonly int rangeE = 525;
        private static bool thorwQWait = false;
        private static AIBaseClient KillQTarget = null;
        private static int lasttick = 0;
        private static Menu AntiGapcloserMenu;

        private struct NewFarmLocation
        {
            public int MinionsHit;
            public Vector2 Position1;
            public Vector2 Position2;
            public NewFarmLocation(Vector2 startpos, Vector2 endpos, int minionsHit)
            {
                Position1 = startpos;
                Position2 = endpos;
                MinionsHit = minionsHit;
            }
        }

        #region 菜单选项
        private static bool Combo_UseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static int Combo_WMode => ChampionMenu["Combo"]["CW"].GetValue<MenuList>().Index;
        private static bool Combo_UseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static int Combo_RMode => ChampionMenu["Combo"]["CR"].GetValue<MenuList>().Index;
        private static int Combo_Rheal => ChampionMenu["Combo"]["cRheal"].GetValue<MenuSlider>().Value;
        private static int Combo_RTick => ChampionMenu["Combo"]["cRtick"].GetValue<MenuSlider>().Value;
        private static int Combo_Rhits => ChampionMenu["Combo"]["CRCount"].GetValue<MenuSlider>().Value;
        private static bool wasteR => ChampionMenu["Combo"]["wasteR"].GetValue<MenuBool>().Enabled;
        private static bool Combo_AutoFollowR => ChampionMenu["Combo"]["CRFlow"].GetValue<MenuBool>().Enabled;
        private static bool Harass_UseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool Harass_UseE => ChampionMenu["Harass"]["HE"].GetValue<MenuBool>().Enabled;
        private static int Harass_Mana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;

        private static bool LaneClear_UseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static bool LaneClear_UseE => ChampionMenu["LaneClear"]["LE"].GetValue<MenuBool>().Enabled;
        private static int LaneClear_EHits => ChampionMenu["LaneClear"]["LECount"].GetValue<MenuSlider>().Value;
        private static int LaneClear_Mana => ChampionMenu["LaneClear"]["LMana"].GetValue<MenuSlider>().Value;
        private static bool JungleClear_UseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleClear_UseE => ChampionMenu["JungleClear"]["JE"].GetValue<MenuBool>().Enabled;
        private static bool Killsteal_q => ChampionMenu["Killsteal"]["KQ"].GetValue<MenuBool>().Enabled;
        private static bool Killsteal_e => ChampionMenu["Killsteal"]["KE"].GetValue<MenuBool>().Enabled;

        private static bool DisableAA => ChampionMenu["Misc"]["DisableAA"].GetValue<MenuBool>().Enabled;
        private static int DisableAAlvl => ChampionMenu["Misc"]["DisableAALvL"].GetValue<MenuSlider>().Value;
        private static bool InterruptW => ChampionMenu["Misc"]["InterruptW"].GetValue<MenuBool>().Enabled;
        private static bool InterruptR => ChampionMenu["Misc"]["InterruptR"].GetValue<MenuBool>().Enabled;
        private static bool AutoWCC => ChampionMenu["Misc"]["autoW"].GetValue<MenuBool>().Enabled;
        private static bool DrawQ => ChampionMenu["Draw"]["DrawQ"].GetValue<MenuBool>().Enabled;
        private static bool DrawW => ChampionMenu["Draw"]["DrawW"].GetValue<MenuBool>().Enabled;
        private static bool DrawE => ChampionMenu["Draw"]["DrawE"].GetValue<MenuBool>().Enabled;
        private static bool DrawMaxE => ChampionMenu["Draw"]["DrawMaxE"].GetValue<MenuBool>().Enabled;
        private static bool DrawR => ChampionMenu["Draw"]["DrawR"].GetValue<MenuBool>().Enabled;

        private static bool AntiGap => AntiGapcloserMenu["AntiEGap"].GetValue<MenuBool>().Enabled;
        private static bool AttacksEnabled
        {
            get
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    return ((!Q.IsReady() || Player.Mana < Q.Instance.ManaCost) && (!E.IsReady() || Player.Mana < E.Instance.ManaCost) && (!DisableAA || Player.HasBuff("viktorpowertransferreturn")));
                }
                return true;
            }
        }
        #endregion

        #region 初始
        public Viktor()
        {
            Q = new Spell(SpellSlot.Q, 652f);
            Q.SetTargetted(0.25f, 2000f);

            W = new Spell(SpellSlot.W, 800f);
            W.SetSkillshot(0.4f, 300f, float.MaxValue, false, SpellType.Circle);

            E = new Spell(SpellSlot.E, 525f);
            E.SetSkillshot(0f, 90f, SpeedE, false, SpellType.Line);

            R = new Spell(SpellSlot.R, 700f);
            R.SetSkillshot(0.6f, 450f, float.MaxValue, false, SpellType.Circle);

            OnMenuLoad();
            Game.OnUpdate += Game_OnUpdate;

            AIBaseClient.OnDoCast += (s, g) => {
                if (s.IsMe)
                {
                    if(g.SData.Name == "ViktorPowerTransfer")
                    {
                        thorwQWait = true;
                    }
                }
            };
            AIBaseClient.OnProcessSpellCast += (s, g) => {
                if (s.IsMe)
                {
                    if (g.SData.Name == "ViktorPowerTransferReturn")
                    {
                        thorwQWait = false;
                        KillQTarget = null;
                    }
                }
            };
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
            Teleport.OnTeleport += OnTeleport;
            Interrupter.OnInterrupterSpell += OnInterruptSpell;
            AntiGapcloser.OnGapcloser += AntiGapCloser;
            Orbwalker.OnNonKillableMinion += NonKillable;
            Render.OnEndScene += OnDraw;
        }
        private void OnMenuLoad()
        {
            
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Viktor));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuList("CW", "Use W", new string[] { "Always", "Only Slow/CC", "With R", "从不" }, 1));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuList("CR", "Use R", new string[] { "Health <= X%", "Killable", "Disable" }, 1));
                Combo.Add(new MenuSlider("cRheal", Program.Chinese ? "当敌人血量低于X%使用R" : "Use R if Enemy health <= X%", 40, 0, 100));
                Combo.Add(new MenuSlider("cRtick", Program.Chinese ? "多计算X次R伤害层数" : "Extra R Count", 3, 1, 6));
                Combo.Add(new MenuSlider("CRCount", Program.Chinese ? "使用 R 最少击中X个敌人" : "R min HitCount", 3, 1, 5));
                Combo.Add(new MenuBool("CRFlow", Program.Chinese ? "自动跟随R" : "Auto Follow"));
                Combo.Add(new MenuBool("wasteR", Program.Chinese ? "节约r" : "waste R"));
            }
            var blackList = ChampionMenu.Add(new Menu("BlackList", "R BlackList"));
            foreach (var obj in Cache.EnemyHeroes)
            {
                blackList.Add(new MenuBool("blacklist." + obj.CharacterName, obj.CharacterName, false));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HE", "Use E"));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "蓝量 <= X时不骚扰" : "Don't Use Spell Harass if Mana <= X%", 60, 0, 100));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuBool("LE", "Use E"));
                LaneClear.Add(new MenuSlider("LECount", Program.Chinese ? "如果 E 能打中 x 个敌人" : "Min E HitCount", 3, 1, 6));
                LaneClear.Add(new MenuSlider("LMana", Program.Chinese ? "蓝量 <= X时不清线" : "Don't LaneClear/JungleClear if Mana <= X%", 40, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Use Q"));
                JungleClear.Add(new MenuBool("JE", "Use E"));
            }
            var Killsteal = ChampionMenu.Add(new Menu("Killsteal", "Killable"));
            {
                Killsteal.Add(new MenuBool("KQ", "Use Q"));
                Killsteal.Add(new MenuBool("KE", "Use E"));
            }
            var Misc = ChampionMenu.Add(new Menu("Misc", "Misc"));
            {
                Misc.Add(new MenuBool("DisableAA", Program.Chinese ? "连招模式中关闭普攻" : "Disable AA if Orb - Combo Active", false));
                Misc.Add(new MenuSlider("DisableAALvL", Program.Chinese ? "^-在等级 >= X级后关闭" : "-> Level >= X disable AA", 12, 1, 18));
                Misc.Add(new MenuBool("InterruptW", Program.Chinese ? "使用W打断技能" : "Auto W Interrupt Spell"));
                Misc.Add(new MenuBool("InterruptR", Program.Chinese ? "使用R打断技能" : "Auto R Interrupt Spell"));
                Misc.Add(new MenuBool("autoW", Program.Chinese ? "自动W衔接控制" : "Auto W CC"));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "绘图设置"));
            {
                Draw.Add(new MenuBool("DrawQ", "Q Range", false));
                Draw.Add(new MenuBool("DrawW", "W Range", false));
                Draw.Add(new MenuBool("DrawE", "E Range", false));
                Draw.Add(new MenuBool("DrawMaxE", "MaxE Range", false));
                Draw.Add(new MenuBool("DrawR", "R Range", false));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("AntiEGap", "Use W AntiGapCloser"));
            }
        }
        #endregion

        #region 类方法HOOK
        private void NonKillable(object s,NonKillableMinionEventArgs args)
        {
            if((Orbwalker.ActiveMode == OrbwalkerMode.LaneClear || Orbwalker.ActiveMode == OrbwalkerMode.LastHit) && Q.IsReady() && Enable_laneclear && LaneClear_UseQ && Player.ManaPercent > LaneClear_Mana)
            {
                var target = args.Target as AIBaseClient;
                if (target == null) return;

                if (Q.GetHealthPrediction(target) > 0 &&
                        Q.GetHealthPrediction(target) < Q.GetDamage(target))
                {
                    Q.CastOnUnit(target);
                }
            }
        }
        private void AntiGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if(AntiGap && W.IsReady())
            {
                if (sender.IsEnemy)
                {
                    if (e.StartPosition.DistanceToPlayer() > e.EndPosition.DistanceToPlayer() && e.EndPosition.DistanceToPlayer() <= W.Range)
                    {
                        W.Cast(e.EndPosition);
                    }
                }
            }
        }
        private void OnInterruptSpell(AIHeroClient sender,Interrupter.InterruptSpellArgs args)
        {
            if(sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High)
            {
                if(InterruptW && W.IsReady() && sender.NewIsValidTarget(W.Range) &&
                    (Game.Time + 1.5 + W.Delay) >= args.EndTime)
                {
                    W.Cast(sender.ServerPosition);
                    return;
                }
                else if (InterruptR && R.IsReady() && sender.NewIsValidTarget(R.Range) && R.Instance.Name == "ViktorChaosStorm")
                {
                    R.Cast(sender.ServerPosition);
                }
            }
        }
        private void OnTeleport(AIBaseClient sender,Teleport.TeleportEventArgs args)
        {
            if (AutoWCC && W.IsReady())
            {
                if (args.Type != Teleport.TeleportType.Teleport || args.Status != Teleport.TeleportStatus.Start)
                {
                    return;
                }
                if (!sender.IsEnemy)
                {
                    return;
                }
                if (W.IsInRange(sender))
                {
                    W.Cast(sender.ServerPosition);
                }
            }
        }
        private void OnBeforeAttack(object s ,BeforeAttackEventArgs args)
        {
            if (DisableAA)
            {
                if (Player.Level >= DisableAAlvl && args.Target is AIHeroClient)
                {
                    args.Process = AttacksEnabled;
                }
            }
            else
            {
                args.Process = true;
            }
        }
        private void OnDraw(EventArgs args)
        {
            if(DrawQ && Q.IsReady())
            {

                PlusRender.DrawCircle(Player.Position, Q.Range, Color.BurlyWood);
            }
            if (DrawW && W.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, W.Range, Color.Red);
            }
            if (DrawE && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, 525f, Color.DarkViolet);
            }
            if (DrawMaxE && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, maxRangeE, Color.Yellow);
            }
            if (DrawR && R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, R.Range, Color.Orange);
            }
        }
        private void Game_OnUpdate(EventArgs args)
        {
            FollowR();
            AutoKill();
            AutoWCCLogic();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    Laneclear();
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        #endregion

        #region Methods
        private bool IsKillalbeQTarget(AIBaseClient unit)
        {
            if(unit == null || KillQTarget == null || !thorwQWait || KillQTarget.NetworkId != unit.NetworkId)
            {
                return false;
            }
            return true;
        }
        
        private void AutoWCCLogic()
        {
            if(AutoWCC && W.IsReady())
            {
                foreach (var obj in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(W.Range)))
                {
                    var ccPos = GetCCBuffPos(obj);
                    if (ccPos != Vector3.Zero)
                    {
                        W.Cast(ccPos);
                        break;
                    }
                }
            }
        }
        private void FollowR()
        {
            // Ultimate follow
            if (R.Instance.Name != "ViktorChaosStorm" && Combo_AutoFollowR && Variables.TickCount - lasttick > 0)
            {
                var stormT = TargetSelector.GetTarget(1100, DamageType.Magical);
                if (stormT != null)
                {
                    R.Cast(stormT.ServerPosition);
                    lasttick = Variables.TickCount + 500;
                }
            }
        }
        private void AutoKill()
        {
            foreach (var obj in Cache.EnemyHeroes.Where(x => x.IsEnemy && x.NewIsValidTarget()))
            {
                if (Killsteal_q && Q.IsReady())
                {
                    if (Q.IsInRange(obj) && Q.GetDamage(obj) > obj.GetRealHeath(DamageType.Magical))
                    {
                        Q.Cast(obj);
                        KillQTarget = obj;
                        return;
                    }
                }
                if (Killsteal_e && E.IsReady() && obj.GetRealHeath(DamageType.Magical) < GetEDmg(obj))
                {
                    PredictCastE(obj);
                }
            }
        }
        private NewFarmLocation GetBestLaserFarmLocation(bool jungle)
        {
            var bestendpos = new Vector2();
            var beststartpos = new Vector2();
            var minionCount = 0;
            List<AIBaseClient> allminions;
            var minimalhit = LaneClear_EHits;

            if (!jungle)
            {
                allminions = Cache.GetMinions(Player.ServerPosition, maxRangeE);
            }
            else
            {
                allminions = Cache.GetJungles(Player.ServerPosition, maxRangeE);
            }

            var minionslist = from mnion in allminions select mnion.ServerPosition.ToVector2();
            var posiblePositions = new List<Vector2>();
            posiblePositions.AddRange(minionslist);
            var max = posiblePositions.Count;
            for (var i = 0; i < max; i++)
            {
                for (var j = 0; j < max; j++)
                {
                    if (posiblePositions[j] != posiblePositions[i])
                    {
                        posiblePositions.Add((posiblePositions[j] + posiblePositions[i]) / 2);
                    }
                }
            }

            foreach (var startposminion in allminions.Where(m => Player.Distance(m) < rangeE))
            {
                var startPos = startposminion.ServerPosition.ToVector2();

                foreach (var pos in posiblePositions)
                {
                    if (pos.Distance(startPos) <= lengthE * lengthE)
                    {
                        var endPos = startPos + lengthE * (pos - startPos).Normalized();

                        var count =
                            minionslist.Count(pos2 => pos2.Distance(startPos, endPos, true) <= 140 * 140);

                        if (count >= minionCount)
                        {
                            bestendpos = endPos;
                            minionCount = count;
                            beststartpos = startPos;
                        }

                    }
                }
            }
            if ((!jungle && minimalhit < minionCount) || (jungle && minionCount > 0))
            {
                return new NewFarmLocation(beststartpos, bestendpos, minionCount);
            }
            else
            {
                return new NewFarmLocation(beststartpos, bestendpos, 0);
            }
        }
        private bool PredictCastMinionE(bool jungle)
        {
            var farmLoc = GetBestLaserFarmLocation(jungle);
            if (farmLoc.MinionsHit >= LaneClear_EHits)
            {
                CastE(farmLoc.Position1.ToVector3World(), farmLoc.Position2.ToVector3World());
                return true;
            }

            return false;
        }
        private void CastE(Vector3 source, Vector3 destination)
        {
            E.Cast(source, destination);
        }
        private void PredictCastE(AIHeroClient objs = null)
        {
            var target = objs ?? TargetSelector.GetTarget(maxRangeE, DamageType.Magical);
            if (target == null)
                return;

            //是否在标准E范围内
            var inRange = target.DistanceSquared(Player) < E.Range * E.Range;
            PredictionOutput prediction;
            var spellCasted = false;
            Vector3 pos1, pos2;

            var nearChamps = (from champ in Cache.EnemyHeroes where champ.NewIsValidTarget(maxRangeE) && target != champ select champ).ToList();
            var innerChamps = new List<AIHeroClient>();
            var outerChamps = new List<AIHeroClient>();

            foreach (var champ in nearChamps)
            {
                if (champ.DistanceSquared(Player) < E.Range * E.Range)
                    innerChamps.Add(champ);
                else
                    outerChamps.Add(champ);
            }

            var nearMinions = Cache.GetMinions(Player.ServerPosition, maxRangeE);
            var innerMinions = new List<AIBaseClient>();
            var outerMinions = new List<AIBaseClient>();
            foreach (var minion in nearMinions)
            {
                if (minion.DistanceSquared(Player) < E.Range * E.Range)
                    innerMinions.Add(minion);
                else
                    outerMinions.Add(minion);
            }

            if (inRange)
            {
                //标准E范围内
                E.From = target.ServerPosition + ((Player.ServerPosition - target.ServerPosition).Normalized() * (lengthE * 0.1f));
                prediction = E.GetPrediction(target, true);
                E.From = Player.ServerPosition;

                if (prediction.CastPosition.Distance(Player.ServerPosition) < E.Range)
                    pos1 = prediction.CastPosition;
                else
                {
                    pos1 = target.ServerPosition;
                }

                E.From = pos1;
                E.RangeCheckFrom = pos1;
                E.Range = lengthE;

                if (nearChamps.Count > 0)
                {
                    var closeToPrediction = new List<AIHeroClient>();
                    foreach (var enemy in nearChamps)
                    {
                        prediction = E.GetPrediction(enemy, true);
                        if (prediction.Hitchance >= HitChance.High && pos1.DistanceSquared(prediction.CastPosition) < (E.Range * E.Range) * 0.8)
                            closeToPrediction.Add(enemy);
                    }

                    if (closeToPrediction.Count > 0)
                    {
                        if (closeToPrediction.Count > 1)
                            closeToPrediction.Sort((enemy1, enemy2) => enemy2.GetRealHeath(DamageType.Magical).CompareTo(enemy1.GetRealHeath(DamageType.Magical)));

                        prediction = E.GetPrediction(closeToPrediction[0], true);
                        pos2 = prediction.CastPosition;

                        CastE(pos1, pos2);
                        spellCasted = true;
                    }
                }

                if (!spellCasted)
                {
                    CastE(pos1, E.GetPrediction(target).CastPosition);
                }

                E.Range = rangeE;
                E.From = Player.ServerPosition;
                E.RangeCheckFrom = Player.ServerPosition;
            }
            else
            {
                float startPointRadius = 150;

                Vector3 startPoint = Player.ServerPosition + (target.ServerPosition - Player.ServerPosition).Normalized() * rangeE;

                var targets = (from champ in nearChamps where champ.DistanceSquared(startPoint) < startPointRadius * startPointRadius && Player.DistanceSquared(champ) < rangeE * rangeE select champ).ToList();
                if (targets.Count > 0)
                {
                    if (targets.Count > 1)
                        targets.Sort((enemy1, enemy2) => enemy2.GetRealHeath(DamageType.Magical).CompareTo(enemy1.GetRealHeath(DamageType.Magical)));

                    pos1 = targets[0].ServerPosition;
                }
                else
                {

                    var minionTargets = (from minion in nearMinions where minion.DistanceSquared(startPoint) < startPointRadius * startPointRadius && Player.DistanceSquared(minion) < rangeE * rangeE select minion).ToList();
                    if (minionTargets.Count > 0)
                    {
                        if (minionTargets.Count > 1)
                            minionTargets.Sort((enemy1, enemy2) => enemy2.GetRealHeath(DamageType.Magical).CompareTo(enemy1.GetRealHeath(DamageType.Magical)));

                        pos1 = minionTargets[0].ServerPosition;
                    }
                    else
                        pos1 = startPoint;
                }

                E.From = pos1;
                E.Range = lengthE;
                E.RangeCheckFrom = pos1;
                prediction = E.GetPrediction(target, true);

                if (prediction.Hitchance >= HitChance.High)
                    CastE(pos1, prediction.CastPosition);

                E.Range = rangeE;
                E.From = Player.ServerPosition;
                E.RangeCheckFrom = Player.ServerPosition;
            }
        }
        private void CastW(AIHeroClient unit = null)
        {
            var target = unit ?? TargetSelector.GetTarget(W.Range, DamageType.Magical);
            if (target != null)
            {
                if (Combo_WMode == 0)
                {
                    
                    var prds = W.GetPrediction(target, true);
                    if (prds.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(prds.CastPosition);
                        return;
                    }
                }
                if (Combo_WMode == 1)
                {
                    if (target.Path.Count() < 2)
                    {
                        if (target.HasBuffOfType(BuffType.Slow))
                        {
                            var prds = W.GetPrediction(target, true);
                            if (prds.Hitchance >= HitChance.Medium)
                            {
                                W.Cast(prds.CastPosition);
                                return;
                            }
                        }
                    }
                }
            }
        }
        private float GetRDmg(AIHeroClient unit)
        {
            if (unit == null)
                return 0f;

            var RBaseDmg = 100 + (R.Level - 1) * 75;
            var ExtraDmg = Player.TotalMagicalDamage * 0.5;
            var TickDmg = 65 + (R.Level - 1) * 40;
            var CountDmg = TickDmg * Combo_RTick;
            return (float)Player.CalculateMagicDamage(unit, RBaseDmg + ExtraDmg + CountDmg);
        }
        private float GetEDmg(AIBaseClient unit)
        {
            if (unit == null)
                return 0f;
            var BaseDmg = 70 + (E.Level - 1) * 40;
            var ExtraDmg = Player.TotalMagicalDamage * 0.5;
            return (float)Player.CalculateMagicDamage(unit, BaseDmg + ExtraDmg);
        }
        private bool UnitIsBlock(AIHeroClient unit)
        {
            return ChampionMenu["BlackList"]["blacklist." + unit.CharacterName].GetValue<MenuBool>().Enabled;
        }
        private bool CanCastR(AIHeroClient unit)
        {
            if (!wasteR)
            {
                return true;
            }
            if (!R.IsInRange(unit))
            {
                return false;
            }
            if (Q.IsReady() && (Combo_UseQ || Killsteal_q))
            {
                if((Q.IsInRange(unit) && Q.GetDamage(unit) > unit.GetRealHeath(DamageType.Magical)) || IsKillalbeQTarget(unit))
                {
                    return false;
                }
            }
            if (E.IsReady() && (Combo_UseE || Killsteal_e))
            {
                if (unit.DistanceSquared(Player) < maxRangeE * maxRangeE && GetEDmg(unit) > unit.GetRealHeath(DamageType.Magical))
                {
                    return false;
                }
            }
            if(HealthPrediction.GetPrediction(unit,250) <= 0)
            {
                return false;
            }
            if(unit.HealthPercent <= 10 && unit.CountAllysHerosInRangeFix(400f) - 1 >= 1)
            {
                return false;
            }
            return true;
        }
        private void LogicRKillable()
        {
            var min_HealthTarget = TargetSelector.GetTargets(R.Range, DamageType.Magical);
            foreach (var obj in min_HealthTarget)
            {
                if (obj.CountEnemyHerosInRangeFix(300) >= Combo_Rhits)
                {
                    if (Combo_WMode == 2 && W.IsReady() && W.IsInRange(obj))
                    {
                        var Preds = Prediction.GetPrediction(obj, 0.4f);
                        if (Preds.CastPosition.IsValid())
                        {
                            W.Cast(Preds.CastPosition);
                        }
                    }
                    var rprds = R.GetPrediction(obj, true);
                    if (rprds.Hitchance >= HitChance.High)
                    {
                        R.Cast(rprds.CastPosition);
                    }
                }
                if (UnitIsBlock(obj))
                {
                    continue;
                }
                if (Combo_RMode == 0)
                {
                    if (obj.HealthPercent <= Combo_Rheal)
                    {
                        var Preds = Prediction.GetPrediction(obj, R.Delay);
                        if (Preds.CastPosition.IsValid())
                        {
                            R.Cast(Preds.CastPosition);
                        }
                    }
                    return;
                }
                if (Combo_RMode == 1)
                {
                    //如果血量足够 而且目标周围的友军小于2

                    if (obj.GetRealHeath(DamageType.Magical) <= GetRDmg(obj) && CanCastR(obj))
                    {
                        var Preds = Prediction.GetPrediction(obj, R.Delay);
                        if (Preds.CastPosition.IsValid())
                        {
                            R.Cast(Preds.CastPosition);
                        }
                    }
                }
            }
        }
        private void Combo()
        {
            if (Combo_WMode != 3 && W.IsReady())
            {
                CastW();
            }
            if (Combo_UseE && E.IsReady())
            {
                PredictCastE();
            }
            if (Combo_RMode != 2 && R.IsReady())
            {
                LogicRKillable();
            }
            if (Combo_UseQ && Q.IsReady())
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range + 75f, DamageType.Magical);
                if (Qtarget != null && Qtarget.IsValidTarget(Q.Range + Qtarget.BoundingRadius))
                {
                    Q.CastOnUnit(Qtarget);
                    if (Qtarget.GetRealHeath(DamageType.Magical) < Q.GetDamage(Qtarget))
                    {
                        KillQTarget = Qtarget;
                    }
                }
            }

        }
        private void Harass()
        {
            if (Player.ManaPercent <= Harass_Mana)
                return;

            if (Harass_UseQ && Q.IsReady())
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (qtarget != null)
                    Q.Cast(qtarget);
            }
            if (Harass_UseE && E.IsReady())
            {
                PredictCastE();
            }
        }
        private void Laneclear()
        {
            if (Player.ManaPercent <= LaneClear_Mana || !Enable_laneclear)
                return;
            if (LaneClear_UseQ && Q.IsReady())
            {
                foreach (var minion in Cache.GetMinions(Player.ServerPosition, Q.Range))
                {
                    if (Q.GetHealthPrediction(minion) > 0 && 
                        Q.GetHealthPrediction(minion) < Q.GetDamage(minion) && 
                        (!Player.Spellbook.IsWindingUp || Orbwalker.CanAttack()))
                    {
                        Q.CastOnUnit(minion);
                        break;
                    }
                }
            }

            if (LaneClear_UseE && E.IsReady())
            {
                PredictCastMinionE(false);
            }
        }
        private void JungleClear()
        {
            if (Player.ManaPercent <= LaneClear_Mana || !Enable_laneclear)
                return;
            if (JungleClear_UseQ && Q.IsReady())
            {
                var junglsFirst = Cache.GetJungles(Player.ServerPosition, Q.Range).FirstOrDefault();
                if (junglsFirst != null)
                {
                    Q.Cast(junglsFirst);
                }
            }
            if (JungleClear_UseE && E.IsReady())
            {
                PredictCastMinionE(true);
            }
        }
        #endregion
    }
}
