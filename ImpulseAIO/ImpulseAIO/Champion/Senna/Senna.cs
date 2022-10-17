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
namespace ImpulseAIO.Champion.Senna
{
    class BarrelsInfo
    {
        public AIBaseClient Pointer { get; set; }
        public int VaildTime { get; set; }
    }
    internal class Senna : Base
    {
        private static Spell Q, ExtraDmgQ,ExtraHealQ,W, E,HealthR,DamageR;
        private static List<BarrelsInfo> Barrels = new List<BarrelsInfo>();
        #region 菜单选项
        private int ComboOrbMode => ChampionMenu["Combo"]["Target"].GetValue<MenuList>().Index;
        private bool Combo_UseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private bool Combo_ExtraQ => ChampionMenu["Combo"]["CQExtra"].GetValue<MenuBool>().Enabled;
        private bool Combo_W => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private int UseRMode => ChampionMenu["Rset"]["UseR"].GetValue<MenuList>().Index;
        private bool UseRHealAlly => ChampionMenu["Rset"]["RHealAlly"].GetValue<MenuBool>().Enabled;
        private int RHealPerfe => ChampionMenu["Rset"]["RHealPerfe"].GetValue<MenuSlider>().Value;
        private int RHealEnemyCount => ChampionMenu["Rset"]["RhealEnemyCount"].GetValue<MenuSlider>().Value;
        private bool UsePredHealR => ChampionMenu["Rset"]["UsePredHealth"].GetValue<MenuBool>().Enabled;
        private bool UseKillableR => ChampionMenu["Rset"]["CRKillable"].GetValue<MenuBool>().Enabled;
        private int UseKillableRCountMeEnemy => ChampionMenu["Rset"]["RCountEnemyForMe"].GetValue<MenuSlider>().Value;
        private int UseQHealAlly => ChampionMenu["Heal"]["UseQHeal"].GetValue<MenuList>().Index;
        private int QHealPerfe => ChampionMenu["Heal"]["QHealPerfe"].GetValue<MenuSlider>().Value;
        private bool FastHeal => ChampionMenu["Heal"]["QHealKey"].GetValue<MenuKeyBind>().Active;

        private bool Harass_UseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private bool Harass_UseExtraQ => ChampionMenu["Harass"]["HQExtra"].GetValue<MenuBool>().Enabled;
        private int Harass_Mana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;
        private bool AutoHarass => ChampionMenu["Harass"]["autoHarass"].GetValue<MenuKeyBind>().Active;

        private bool Laneclear_UseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private int Laneclear_UseQCountEnemy => ChampionMenu["LaneClear"]["LQCount"].GetValue<MenuSlider>().Value;
        private int Laneclear_Mana => ChampionMenu["LaneClear"]["LMana"].GetValue<MenuSlider>().Value;
        private bool Laneclear_Pick => ChampionMenu["LaneClear"]["AutoPick"].GetValue<MenuBool>().Enabled;

        private bool JungleClear_UseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private bool JungleClear_UseW => ChampionMenu["JungleClear"]["JW"].GetValue<MenuBool>().Enabled;

        private bool DrawQ => ChampionMenu["Draw"]["DrawQ"].GetValue<MenuBool>().Enabled;
        private bool DrawQExtra => ChampionMenu["Draw"]["DrawQExtra"].GetValue<MenuBool>().Enabled;
        private bool DrawW => ChampionMenu["Draw"]["DrawW"].GetValue<MenuBool>().Enabled;
        private bool DrawBarrel => ChampionMenu["Draw"]["DrawPick"].GetValue<MenuBool>().Enabled;
        #endregion

        #region 初始化
        public Senna()
        {
            Q = new Spell(SpellSlot.Q, 600f + Player.BoundingRadius);
            Q.SetTargetted(0.325f, float.MaxValue);
            ExtraDmgQ = new Spell(SpellSlot.Q, 1300f);
            ExtraDmgQ.SetSkillshot(0.325f, 50f, float.MaxValue, false, SpellType.Line);
            ExtraHealQ = new Spell(SpellSlot.Q, 1300f);
            ExtraHealQ.SetSkillshot(0.325f, 140f, float.MaxValue, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 1200f);
            W.SetSkillshot(0.25f, 70f, 1200f, true, SpellType.Line);
            E = new Spell(SpellSlot.E, 400f);
            HealthR = new Spell(SpellSlot.R, float.MaxValue);
            DamageR = new Spell(SpellSlot.R, float.MaxValue);

            DamageR.SetSkillshot(1f, 160f, 20000f, false, SpellType.Line);
            HealthR.SetSkillshot(1f, 1200f, 20000f, false, SpellType.Line);
            OnMenuLoad();
            InitBarrelArray();
            Common.BaseUlt.BaseUlt.Initialize(ChampionMenu, DamageR);
            Game.OnUpdate += Game_OnUpdate;
            Render.OnEndScene += OnDraw;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Senna));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CQExtra", "Use Extend Q"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuList("Target", Program.Chinese ? "技能/普攻目标权重" : "Orb Mode", new string[] { "Passive", "Default" })).AddPermashow();
            }
            var Rset = ChampionMenu.Add(new Menu("Rset", Program.Chinese ? "暗影燎原" : "R"));
            {
                Rset.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.SennaR));
                Rset.Add(new MenuList("UseR", "Use R", new string[] { "Always", "Only Combo", "Disable" })).AddPermashow();
                Rset.Add(new MenuBool("RHealAlly", !Program.Chinese ? "Use R Help Low HP Ally" : "使用R救治残血友军"));
                Rset.Add(new MenuSlider("RHealPerfe", Program.Chinese ? "-> 当友军生命比例 <= X" : "if ally hp <= X%", 15, 0, 100));
                Rset.Add(new MenuSlider("RhealEnemyCount", Program.Chinese ? "-> 当友军周围敌人数 >= X" : "if ally count enemy >= X", 1, 0, 5));
                Rset.Add(new MenuBool("UsePredHealth", Program.Chinese ? "-> 使用血量伤害预测" : " use Health Prediction"));
                var HealAllyList = Rset.Add(new Menu("HealthList", "R Black List"));
                {
                    foreach (var obj in GameObjects.AllyHeroes)
                    {
                        HealAllyList.Add(new MenuBool("not." + obj.CharacterName, obj.CharacterName, false));
                    }
                }
                Rset.Add(new MenuBool("CRKillable", Program.Chinese ? "使用R击杀残血英雄" : "Use R Killable"));
                Rset.Add(new MenuSlider("RCountEnemyForMe", Program.Chinese ? "当周围人数 <= X时释放" : "when count enemy <= X", 1, 0, 5));
            }
            var Heal = ChampionMenu.Add(new Menu("Heal", Program.Chinese ? "治疗设置" : "Heal set"));
            {
                Heal.Add(new MenuList("UseQHeal", "Use Q Heal", new string[] { "Always", "Only Combo", "Disable" })).AddPermashow();
                Heal.Add(new MenuSlider("QHealPerfe", Program.Chinese ? "-> 当友军生命比例 <= X" : "When Ally HP <= X%", 15, 0, 100));
                Heal.Add(new MenuKeyBind("QHealKey", Program.Chinese ? "快速治疗(无视血量比例 治疗血量最低友军 若没有友军则Q眼)" : "Fast Heal", Keys.A, KeyBindType.Press)).AddPermashow();
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HQExtra", "Use Extend Q"));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "当蓝量 <= x%时不骚扰" : "Don't Harass if Mana <= X%", 40, 0, 100));
                Harass.Add(new MenuKeyBind("autoHarass", "Auto Harass", Keys.T, KeyBindType.Toggle)).AddPermashow();
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuSlider("LQCount", "Use Q Min HitCount Minion", 2, 1, 6));
                LaneClear.Add(new MenuSlider("LMana", Program.Chinese ? "当蓝量 <= x%时不清线" : "Don't LaneClear if Mana <= X%", 40,0,100));
                LaneClear.Add(new MenuBool("AutoPick", "Auto Pick Barrel"));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Draw Q"));
                JungleClear.Add(new MenuBool("JW", "Draw W"));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("DrawQ", "Draw Q"));
                Draw.Add(new MenuBool("DrawQExtra", "Draw Extend Q"));
                Draw.Add(new MenuBool("DrawW", "Draw W"));
                Draw.Add(new MenuBool("DrawPick", "Draw Barrel"));
            }
        }
        private void InitBarrelArray()
        {
            foreach (var minion in GameObjects.Get<AIBaseClient>().Where(minion => minion.IsValid))
            {
                Game_OnObjectCreate(minion, null);
            }
            GameObject.OnCreate += Game_OnObjectCreate;
        }
        #endregion

        #region 类方法HOOK
        private void OnDraw(EventArgs args)
        {
            if(DrawQ && Q.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, Q.Range, Color.Red);
            }
            if (DrawQExtra && Q.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, ExtraDmgQ.Range, Color.Orange);
            }
            if (DrawW && W.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, W.Range, Color.Black);
            }
            if (DrawBarrel)
            {
                foreach (var obj in Barrels)
                {
                    var WorldToScreen = Drawing.WorldToScreen(obj.Pointer.Position);
                    float lowTime = (obj.VaildTime - Variables.GameTimeTickCount) / 1000f;
                    PlusRender.DrawText(lowTime.ToString(), WorldToScreen.X, WorldToScreen.Y, Color.LawnGreen);
                }
            }
        }
        private void Game_OnUpdate(EventArgs args)
        {
            ExtraHealQ.Delay = ExtraDmgQ.Delay = Q.Delay = 0.4f - Math.Max(0, Math.Min(0.2f, 0.02f * ((Player.AttackSpeedMod - 1) / 0.25f)));
            HealAllyLogic();
            AutoRLogic();
            ResetQRange();
            if (AutoHarass && Orbwalker.ActiveMode != OrbwalkerMode.Combo)
            {
                Harass();
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
                    if (Laneclear_Pick)
                    {
                        AutoPick();
                    }
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
            Barrels.RemoveAll(minion => !IsValidBarrels(minion));
            //Orbwalker.ForceTarget = null;
        }
        private bool IsValidBarrels(BarrelsInfo Barrel)
        {
            if (Barrel == null || !Barrel.Pointer.IsValid || Barrel.Pointer.IsDead || Variables.GameTimeTickCount > Barrel.VaildTime)
                return false;
            return true;
        }
        private void Game_OnObjectCreate(GameObject obj,EventArgs s)
        {
            var ToBase = obj as AIBaseClient;
            if(ToBase == null)
            {
                return;
            }
            if(ToBase.Name.Equals("Barrel") && ToBase.MaxHealth == 1 && !ToBase.IsAlly && ToBase.IsTargetableToTeam(GameObjectTeam.Neutral))
            {
                Barrels.Add(new BarrelsInfo() { Pointer = ToBase, VaildTime = Variables.GameTimeTickCount + 8000 });
            }
        }
        #endregion

        #region Methods
        private void ResetQRange()
        {
            var PassiveCount = Player.GetBuffCount("SennaPassiveStacks");
            if(PassiveCount >= 400)
            {
                Q.Range = 1100f;
                return;
            }
            int Dist = 600 + (PassiveCount / 20) * 25;
            Q.Range = Dist + Player.BoundingRadius;
        }
        private void HealAllyLogic()
        {
            if (!Q.IsReady() || UseQHealAlly == 2) return;
            if (FastHeal)
            {
                var allyList = Cache.AlliesHeroes.Where(x => x.HealthPercent != 100 && !x.IsMe && x.NewIsValidTarget(ExtraHealQ.Range, false)).OrderBy(x => x.GetRealHeath(DamageType.Physical)).ToList();
                if (allyList.Count == 0)
                {
                    if (Player.HealthPercent != 100)
                    {
                        var Eye = Cache.GetTrinket(Player.ServerPosition, Q.Range).FirstOrDefault();
                        if (Eye != null)
                        {
                            Q.Cast(Eye);
                        }
                    }
                }
                foreach (var allyobj in allyList)
                {
                    if (Q.IsInRange(allyobj))
                    {
                        Q.CastOnUnit(allyobj);
                    }
                    else
                    {
                        CastExtraQ(false, allyobj);
                    }
                }
            }
            if (UseQHealAlly == 0)
            {
                foreach (var allyobj in Cache.AlliesHeroes.Where(x => x.NewIsValidTarget(ExtraHealQ.Range, false)))
                {
                    if (allyobj.HealthPercent <= QHealPerfe)
                    {
                        if (Q.IsInRange(allyobj))
                        {
                            Q.CastOnUnit(allyobj);
                            break;
                        }
                        else
                        {
                            CastExtraQ(false,allyobj);
                        }
                    }
                }
            }
        }
        private List<AIBaseClient> GetHittableTargets()
        {
            var unitList = new List<AIBaseClient>();
            var minions = Cache.GetMinions(
                Player.ServerPosition,
                Q.Range);
            var jungles = Cache.GetJungles(Player.ServerPosition, Q.Range);
            var heros = Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(Q.Range, false) && x.IsTargetable);
            var turrent = GameObjects.Get<AITurretClient>().Where(x => x.NewIsValidTarget(Q.Range, false) && x.IsTargetable);
            unitList.AddRange(minions);
            unitList.AddRange(jungles);
            unitList.AddRange(Cache.MinionsListAlly.Where(x => x.NewIsValidTarget(Q.Range,false)));
            unitList.AddRange(heros);
            unitList.AddRange(turrent);
            unitList.AddRange(Cache.GetTrinket(Player.ServerPosition, Q.Range));
            return unitList;
        }
        private bool IsBlockR(AIHeroClient unit)
        {
            return ChampionMenu["Rset"]["HealthList"]["not." + unit.CharacterName].GetValue<MenuBool>().Enabled;
        }
        private float GetRDmg(AIHeroClient unit)
        {
            if(unit == null)
            {
                return 0f;
            }
            int BaseDamage = 250 + (DamageR.Level - 1) * 125;
            float ExtraDmg = (Player.TotalMagicalDamage * 0.7f) + (Player.TotalAttackDamage - Player.BaseAttackDamage);
            return (float)Player.CalculatePhysicalDamage(unit, BaseDamage + ExtraDmg);
        }
        private bool CanCastSpell(Spell spl, AIHeroClient obj)
        {
            if (HealthPrediction.GetPrediction(obj, (int)(spl.Delay * 1000)) <= 0 || (obj.HealthPercent <= 10 && obj.CountAllysHerosInRangeFix(400f) - 1 >= 1))
            {
                return false;
            }
            return true;
        }
        private void AutoRLogic()
        {
            if (UseRMode != 2)
            {
                if (UseRMode == 0 || (UseRMode == 1 && Orbwalker.ActiveMode == OrbwalkerMode.Combo))
                {
                    if (UseRHealAlly)
                    {
                        foreach (var allyobj in Cache.AlliesHeroes.Where(x => x.NewIsValidTarget(HealthR.Range, false) && !IsBlockR(x)))
                        {
                            if (allyobj.HealthPercent <= RHealPerfe && allyobj.CountEnemyHerosInRangeFix(400f) >= RHealEnemyCount)
                            {
                                //1s后血量大于0  而且1.5s后友军死啦
                                var SpellToHero = ((allyobj.DistanceToPlayer() / DamageR.Speed) * 1000) + (DamageR.Delay * 1000);

                                if (!UsePredHealR || (HealthPrediction.GetPrediction(allyobj, (int)SpellToHero) > 0 &&
                                       HealthPrediction.GetPrediction(allyobj, (int)SpellToHero + 500) <= 0))
                                {
                                    HealthR.Cast(allyobj.ServerPosition); //直接对友军位置释放 不考虑预判
                                }
                            }
                        }
                    }
                    if (UseKillableR)
                    {
                        foreach (var enemy in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(DamageR.Range)))
                        {
                            var SpellToHero = ((enemy.DistanceToPlayer() / DamageR.Speed) * 1000) + (DamageR.Delay * 1000);

                            var HealthPreds = HealthPrediction.GetPrediction(enemy, (int)SpellToHero);
                            if (CanCastSpell(DamageR, enemy) && HealthPreds < GetRDmg(enemy) && Player.CountEnemyHerosInRangeFix(800) == 0)
                            {
                                if (Player.CountEnemyHerosInRangeFix(600f) <= UseKillableRCountMeEnemy && !Player.IsWindingUp)
                                {
                                    var pred = DamageR.GetPrediction(enemy, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                                    if (pred.Hitchance >= HitChance.VeryHigh)
                                    {
                                        DamageR.Cast(pred.CastPosition);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void CastW()
        {
            var Ret = IMPGetTarGet(W, false, HitChance.VeryHigh);
            if(Ret.SuccessFlag && Ret.Obj.IsValid)
            {
                W.Cast(Ret.CastPosition);
            }
        }
        private bool CastNornmlQ(AIBaseClient unit = null)
        {
            var Qtarget = unit ?? TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (!Qtarget.NewIsValidTarget(Q.Range))
                return false;
            return Q.CastOnUnit(Qtarget);
        }
        private bool CastExtraQ(bool Damage = true,AIBaseClient HealAlly = null)
        {
            if (Damage)
            {
                var t1 = TargetSelector.GetTarget(ExtraDmgQ.Range, DamageType.Physical);
                if (t1.NewIsValidTarget(ExtraDmgQ.Range))
                {
                    var predictionPosition = ExtraDmgQ.GetPrediction(t1,true);
                    if (predictionPosition.Hitchance < HitChance.High)
                        return false;

                    
                    foreach (var unit in from unit in GetHittableTargets()
                                         let polygon =
                                             new Geometry.Rectangle(
                                             Player.ServerPosition,
                                             Player.ServerPosition.Extend(
                                                 unit.ServerPosition,
                                                 ExtraDmgQ.Range),
                                             ExtraDmgQ.Width)
                                         where polygon.IsInside(predictionPosition.CastPosition) && Q.IsInRange(unit)
                                         select unit)
                    {
                        
                        return ExtraDmgQ.CastOnUnit(unit);
                    }

                }
            }
            else
            {
                if (HealAlly != null)
                {
                    if (HealAlly.NewIsValidTarget(ExtraHealQ.Range,false) && !Q.IsInRange(HealAlly))
                    {
                        foreach (var unit in from unit in GetHittableTargets()
                                             let polygon =
                                                 new Geometry.Rectangle(
                                                 Player.ServerPosition,
                                                 Player.ServerPosition.Extend(
                                                     unit.ServerPosition,
                                                     ExtraHealQ.Range),
                                                 ExtraHealQ.Width)
                                             where polygon.IsInside(HealAlly.ServerPosition) && Q.IsInRange(unit)
                                             select unit)
                        {
                            return ExtraHealQ.CastOnUnit(unit);
                        }

                    }
                }
            }
            return false;
        }
        private void AutoPick()
        {
            foreach (var PickObj in Barrels)
            {
                if (PickObj.Pointer.InAutoAttackRange())
                {
                    if (Orbwalker.CanAttack())
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, PickObj.Pointer);
                    }
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= Laneclear_Mana)
            {
                return;
            }
            if (W.IsReady() && JungleClear_UseW && !Player.Spellbook.IsWindingUp)
            {
                var minions = Cache.GetJungles(Player.ServerPosition, W.Range).FirstOrDefault();
                if(minions != null)
                {
                    W.Cast(minions.ServerPosition);
                }
            }
            if (Q.IsReady() && JungleClear_UseQ && !Player.Spellbook.IsWindingUp)
            {
                var minions = Cache.GetJungles(Player.ServerPosition, Q.Range).MinOrDefault(x => x.GetRealHeath(DamageType.Physical));
                if (minions != null)
                {
                    Q.Cast(minions);
                }
            }
        }
        private void LaneClear()
        {
            if(!Enable_laneclear || Player.ManaPercent <= Laneclear_Mana)
            {
                return;
            }

            if (Laneclear_UseQ && Q.IsReady() && !Player.Spellbook.IsWindingUp)
            {
                var minions = Cache.GetMinions(Player.ServerPosition, ExtraDmgQ.Range);
                foreach (var minion in minions)
                {
                    var poutput = ExtraDmgQ.GetPrediction(minion, true, -1, new CollisionObjects[] { CollisionObjects.Heroes, CollisionObjects.Minions });

                    var col = poutput.CollisionObjects;

                    if (col.Count >= Laneclear_UseQCountEnemy)
                    {
                        var minionQ = col.FirstOrDefault(x => x.DistanceToPlayer() <= Q.Range);
                        if (minionQ.NewIsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(minionQ);
                            return;
                        }
                    }
                }
            }


        }
        private void Harass()
        {
            if(Player.ManaPercent <= Harass_Mana)
            {
                return;
            }
            if(Harass_UseQ && Q.IsReady())
            {
                if(!CastNornmlQ() && Harass_UseExtraQ)
                    CastExtraQ(true);
            }
        }
        private void Combo()
        {
            if (ComboOrbMode == 0)
            {
                var OrbTarget = Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(Player.AttackRange) &&
                                                                      x.HasBuff("sennapassivemarker")).MaxOrDefault(y => TargetSelector.GetPriority(y));
                Orbwalker.ForceTarget = OrbTarget;
            }
            if (Combo_W && W.IsReady() && !Player.Spellbook.IsWindingUp)
            {
                CastW();
            }
            if (Combo_UseQ && Q.IsReady() && !Player.Spellbook.IsWindingUp)
            {
                if(!CastNornmlQ() && Combo_ExtraQ)
                    CastExtraQ(true);
            }
        }
        #endregion
    }
}
