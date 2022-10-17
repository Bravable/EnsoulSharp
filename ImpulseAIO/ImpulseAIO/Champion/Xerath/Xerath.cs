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

namespace ImpulseAIO.Champion.Xerath
{
    internal class Xerath : Base 
    {
        private static Spell Q, W, E, R;
        private static Vector3 LastChargePosition;
        private static int LastChargeTime;
        private static int ChargesRemaining;
        private static int _previousLevel = 0;
        private static AIHeroClient _lastUltTarget;
        private static bool _targetWillDie;
        private static Menu AntiGapcloserMenu;
        private static int MaxCharges
        {
            get { return !R.Instance.Learned ? 3 : 2 + R.Level; }
        }
        private static bool IsCastingUlt
        {
            get { return Player.Buffs.Any(b => b.IsValid && b.Name == "xerathrshots"); }
        }
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static int ComboUseQExtra => ChampionMenu["Combo"]["CQExtra"].GetValue<MenuSlider>().Value;
        private static bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseW => ChampionMenu["Harass"]["HW"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseE => ChampionMenu["Harass"]["HE"].GetValue<MenuBool>().Enabled;
        private static int HarassUseQExtra => ChampionMenu["Harass"]["HQExtra"].GetValue<MenuSlider>().Value;
        private static int HarassMana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;

        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static bool LaneClearUseW => ChampionMenu["LaneClear"]["LW"].GetValue<MenuBool>().Enabled;
        private static int MinHitQ => ChampionMenu["LaneClear"]["HitQ"].GetValue<MenuSlider>().Value;
        private static int MinHitW => ChampionMenu["LaneClear"]["HitW"].GetValue<MenuSlider>().Value;
        private static int LaneClearMana => ChampionMenu["LaneClear"]["LMana"].GetValue<MenuSlider>().Value;
        private static bool gapE => AntiGapcloserMenu["GapE"].GetValue<MenuBool>().Enabled;
        private static bool intE => ChampionMenu["Miscellaneous"]["intE"].GetValue<MenuBool>().Enabled;

        private static bool DQ => ChampionMenu["Draw"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DW => ChampionMenu["Draw"]["DW"].GetValue<MenuBool>().Enabled;
        private static bool DE => ChampionMenu["Draw"]["DE"].GetValue<MenuBool>().Enabled;
        public Xerath()
        {
            Q = new Spell(SpellSlot.Q, 750f);
            Q.SetCharged("XerathArcanopulseChargeUp", "XerathArcanopulseChargeUp", 750, 1500, 1.5f);
            Q.SetSkillshot(0.5f, 70f, float.MaxValue, false, SpellType.Line);  
            W = new Spell(SpellSlot.W, 970f);
            W.SetSkillshot(0.75f, 125f, float.MaxValue, false, SpellType.Circle);
            E = new Spell(SpellSlot.E, 1050f);
            E.SetSkillshot(0.25f, 60f, 1400f, true, SpellType.Line);
            R = new Spell(SpellSlot.R, 5000f);
            R.SetSkillshot(0.6f, 100, float.MaxValue, false, SpellType.Circle);
            OnMenuLoad();
            Game.OnUpdate += OnGameUpdate;
            AIBaseClient.OnDoCast += OnProcessSpellCast;
            AntiGapcloser.OnGapcloser += (sender, args) =>
            {
                if (sender.IsEnemy && gapE && E.IsReady() && sender.IsDashing())
                {
                    // Cast E on the gapcloser caster
                    var PredV = E.GetPrediction(sender, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions });
                    if (PredV.Hitchance >= HitChance.High)
                    {
                        E.Cast(PredV.CastPosition);
                        return;
                    }

                }
            };
            Interrupter.OnInterrupterSpell += (sender, args) =>
            {
                if (sender.IsEnemy && args.DangerLevel == Interrupter.DangerLevel.High && intE &&E.IsReady() &&E.IsInRange(sender))
                {
                    // Cast E on the unit casting the interruptable spell
                    var PredV = E.GetPrediction(sender, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions });
                    if (PredV.Hitchance == HitChance.High)
                    {
                        E.Cast(PredV.CastPosition);
                        return;
                    }

                }
            };
            Render.OnEndScene += (args) =>
            {
                if (DQ && Q.IsReady())
                    PlusRender.DrawCircle(Player.Position, Q.ChargedMaxRange, Color.Orange);
                if (DW && W.IsReady())
                    PlusRender.DrawCircle(Player.Position, W.Range, Color.White);
                if (DE && E.IsReady())
                    PlusRender.DrawCircle(Player.Position, E.Range, Color.Red);

                if (R.IsReady())
                {
                    MiniMap.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Red);
                }
            };
        }
        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.SData.Name)
                {
                    // Ult activation
                    case "XerathLocusOfPower2":
                        LastChargePosition = Vector3.Zero;
                        LastChargeTime = 0;
                        ChargesRemaining = MaxCharges;
                        break;
                    // Ult charge usage
                    case "XerathLocusPulse":
                        LastChargePosition = args.End;
                        LastChargeTime = Environment.TickCount;
                        ChargesRemaining--;
                        break;
                }
            }
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Xerath));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo", true));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuSlider("CQExtra", "Q Extra Range", 30, 0, 200));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass", true));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HW", "Use W"));
                Harass.Add(new MenuBool("HE", "Use E"));
                
                Harass.Add(new MenuSlider("HQExtra", "Q Extra Range", 200, 0, 200));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ?"当蓝量 >= X时才骚扰" : "Dont harass if mana <= X%", 30));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear", true));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuBool("LW", "Use W"));
                LaneClear.Add(new MenuSlider("HitQ", "Q Min Hit minion", 3, 1, 10));
                LaneClear.Add(new MenuSlider("HitW", "W Min Hit minion", 3, 1, 10));
                LaneClear.Add(new MenuSlider("LMana", Program.Chinese ? "当蓝量 >= X时才清线" : "Don't laneclear if mana <= X%", 30));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("GapE", "Anti GapE"));
            }
            var Miscellaneous = ChampionMenu.Add(new Menu("Miscellaneous", "Misc", true));
            {
                Miscellaneous.Add(new MenuBool("intE", "Use E Interrupt"));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw", true));
            {
                Draw.Add(new MenuBool("DQ", "Draw Q"));
                Draw.Add(new MenuBool("DW", "Draw W"));
                Draw.Add(new MenuBool("DE", "Draw E"));
            }
        }
        private void OnGameUpdate(EventArgs args)
        {
            RLogic();
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
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void RLogic()
        {
            Orbwalker.AttackEnabled = !Q.IsCharging && !IsCastingUlt;
            Orbwalker.MoveEnabled = !IsCastingUlt;

            if (IsCastingUlt)
            {
                // Get first time target
                if (_lastUltTarget == null || ChargesRemaining >= 3)
                {
                    
                    var target = TargetSelector.GetTarget(R.Range,DamageType.Magical);
                    if (target != null && target.IsValidTarget())
                    {
                        var pred = R.GetPrediction(target);
                        if (pred.Hitchance >= HitChance.High)
                        {
                            R.Cast(pred.CastPosition);
                            _lastUltTarget = target;
                            _targetWillDie = target.GetRealHeath(DamageType.Magical) < R.GetDamage(target);
                        }
                    }
                }
                // Next target
                else if (ChargesRemaining < 3)
                {
                    // Shoot the same target again if in range
                    if ((!_targetWillDie || Environment.TickCount - LastChargeTime > 600) && _lastUltTarget.IsValidTarget(R.Range))
                    {
                        var pred = R.GetPrediction(_lastUltTarget);
                        if (pred.Hitchance >= HitChance.High)
                        {
                            R.Cast(pred.CastPosition);
                            _targetWillDie = _lastUltTarget.Health < R.GetDamage(_lastUltTarget);
                        }
                    }
                    // Target died or is out of range, shoot new target
                    else
                    {
                        /* TODO
                        // Check if last target is still alive
                        if (!_lastUltTarget.IsDead && ItemManager.UseRevealingOrb(_lastUltTarget.ServerPosition))
                        {
                            _orbUsedTime = Environment.TickCount;
                            break;
                        }

                        // Check if orb was used
                        if (Environment.TickCount - _orbUsedTime < 250)
                            break;
                        */

                        // Get a new target
                        var target = TargetSelector.GetTarget(R.Range,DamageType.Magical);
                        if (target != null && target.IsValidTarget())
                        {
                            // Only applies if smart targetting is enabled

                            // Calculate smart target change time
                            var waitTime = Math.Max(1500, target.Distance(LastChargePosition)) + 500;
                            if (Environment.TickCount - LastChargeTime + waitTime < 0)
                            {
                                return;
                            }

                            var pred = R.GetPrediction(target);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                R.Cast(pred.CastPosition);
                                _lastUltTarget = target;
                                _targetWillDie = target.Health < R.GetDamage(target);
                            }
                        }
                    }
                }
            }
        }
        private void Harass()
        {
            // Q is already charging, ignore mana check
            if (Q.IsReady() && Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High)
                    {
                        if (Q.Range == Q.ChargedMaxRange)
                        {
                            var PredV = Q.GetPrediction(target, true);
                            if (PredV.Hitchance >= HitChance.High)
                            {
                                Q.ShootChargedSpell(PredV.CastPosition);
                                return;
                            }
                        }
                        else
                        {
                            if (Player.InRange(prediction.UnitPosition + HarassUseQExtra * (prediction.UnitPosition - Player.ServerPosition).Normalized(), Q.Range))
                            {
                                if (Q.ShootChargedSpell(prediction.CastPosition))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
            {
                return;
            }

            // Check mana
            if (Player.ManaPercent < HarassMana)
            {
                return;
            }

            if (W.IsReady() && HarassUseW)
            {
                var WT = W.GetTarget();
                if (WT != null && WT.IsValidTarget())
                {
                    var pred = W.GetPrediction(WT, true);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        W.Cast(pred.CastPosition);
                        return;
                    }
                }
            }

            if (E.IsReady() && HarassUseE)
            {
                var Ret = IMPGetTarGet(E, false, HitChance.High);
                if (Ret.SuccessFlag && Ret.Obj.IsValid)
                {
                    E.Cast(Ret.CastPosition);
                    return;
                }
            }

            // Q chargeup
            if (Q.IsReady() && HarassUseQ && !Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High)
                    {
                        Q.StartCharging();
                    }
                }
            }
        }
        private void Combo()
        {
            // Validate that Q is not charging
            if (!Q.IsCharging)
            {
                if (ComboUseW && W.IsReady())
                {
                    var WT = W.GetTarget();
                    if (WT != null && WT.IsValidTarget())
                    {
                        var pred = W.GetPrediction(WT,true);
                        if (pred.Hitchance >= HitChance.High)
                        {
                            W.Cast(pred.CastPosition);
                            return;
                        }
                    }

                }

                if (ComboUseE && E.IsReady())
                {
                    var Ret = IMPGetTarGet(E, false, HitChance.High);
                    if(Ret.SuccessFlag && Ret.Obj.IsValid)
                    {
                        if(Ret.Obj.GetStunDuration() == 0 || Ret.Obj.GetStunDuration() < (Player.ServerPosition.Distance(Ret.Obj.ServerPosition) / E.Speed + E.Delay / 1000f) * 1000)
                        {
                            E.Cast(Ret.CastPosition);
                            return;
                        }
                    }
                }
            }
            if (ComboUseQ && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High)
                    {
                        if (!Q.IsCharging)
                        {
                            Q.StartCharging();
                            return;
                        }
                        if (Q.Range == Q.ChargedMaxRange)
                        {
                            var PredV = Q.GetPrediction(target, true);
                            if (PredV.Hitchance >= HitChance.High)
                            {
                                Q.ShootChargedSpell(PredV.CastPosition);
                                return;
                            }
                        }
                        else
                        {
                            if (Player.InRange(prediction.UnitPosition + ComboUseQExtra * (prediction.UnitPosition - Player.ServerPosition).Normalized(), Q.Range))
                            {
                                if (Q.ShootChargedSpell(prediction.CastPosition))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
            {
                return;
            }
        }
        private void LaneClear()
        {
            // Get the minions around

            if (!Enable_laneclear)
                return;

            var minions = Cache.GetMinions(Player.ServerPosition, Q.ChargedMaxRange).ToList();
            if (minions.Count == 0)
            {
                return;
            }

            // Q is charging, ignore mana check
            if (Q.IsReady() && LaneClearUseQ && Q.IsCharging)
            {
                var fime = Q.GetLineFarmLocation(minions);
                if (fime.MinionsHit >=MinHitQ)
                {
                    if (Q.ShootChargedSpell(fime.Position))
                    {
                        return;
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
            {
                return;
            }

            // Check mana
            if (LaneClearMana > Player.ManaPercent)
            {
                return;
            }

            if (Q.IsReady() &&LaneClearUseQ)
            {
                if (minions.Count >=MinHitQ)
                {
                    // Check if we would hit enough minions
                    if (Q.GetLineFarmLocation(minions).MinionsHit >=MinHitQ)
                    {
                        // Start charging
                        Q.StartCharging();
                        return;
                    }
                }
            }

            if (W.IsReady() &&LaneClearUseW)
            {
                if (minions.Count >=MinHitW)
                {
                    var farmLocation = W.GetCircularFarmLocation(minions);
                    if (farmLocation.MinionsHit >=MinHitW)
                    {
                        if (W.Cast(farmLocation.Position))
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
    
}
