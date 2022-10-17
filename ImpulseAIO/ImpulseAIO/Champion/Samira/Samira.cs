using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;

using System.Windows.Forms;

using SharpDX;
using ImpulseAIO.Common;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;
using Keys = EnsoulSharp.SDK.MenuUI.Keys;

namespace ImpulseAIO.Champion.Samira
{
    internal class Samira : Base
    {
        private static Spell ConeQ, LineQ, W, E, R;
        private static SpellSlot LastSpell = SpellSlot.Unknown;
        private static float lastaa = 0;
        private static int waitAAA;
        private static bool ActivePacket = false;
        private static int WaitCastE = 0;

        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool CWOnlyHaveR => ChampionMenu["Combo"]["CWOnlyHaveR"].GetValue<MenuBool>().Enabled;
        private static bool Ekey => ChampionMenu["Combo"]["Ekey"].GetValue<MenuKeyBind>().Active;
        private static bool CEUnderTower => ChampionMenu["Combo"]["CEUnderTower"].GetValue<MenuKeyBind>().Active;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseR => ChampionMenu["Combo"]["CR"].GetValue<MenuBool>().Enabled;
        private static int ComboUseRCount => ChampionMenu["Combo"]["CRCount"].GetValue<MenuSlider>().Value;
        private static bool ComboUseRKill => ChampionMenu["Combo"]["CROnlyKill"].GetValue<MenuBool>().Enabled;
        private static bool UsePacket => ChampionMenu["Combo"]["UsePacket"].GetValue<MenuBool>().Enabled;

        public static bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        public static int HarassUseQMagic => ChampionMenu["Harass"]["HQMagic"].GetValue<MenuSlider>().Value;

        public static bool LaneCluerUseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        public static int LaneCluerUseQMagic => ChampionMenu["LaneClear"]["LQMagic"].GetValue<MenuSlider>().Value;
        public static bool JungleCluerUseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;

        public static bool LastHitUseQ => ChampionMenu["LastHit"]["LhQ"].GetValue<MenuBool>().Enabled;
        public static int LastHitMagic => ChampionMenu["LastHit"]["LhQMagic"].GetValue<MenuSlider>().Value;
        public static bool DQ => ChampionMenu["Drawing"]["DQ"].GetValue<MenuBool>().Enabled;
        public static bool DE => ChampionMenu["Drawing"]["DE"].GetValue<MenuBool>().Enabled;
        public static bool DR => ChampionMenu["Drawing"]["DR"].GetValue<MenuBool>().Enabled;
        public static bool DDmg => ChampionMenu["Drawing"]["DDmg"].GetValue<MenuBool>().Enabled;

        public static bool AfterAttack
        {
            get
            {
                var AttackendTime = lastaa + (Player.AttackDelay * 1000);
                var XianTime = Game.Time * 1000;
                var emmT = AttackendTime - XianTime;
                if (emmT >= emmT * 0.7)
                {
                    return true;
                }
                return false;
            }
        }
        public Samira()
        {
            ConeQ = new Spell(SpellSlot.Q, 375f);
            LineQ = new Spell(SpellSlot.Q, 950f);
            ConeQ.SetSkillshot(0.25f, 45f, float.MaxValue, false, SpellType.Cone);
            LineQ.SetSkillshot(0.25f, 60f, 2600f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 390f);
            E = new Spell(SpellSlot.E, 600f);
            E.SetTargetted(0f, 500f);
            R = new Spell(SpellSlot.R, 600f);
            ConeQ.MinHitChance = LineQ.MinHitChance = HitChance.High;
            OnMenuLoad();
            new EvadeTarget().Init();
            Game.OnUpdate += Game_OnUpdate;
            Spellbook.OnCastSpell += OnActivePassPacket;
            Game.OnProcessPacket += OnByPassPacket;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCastOnLastSpell;
            AIBaseClient.OnProcessSpellCast += OnCastSpell;
            Render.OnEndScene += OnDraw;
        }  
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Samira));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("UsePacket", "Use Packet Mode"));
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuBool("CWOnlyHaveR", Program.Chinese ? "->仅当大招即将就绪时使用W" : "Use W Only have R",false));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuKeyBind("CEUnderTower", Program.Chinese ? "使用 E 越塔" : "Use E UnderTower",Keys.A,KeyBindType.Toggle)).AddPermashow();
                Combo.Add(new MenuKeyBind("Ekey", Program.Chinese ? "快速向指定方向E" : "Fast E to MousePos",Keys.T,KeyBindType.Press)).AddPermashow();
                Combo.Add(new MenuBool("CR", "Use R"));
                Combo.Add(new MenuSlider("CRCount", Program.Chinese ? "^-当周围敌军 >= X时" : "Count enemy >= X", 2, 1, 5));
                Combo.Add(new MenuBool("CROnlyKill", Program.Chinese ? "^-当可击杀英雄时使用" : "killable R"));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuSlider("HQMagic", Program.Chinese ? "^-当蓝量 < X% 时不用Q骚扰" : "Don't Harass if mana <= X%", 30, 0, 100));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuSlider("LQMagic", Program.Chinese ? "^-当蓝量 < X% 时不用Q清线野" : "Dont lane/jungle clear if mana <= X%", 30, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Use Q"));
            }
            var LastHit = ChampionMenu.Add(new Menu("LastHit", "LastHit"));
            {
                LastHit.Add(new MenuBool("LhQ", "Use Q").SetValue(true));
                LastHit.Add(new MenuSlider("LhQMagic", Program.Chinese ? "^-当蓝量 < X% 时 不用Q尾刀" : "Dont lasthit if mana <= X%", 20, 0, 100));
            }
            var Drawing = ChampionMenu.Add(new Menu("Drawing", "Draw"));
            {
                Drawing.Add(new MenuBool("DQ", "Draw Q"));
                Drawing.Add(new MenuBool("DE", "Draw E"));
                Drawing.Add(new MenuBool("DR", "Draw R"));
                Drawing.Add(new MenuBool("DDmg", "Draw Dmg",false));
            }
        }
        private void Game_OnUpdate(EventArgs args)
        {
            Check();
            castekey();
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
                    LastHit();
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnProcessSpellCastOnLastSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.Slot)
                {
                    case SpellSlot.Q:
                        LastSpell = SpellSlot.Q;
                        break;
                    case SpellSlot.W:
                        LastSpell = SpellSlot.W;
                        break;
                    case SpellSlot.E:
                        Game.SendEmote(EmoteId.Joke);
                        LastSpell = SpellSlot.E;
                        break;
                    case SpellSlot.R:
                        Game.SendEmote(EmoteId.Joke);
                        LastSpell = SpellSlot.R;
                        break;
                    default:
                        if (Orbwalker.IsAutoAttack(args.SData.Name) && args.Target is AIHeroClient)
                        {
                            
                            lastaa = Game.Time * 1000;
                            LastSpell = SpellSlot.Item1;
                            if (GetPassiveStack() == Passive.Non)
                            {
                                waitAAA = Variables.GameTimeTickCount + 150;
                            }
                        }
                        break;

                }
            }
        }
        private void OnDraw(EventArgs args)
        {
            if(DQ && LineQ.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, LineQ.Range, Color.Green);
            }
            if (DE && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Orange);
            }
            if (DR && CheckRReady())
            {
                PlusRender.DrawCircle(Player.Position, R.Range, Color.Red);
            }

            if (!DDmg)
                return;

            foreach (var enemy in Cache.EnemyHeroes.Where(e => e.IsValidTarget(R.Range + 500)))
            {
                var dmg = GetComboDamage(enemy,true);
                if (dmg <= 0f)
                    continue;
                int HpBarLeftX = (int)enemy.HPBarPosition.X - 45;
                int HpBarLeftY = (int)enemy.HPBarPosition.Y - 25;
                int HpBarHeight = 13;
                int HPBarTotalLength = ((int)enemy.HPBarPosition.X - HpBarLeftX) * 2 + 16;
                var DamageCeiling = dmg / enemy.GetRealHeath(DamageType.Physical);
                DamageCeiling = Math.Min(DamageCeiling, 1);
                int FixedHPBarLength = (int)(DamageCeiling * HPBarTotalLength);
                PlusRender.DrawRect(HpBarLeftX, HpBarLeftY, FixedHPBarLength, HpBarHeight, new Color((int)Color.Cornsilk.R, (int)Color.Cornsilk.G, (int)Color.Cornsilk.B, 100));
            }
        }
        private void OnByPassPacket(GamePacketEventArgs args)
        {
            if (args.NetworkId == Player.NetworkId)
                return;
            var Data = new GamePacket(args.PacketData);
            if (ActivePacket && Data.Header == 56)
            {
                args.Process = false;
                return;
            }
        }
        private void OnCastSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.E)
            {
                var w = args.Target as AIHeroClient;
                if (w == null || !w.IsValid)
                {
                    return;
                }
                DelayAction.Add(100, () => ConeQ.Cast(Player.ServerPosition));
                return;
            }
        }
        private void OnActivePassPacket(Spellbook s,SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.W && UsePacket)
            {
                ActivePacket = true;
                DelayAction.Add(750, () => {  ActivePacket = false; });
            }
        }
        private void Check()
        {
            if (GetPassiveStack() == Passive.Non && !AfterAttack)
            {
                LastSpell = SpellSlot.Unknown;
            }
            Orbwalker.AttackEnabled = !(Player.HasBuff("SamiraW") || Player.Spellbook.IsChanneling || Player.HasBuff("SamiraR"));
            if (Player.HasBuff("SamiraR"))
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
        }
        private bool CheckRReady()
        {
            if (!R.Instance.Learned)
                return false;
            if (R.CooldownTime <= 2)
                return true;
            return false;
        }
        private void LaneClear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= LaneCluerUseQMagic)
                return;
            if (LaneCluerUseQ && ConeQ.IsReady() && !Player.IsWindingUp)
            {
                var Minions = Cache.GetMinions(Player.ServerPosition, ConeQ.Range).Where(y => y.IsValidTarget(ConeQ.Range));
                if(Minions != null || Minions.Count() != 0)
                {
                    var farmLocation = ConeQ.GetCircularFarmLocation(Minions);
                    if (farmLocation.MinionsHit >= 2)
                    {
                        ConeQ.Cast(farmLocation.Position);
                        return;
                    }
                        
                }

                var preds = Cache.GetMinions(Player.Position, LineQ.Range).Where(i => LineQ.GetHealthPrediction(i) > 0 && LineQ.GetHealthPrediction(i) <= LineQ.GetDamage(i)
                && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.Health > Player.GetAutoAttackDamage(i))).Select(y => LineQ.GetPrediction(y, false, -1, new CollisionObjects[] { CollisionObjects.Minions })).Where(i => i.Hitchance >= HitChance.High && i.CastPosition.DistanceToPlayer() <= LineQ.Range).ToList();
                if (preds.Count > 0)
                {
                    LineQ.Cast(preds.FirstOrDefault().CastPosition);
                }
            }
        }
        private void LastHit()
        {
            if (LastHitUseQ && Player.ManaPercent > LastHitMagic)
            {
                
                var minion =
                        Cache.GetMinions(Player.ServerPosition, LineQ.Range).Where(
                            i =>
                            i.IsValidTarget(LineQ.Range)
                            && LineQ.GetHealthPrediction(i) > 0 && LineQ.GetHealthPrediction(i) <= GetQDmg(i)
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.GetRealHeath(DamageType.Physical) > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                if (minion != null)
                {
                    var pred = LineQ.GetPrediction(minion,false, -1,new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions });
                    if (pred.Hitchance >= HitChance.Medium && LineQ.Cast(pred.CastPosition))
                    {
                        return;
                    }
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= LaneCluerUseQMagic)
                return;
            if (JungleCluerUseQ && ConeQ.IsReady())
            {
                var Minions = Cache.GetJungles(Player.ServerPosition, LineQ.Range).Where(y => y.IsValidTarget(LineQ.Range));
                if (Minions != null)
                {
                    foreach (var min in Minions)
                    {
                        if (LineQ.IsReady())
                        {
                            LineQ.Cast(min.Position);
                        }
                    }
                }
            }
        }
        private void Harass()
        {
            if (HarassUseQ && HarassUseQMagic < Player.ManaPercent)
            {
                if (LineQ.IsReady() && !Player.Spellbook.IsWindingUp)
                {
                    var targets = TargetSelector.GetTargets(LineQ.Range, DamageType.Physical);
                    if (targets.Count != 0)
                    {
                        var preds =
                        targets.Select(i => LineQ.GetPrediction(i, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions }))
                            .Where(
                                i =>
                                i.Hitchance >= HitChance.High && i.CastPosition.DistanceToPlayer() <= LineQ.Range)
                            .ToList();
                        if (preds.Count > 0)
                        {
                            LineQ.Cast(preds.MaxOrDefault(i => i.Hitchance).CastPosition);
                        }
                    }
                }
            }
        }
        private void Combo()
        {
            if (Player.Spellbook.IsWindingUp && CanCastThisSpell(SpellSlot.Item1))
            {
                return;
            }
            if (ComboUseW && W.IsReady() && CanCastThisSpell(SpellSlot.W) && (!CWOnlyHaveR || CheckRReady()))
            {
                //如果层数没有到S  或者 有A评级 可以放W R也好了
                if(GetPassiveStack() != Passive.S && 
                    (GetPassiveStack() == Passive.A && CanCastThisSpell(SpellSlot.W) && CheckRReady() && !CanCastThisSpell(SpellSlot.Item1) ||
                     GetPassiveStack() > Passive.Non && LineQ.IsReady() && E.IsReady() ||
                     GetPassiveStack() == Passive.B && !LineQ.IsReady() && !CanCastThisSpell(SpellSlot.Item1) ||
                     GetPassiveStack() >= Passive.D && CanCastThisSpell(SpellSlot.E)))
                {
                    if (W.IsReady() && Player.Mana > E.Instance.ManaCost + W.Instance.ManaCost)
                    {
                        if (HasAnyHeroInWRange())
                        {
                            W.Cast();
                            WaitCastE = Variables.TickCount + 100;
                        }
                    }
                }
            }
            if (ComboUseE && E.IsReady())
            {
                if (!(LineQ.IsReady() && GetPassiveStack() == Passive.Non))
                {
                    if (Variables.TickCount > WaitCastE && CanCastThisSpell(SpellSlot.E))
                    {
                        var target = GetSamiraBestETarget();
                        if (target.IsValidTarget())
                        {
                            E.CastOnUnit(target);
                        }
                    }
                }
            }
            if (ComboUseQ && LineQ.IsReady() && !Player.IsDashing() && Variables.GameTimeTickCount > waitAAA)
            {
                var targetQ = TargetSelector.GetTarget(ConeQ.Range, DamageType.Physical);
                if (targetQ.IsValidTarget())
                {
                    if (CanCastThisSpell(SpellSlot.Q) && (AfterAttack || LastSpell == SpellSlot.Item1 || (GetPassiveStack() == Passive.A && LastSpell == SpellSlot.E)))
                    {
                        var CastPred = Prediction.GetPrediction(targetQ, 0.25f);
                        if(CastPred != null && CastPred.CastPosition.IsValid())
                        {
                            ConeQ.Cast(CastPred.CastPosition);
                        }
                    }
                }
                else
                {
                    targetQ = TargetSelector.GetTarget(LineQ.Range, DamageType.Physical);
                    if (targetQ.IsValidTarget())
                    {
                        if (targetQ.InAutoAttackRange()) //如果目标在我AA范围内时
                        {
                            if (CanCastThisSpell(SpellSlot.Q) && (AfterAttack || LastSpell == SpellSlot.Item1 || (GetPassiveStack() == Passive.A && LastSpell == SpellSlot.E)))
                            {
                                var Qpred = LineQ.GetPrediction(targetQ, false, -1,new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.YasuoWall });
                                if (Qpred.Hitchance >= HitChance.High)
                                {
                                    LineQ.Cast(Qpred.CastPosition);
                                }
                            }
                        }
                        else //目标不在AA范围内时
                        {
                            var Qpred = LineQ.GetPrediction(targetQ,false,-1,new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.YasuoWall });
                            if (Qpred.Hitchance >= HitChance.High)
                            {
                                LineQ.Cast(Qpred.CastPosition);
                            }
                        }
                    }
                }
            }
           
            if (ComboUseR && CheckRReady() && GetPassiveStack() == Passive.S)
            {
                if (Player.CountEnemyHerosInRangeFix(R.Range) >= ComboUseRCount)
                {
                    R.Cast();
                }
                if (ComboUseRKill)
                {
                    var heros = Cache.EnemyHeroes.Where(x =>  x.IsValidTarget(R.Range)).Where(x => x.GetRealHeath(DamageType.Physical) < GetRDmg(x)).FirstOrDefault();
                    if (heros.IsValidTarget())
                    {
                        R.Cast();
                    }
                }
            }
        }
        private AIBaseClient GetSamiraBestETarget()
        {
            //获取最佳收割对象
            var herolist = Cache.EnemyHeroes.Where(x => x.IsValidTarget() && !x.InFountain()).ToList();
            var CanEObj = GameObjects.Get<AIBaseClient>().Where(x => x.IsValid && x.IsEnemy && x.IsTargetable && x.DistanceToPlayer() <= E.Range).ToList();

            if (Player.HasBuff("SamiraR"))
            {
                //自己有R buff时  E小兵或者英雄->接近敌人->R打死
                var BestKiller = herolist.Where(x => x.IsValidTarget(R.Range,true,PosAfterE(x))).OrderBy(x => x.DistanceToPlayer());
                foreach(var KILLOBJ in BestKiller)
                {
                    if (E.IsInRange(KILLOBJ) && (CEUnderTower || !PosAfterE(KILLOBJ).IsUnderEnemyTurret()) && (GetComboDamage(KILLOBJ,true) >= KILLOBJ.GetRealHeath(DamageType.Physical) || PosAfterE(KILLOBJ).CountEnemyHerosInRangeFix(R.Range) > 1))
                    {
                        return KILLOBJ;
                    } 
                    var DashObj = CanEObj.Where(x => (CEUnderTower || !PosAfterE(x).IsUnderEnemyTurret()) && PosAfterE(x).Distance(KILLOBJ) <= Player.Distance(KILLOBJ)).Where(x => PosAfterE(x).CountEnemyHerosInRangeFix(R.Range) > 0).MinOrDefault(x => PosAfterE(x).Distance(KILLOBJ));
                    if(DashObj != null)
                    {
                        return DashObj;
                    }
                }
            }
            else
            {
                //(CEUnderTower || !x.IsUnderEnemyTurret())
                var killhero = herolist.Where(x => x.InRange(E.Range + LineQ.Range)).OrderBy(x => x.GetRealHeath(DamageType.Physical));
                foreach(var KILLOBJ in killhero)
                {
                    //目标能被杀死 要么E过去要么踩小兵过去
                    float bounds = W.Range + KILLOBJ.BoundingRadius;
                    if (CheckRReady())
                    {
                        if (Player.HasBuff("SamiraW"))
                        {
                            //E过去变B E完A A完变S
                            if ((CEUnderTower || !PosAfterE(KILLOBJ).IsUnderEnemyTurret()) &&  //检测越塔
                                (PosAfterE(KILLOBJ).CountEnemyHerosInRangeFix(bounds) > 0 || GetPassiveStack() >= Passive.B || GetPassiveStack() >= Passive.D && LineQ.IsReady()||   
                                //如果目标能被连招打死 且突进后在普攻距离内
                                (KILLOBJ.GetRealHeath(DamageType.Physical) <= GetComboDamage(KILLOBJ,true) && PosAfterE(KILLOBJ).Distance(KILLOBJ) <= Player.GetRealAutoAttackRange(KILLOBJ))))
                            {
                                return KILLOBJ;
                            }
                            var DashObjEx = CanEObj.Where(x => (CEUnderTower || !PosAfterE(x).IsUnderEnemyTurret())).FirstOrDefault(x => PosAfterE(x).CountEnemyHerosInRangeFix(bounds) > 0 ||(GetPassiveStack() >= Passive.A && PosAfterE(x).CountEnemyHerosInRangeFix(R.Range) > 1));
                            if (DashObjEx != null)
                            {
                                return DashObjEx;
                            }
                        }
                        else if ((GetPassiveStack() >= Passive.C && W.IsReady()) || (GetPassiveStack() >= Passive.B && LineQ.IsReady()))
                        {
                            if (E.IsInRange(KILLOBJ) && (PosAfterE(KILLOBJ).Distance(KILLOBJ) <= Player.Distance(KILLOBJ) || PosAfterE(KILLOBJ).CountEnemyHerosInRangeFix(bounds) > 0) && (CEUnderTower || !PosAfterE(KILLOBJ).IsUnderEnemyTurret()))
                            {
                                return KILLOBJ;
                            }
                        }
                        else
                        {
                            if (KILLOBJ.GetRealHeath(DamageType.Physical) < GetComboDamage(KILLOBJ,true))
                            {
                                if (E.IsInRange(KILLOBJ) && (PosAfterE(KILLOBJ).Distance(KILLOBJ) <= Player.Distance(KILLOBJ) || PosAfterE(KILLOBJ).Distance(KILLOBJ) <= Player.GetRealAutoAttackRange(KILLOBJ)) && (CEUnderTower || !PosAfterE(KILLOBJ).IsUnderEnemyTurret()))
                                {
                                    return KILLOBJ;
                                }
                                var DashObjEx = CanEObj.Where(x => (PosAfterE(x).Distance(KILLOBJ) <= Player.Distance(KILLOBJ) || PosAfterE(x).Distance(KILLOBJ) <= Player.GetRealAutoAttackRange(KILLOBJ)) && (CEUnderTower || !PosAfterE(x).IsUnderEnemyTurret())).MinOrDefault(x => PosAfterE(x).Distance(KILLOBJ));
                                if (DashObjEx != null)
                                {
                                    return DashObjEx;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Player.HasBuff("SamiraW") && (LineQ.IsReady() || GetPassiveStack() >= Passive.D))
                        {
                            //E过去变B E完A A完变S
                            if ((CEUnderTower || !PosAfterE(KILLOBJ).IsUnderEnemyTurret()) && (PosAfterE(KILLOBJ).CountEnemyHerosInRangeFix(bounds) > 0 || GetPassiveStack() >= Passive.B || LineQ.IsReady() && GetPassiveStack() >= Passive.C))
                            {
                                return KILLOBJ;
                            }
                            var DashObjEx = CanEObj.Where(x => CEUnderTower || !PosAfterE(x).IsUnderEnemyTurret() ).FirstOrDefault(x => PosAfterE(x).CountEnemyHerosInRangeFix(bounds) > 0);
                            if (DashObjEx != null)
                            {
                                return DashObjEx;
                            }
                        }
                        else if(KILLOBJ.GetRealHeath(DamageType.Physical) < GetComboDamage(KILLOBJ,false))
                        {
                            if (E.IsInRange(KILLOBJ) && (PosAfterE(KILLOBJ).Distance(KILLOBJ) <= Player.Distance(KILLOBJ) || PosAfterE(KILLOBJ).Distance(KILLOBJ) <= Player.GetRealAutoAttackRange(KILLOBJ)) && (CEUnderTower || !PosAfterE(KILLOBJ).IsUnderEnemyTurret()))
                            {
                                return KILLOBJ;
                            }
                            var DashObjEx = CanEObj.Where(x => (PosAfterE(x).Distance(KILLOBJ) <= Player.Distance(KILLOBJ) || PosAfterE(x).Distance(KILLOBJ) <= Player.GetRealAutoAttackRange(KILLOBJ)) && (CEUnderTower || !PosAfterE(x).IsUnderEnemyTurret())).MinOrDefault(x => PosAfterE(x).Distance(KILLOBJ));
                            if (DashObjEx != null)
                            {
                                return DashObjEx;
                            }
                        }
                    }
                }
            }
            return null;
        }
        private float GetComboDamage(AIBaseClient w,bool includeR)
        {
            float PhysicalEndDamages = 0f;
            float MagicEndDamages = 0f;
            if (ConeQ.IsReady())
            {
                PhysicalEndDamages += 5f * (ConeQ.Level - 1) + (0.8f + (0.1f * (ConeQ.Level - 1))) * Player.TotalAttackDamage;
            }
            if (E.IsReady())
            {
                MagicEndDamages += 50f + 10f * (E.Level - 1) + 0.2f * (Player.TotalAttackDamage - Player.BaseAttackDamage);
            }
            if (includeR && GetPassiveStack() >= Passive.C && (CheckRReady() || Player.HasBuff("SamiraR")))
            {
                PhysicalEndDamages += (10f * (R.Level - 1)) + (0.5f * Player.TotalAttackDamage) * 10;
            }
            PhysicalEndDamages += (float)Player.GetAutoAttackDamage(w) * 2;
            float EndDamage = (float)Player.CalculatePhysicalDamage(w, PhysicalEndDamages);
            float End_Damage = (float)Player.CalculateMagicDamage(w, MagicEndDamages);
            return EndDamage + End_Damage;
        }
        private float GetQDmg(AIBaseClient w)
        {
            return (float)Player.CalculatePhysicalDamage(w, 5f * (ConeQ.Level - 1) + (0.8f + (0.1f * (ConeQ.Level - 1))) * Player.TotalAttackDamage);
        }
        public bool CanCastThisSpell(SpellSlot w)
        {
            if (LastSpell == SpellSlot.Unknown) 
                return false;
            if(w == SpellSlot.Item1)
            {
                return LastSpell != w;
            }
            if (w != SpellSlot.Item1 && LastSpell != w && w.IsReady())
                return true;
            return false;
        }
        private Passive GetPassiveStack()
        {
            var RCount = R.Instance.IconUsed;
            return (Passive)RCount;
        }
        private float GetRDmg(AIBaseClient w)
        {
            return (float)Player.CalculatePhysicalDamage(w, (10f * (ConeQ.Level - 1)) + (0.5f * Player.TotalAttackDamage) * 10);
        }
        private static Vector3 PosAfterE(AIBaseClient w)
        {
            return
                Player.ServerPosition.Extend(
                    w.ServerPosition,
                     E.Range);
        }
        private bool HasAnyHeroInWRange()
        {
            return Cache.EnemyHeroes.Where(x => x.IsValidTarget() && x.ServerPosition.Distance(Player.ServerPosition) <= W.Range + x.BoundingRadius).Any();
        }
        private void castekey()
        {
            if (Ekey)
            {
                var CanEObj = GameObjects.Get<AIBaseClient>().Where(x => x.IsValid && x.IsEnemy && x.IsTargetable && x.DistanceToPlayer() <= E.Range).ToList();
                var first = CanEObj.Where(x => PosAfterE(x).DistanceToCursor() < Player.DistanceToCursor()).MinOrDefault(x => PosAfterE(x).DistanceToCursor());
                if(first != null)
                {
                    E.CastOnUnit(first);
                }
            }
        }
        private enum Passive
        {
            Non = 0,
            E = 2,
            D = 3,
            C = 4,
            B = 5,
            A = 6,
            S = 7
        }

        internal class EvadeTarget
        {
            #region Static Fields

            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            private static readonly List<SpellData> Spells = new List<SpellData>();

            #endregion

            #region Public Methods and Operators

            private static Menu EnemysMenu;
            public static Menu evadeMenu2, championmenu2;

            public void Init()
            {
                LoadSpellData();

                evadeMenu2 = ChampionMenu.Add(new Menu("EvadeTarget", "锋旋"));
                {
                    evadeMenu2.Add(new MenuBool("W", "使用 W")); //                                    evadeSpells.Add("ETower", new CheckBox("Under Tower", false));
                    evadeMenu2.Add(new MenuBool("BAttack", "平A"));
                    evadeMenu2.Add(new MenuSlider("BAttackHpU", "-> 如果血量 <", 35));
                    evadeMenu2.Add(new MenuBool("CAttack", "暴击"));
                    evadeMenu2.Add(new MenuSlider("CAttackHpU", "-> 如果血量 <", 40));
                    championmenu2 = evadeMenu2.Add(new Menu("EvadeTargetList", "躲避目标"));
                    foreach (
                        var spell in Spells.Where(i => GameObjects.EnemyHeroes.Any(a => a.CharacterName == i.ChampionName)))
                    {
                        EnemysMenu = championmenu2.Add(new Menu("scc" + spell.ChampionName, spell.ChampionName));

                        EnemysMenu.Add(new MenuBool(spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            true));

                        /*championmenu2.Add(new MenuBool(spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            true));*/
                    }
                }
                Game.OnUpdate += OnUpdateTarget;
                GameObject.OnCreate += ObjSpellMissileOnCreate;
                GameObject.OnDelete += ObjSpellMissileOnDelete;
            }

            #endregion

            #region Methods

            private void LoadSpellData()
            {
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ahri",
                        SpellNames = new[] { "ahriwdamagemissileback1", "ahriwdamagemissilefront1", "ahriwdamagemissileright1" },
                        Slot = SpellSlot.W
                    }
                    );
                Spells.Add(
                    new SpellData
                    { ChampionName = "Ahri", SpellNames = new[] { "ahritumblemissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Akshan", SpellNames = new[] { "akshanrmissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Annie", SpellNames = new[] { "annieq" }, Slot = SpellSlot.Q });

                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Brand",
                        SpellNames = new[] { "brandr", "brandrmissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Caitlyn",
                        SpellNames = new[] { "caitlynrmissile" },
                        Slot = SpellSlot.R
                    });

                Spells.Add(
                    new SpellData { ChampionName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ezreal",
                        SpellNames = new[] { "ezrealemissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "FiddleSticks",
                        SpellNames = new[] { "fiddlesticksqmissilefear" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Gangplank", SpellNames = new[] { "gangplankqproceed" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData { ChampionName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Katarina",
                        SpellNames = new[] { "katarinaq", "katarinaqdaggerarc" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Kindred",
                        SpellNames = new[] { "kindrede" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Leblanc",
                        SpellNames = new[] { "leblancq", "leblancrq" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Lillia",
                        SpellNames = new[] { "lilliarexpungemissile" },
                        Slot = SpellSlot.R
                    });
                //-------------
                Spells.Add(new SpellData { ChampionName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Malphite", SpellNames = new[] { "seismicshard" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "MissFortune",
                        SpellNames = new[] { "missfortunericochetshot", "missfortunershotextra" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Nami",
                        SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ryze",
                        SpellNames = new[] { "ryzee" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrarspell" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Tristana", SpellNames = new[] { "tristanae" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Tristana", SpellNames = new[] { "tristanar" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Urgot",
                        SpellNames = new[] { "urgotrrecastmissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Veigar", SpellNames = new[] { "veigarr" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
            }

            private void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }

                var unit = missile.SpellCaster as AIHeroClient;
                if (unit == null || !unit.IsValid || unit.Team == Player.Team)
                {
                    return;
                }

                var spellData =
                Spells.FirstOrDefault(
                    i =>
                    i.SpellNames.Contains(missile.SData.Name.ToLower())
                    && championmenu2["scc" + i.ChampionName][i.MissileName] != null && championmenu2["scc" + i.ChampionName][i.MissileName].GetValue<MenuBool>().Enabled);

                if (spellData == null //MenuManager.LaneClearMenu["E"].Cast<CheckBox>().CurrentValue
                    && (!missile.SData.Name.ToLower().Contains("crit")
                            ? evadeMenu2["BAttack"].GetValue<MenuBool>().Enabled
                              && Player.HealthPercent < evadeMenu2["BAttackHpU"].GetValue<MenuSlider>().Value
                            : evadeMenu2["CAttack"].GetValue<MenuBool>().Enabled
                              && Player.HealthPercent < evadeMenu2["CAttackHpU"].GetValue<MenuSlider>().Value))
                {
                    spellData = new SpellData
                    { ChampionName = unit.CharacterName, SpellNames = new[] { missile.SData.Name } };
                }
                if (spellData == null || (missile.Target != null && !missile.Target.IsMe))
                {
                    return;
                }
                DetectedTargets.Add(new Targets { Start = unit.Position, Obj = missile });
            }

            private void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }
                var caster = missile.SpellCaster as AIHeroClient;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team)
                {
                    return;
                }

                DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);

            }

            private void OnUpdateTarget(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                if (!W.IsReady(200))
                {
                    return;
                }
                foreach (var target in
                    DetectedTargets.Where(i => Player.Distance(i.Obj.Position) < 700))
                {
                    //如果有风墙阻挡的话
                    if (Collisions.HasYasuoWindWallCollision(target.Obj.Position, Player.ServerPosition))
                    {
                        continue;
                    }
                    if (W.IsReady() && evadeMenu2["W"].GetValue<MenuBool>().Enabled && W.IsInRange(target.Obj.Position))
                    {
                        W.Cast();
                    }
                }
            }

            #endregion

            private class SpellData
            {
                #region Fields

                public string ChampionName;

                public SpellSlot Slot;

                public string[] SpellNames = { };

                #endregion

                #region Public Properties

                public string MissileName
                {
                    get
                    {
                        return this.SpellNames.FirstOrDefault();
                    }
                }

                #endregion
            }

            private class Targets
            {
                #region Fields

                public MissileClient Obj;

                public Vector3 Start;

                #endregion
            }
        }
    }
}
