using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using ImpulseAIO.Common;
namespace ImpulseAIO.Champion.TwistedFate
{
    internal class TwistedFate : Base 
    {
        private static Spell Q, W, E, R;
        private static bool Ykey => ChampionMenu["CardSelector"]["useY"].GetValue<MenuKeyBind>().Active;
        private static bool Bkey => ChampionMenu["CardSelector"]["useB"].GetValue<MenuKeyBind>().Active;
        private static bool Rkey => ChampionMenu["CardSelector"]["useR"].GetValue<MenuKeyBind>().Active;

        private static bool ComboUseQ => ChampionMenu["Combo"]["useQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseQStun => ChampionMenu["Combo"]["useQStun"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseW => ChampionMenu["Combo"]["useW"].GetValue<MenuBool>().Enabled;
        private static int ComboUseWMode => ChampionMenu["Combo"]["useWCard"].GetValue<MenuList>().Index;
        private static int ComboUseWRedCount => ChampionMenu["Combo"]["useWRedCount"].GetValue<MenuSlider>().Value;
        private static int ComboUseWBlueMana => ChampionMenu["Combo"]["useWBlueMana"].GetValue<MenuSlider>().Value;
        private static bool ComboUseWBlueOnlyKill => ChampionMenu["Combo"]["useWBlueOnlyKill"].GetValue<MenuBool>().Enabled;

        private static bool HarassUseQ => ChampionMenu["Harass"]["useQ"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQStun => ChampionMenu["Harass"]["useQStun"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseW => ChampionMenu["Harass"]["useW"].GetValue<MenuBool>().Enabled;
        private static int HarassUseWCard => ChampionMenu["Harass"]["useWCard"].GetValue<MenuList>().Index;
        private static int HarassUseWBlueMana => ChampionMenu["Harass"]["useWBlueMana"].GetValue<MenuSlider>().Value;
        private static int HarassMana => ChampionMenu["Harass"]["Hmana"].GetValue<MenuSlider>().Value;

        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["useQ"].GetValue<MenuBool>().Enabled;
        private static int LaneClearUseQCount => ChampionMenu["LaneClear"]["useQCount"].GetValue<MenuSlider>().Value;
        private static int LaneClearUseQMana => ChampionMenu["LaneClear"]["useQMana"].GetValue<MenuSlider>().Value;
        private static bool LaneClearUseW => ChampionMenu["LaneClear"]["useW"].GetValue<MenuBool>().Enabled;
        private static int LaneClearUseWCard => ChampionMenu["LaneClear"]["useWCard"].GetValue<MenuList>().Index;
        private static int LaneClearUseWBlueMana => ChampionMenu["LaneClear"]["Lmana"].GetValue<MenuSlider>().Value;
        private static bool autoQimm => ChampionMenu["Misc"]["autoQ"].GetValue<MenuBool>().Enabled;
        private static bool autoY => ChampionMenu["Misc"]["autoY"].GetValue<MenuBool>().Enabled;

        private static bool DrawQ => ChampionMenu["Draw"]["useQ"].GetValue<MenuBool>().Enabled;
        private static bool DrawR => ChampionMenu["Draw"]["useR"].GetValue<MenuBool>().Enabled;
        private static bool KilluseQ => ChampionMenu["Killsteal"]["useQ"].GetValue<MenuBool>().Enabled;
        private static bool KilluseW => ChampionMenu["Killsteal"]["useW"].GetValue<MenuBool>().Enabled;
        public TwistedFate()
        {
            Q = new Spell(SpellSlot.Q, 1450f);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, SpellType.Line);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 5500f);
            OnMenuLoad();
            AIHeroClient.OnProcessSpellCast += (sender, args) =>
            {
                if (!sender.IsMe)
                {
                    return;
                }

                if (args.SData.Name.ToLower() == "gate" && autoY)
                {
                    CardSelector.StartSelecting(CardSelector.Cards.Yellow);
                }
            };
            Render.OnEndScene += (args) =>
            {
                if (DrawQ && R.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Yellow.ToSharpDxColor());
                }
                if (DrawR && R.IsReady())
                {
                    MiniMap.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Red);
                }
            };
            AIHeroClient.OnBuffRemove += (s, g) =>
            {
                if (s.IsMe)
                {
                    if (g.Buff.Name == "lichbane")
                    {
                        CardDamage.lichbaneTimer = Variables.TickCount + 2700f;
                    }
                }
            };
            Game.OnUpdate += GameOnUpdate;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.TwistedFate));
            var CardSelector = ChampionMenu.Add(new Menu("CardSelector", Program.Chinese ? "选牌热键" : "CardSelector"));
            {
                CardSelector.Add(new MenuKeyBind("useY", Program.Chinese ? "黄牌热键" : "Yellow", Keys.W, KeyBindType.Press)).AddPermashow();
                CardSelector.Add(new MenuKeyBind("useB", Program.Chinese ? "蓝牌热键" : "Blue", Keys.E, KeyBindType.Press)).AddPermashow();
                CardSelector.Add(new MenuKeyBind("useR", Program.Chinese ? "红牌热键" : "Red", Keys.T, KeyBindType.Press)).AddPermashow();
            }
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("useQ", "Use Q"));
                Combo.Add(new MenuBool("useQStun", Program.Chinese ? "^-仅对定身敌人使用Q" : "Only CC", true));
                Combo.Add(new MenuBool("useW", "Use W"));
                Combo.Add(new MenuList("useWCard", Program.Chinese ? "^-选牌模式" : "Card Mode", new string[] { "Smart", "Blue", "Red", "Yellow" }));
                Combo.Add(new MenuSlider("useWRedCount", Program.Chinese ? "^-当红牌可以AOE打中敌人 X人以上 时" : "Use Red if AOE HitCount >= X", 3, 1, 5));
                Combo.Add(new MenuSlider("useWBlueMana", Program.Chinese ? "^-当蓝量<=X时使用蓝牌" : "Use Blue if Mana <= X", 25, 0, 100));
                Combo.Add(new MenuBool("useWBlueOnlyKill", Program.Chinese ? "^-不启用蓝牌蓝量检查 仅当能打死时使用蓝牌" : "Don't Use Blue Card. Only Can Killalbe Use Blue Card.", true));
                var OnlyY = Combo.Add(new Menu("OnlyY", Program.Chinese ? "对此目标始终使用黄牌" : "Always Yellow card"));
                {
                    foreach (var x in GameObjects.EnemyHeroes)
                    {
                        OnlyY.Add(new MenuBool("Cast." + x.CharacterName, x.CharacterName, false));
                    }
                }
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("useQ", "Use Q "));
                Harass.Add(new MenuBool("useQStun", Program.Chinese ? "^-仅对定身敌人使用Q" : "Only CC", false));

                Harass.Add(new MenuBool("useW", "Use W "));
                Harass.Add(new MenuList("useWCard", Program.Chinese ? "^-选牌模式" : "Card Mode", new string[] { "Smart", "Blue", "Red", "Yellow" }));
                Harass.Add(new MenuSlider("useWBlueMana", Program.Chinese ? "^-当蓝量<=X时使用蓝牌" : "Use Blue Card if Mana <= X%", 25, 0, 100));
                Harass.Add(new MenuSlider("Hmana", Program.Chinese ? "当蓝量 <= X%时不骚扰" : "Don't Use Spell Harass if Mana <= X%", 45, 0, 100));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("useQ", "Use Q "));
                LaneClear.Add(new MenuSlider("useQCount", "^-Min hit x minion", 3, 1, 5));
                LaneClear.Add(new MenuSlider("useQMana", Program.Chinese ? "当蓝量 <= X时不使用Q" : "Don't Q if Mana <= X%", 25, 0, 100));
                LaneClear.Add(new MenuBool("useW", "Use W "));
                LaneClear.Add(new MenuList("useWCard", Program.Chinese ? "^-选牌模式" : "-> Card Mode", new string[] { "Smart", "Blue", "Red", "Yellow" }));
                LaneClear.Add(new MenuSlider("Lmana", Program.Chinese ? "当蓝量 <= X时使用蓝牌" : "Use Blue card When Mana <= X%", 45, 0, 100));

            }
            var Killsteal = ChampionMenu.Add(new Menu("Killsteal", "Killsteal"));
            {
                Killsteal.Add(new MenuBool("useQ", "Use Q"));
                Killsteal.Add(new MenuBool("useW", "Use W"));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("useQ", "Draw Q"));
                Draw.Add(new MenuBool("useR", "Draw R"));
            }
            var Misc = ChampionMenu.Add(new Menu("Misc", "Misc"));
            {
                Misc.Add(new MenuBool("autoQ", Program.Chinese ? "对无法移动的目标使用Q" : "Auto Q CC"));
                Misc.Add(new MenuBool("autoY", Program.Chinese ? "落地黄牌" : "Use R Auto Yellow card."));
            }
        }
        private void GameOnUpdate(EventArgs args)
        {
            Killsteal();
            AutoQ();
            Card();
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
        private void Card()
        {
            if (Ykey)
            {
                CardSelector.SelectCard(CardSelector.Cards.Yellow);
            }
            if (Bkey)
            {
                CardSelector.SelectCard(CardSelector.Cards.Blue);
            }
            if (Rkey)
            {
                CardSelector.SelectCard(CardSelector.Cards.Red);
            }
        }
        private void Combo()
        {
            if (ComboUseW)
            {

                var w_Target =  TargetSelector.GetTargets(Player.GetRealAutoAttackRange() + 300f, DamageType.Magical, false).MaxOrDefault(x => TargetSelector.GetPriority(x));
                if (w_Target != null)
                {
                    switch (ComboUseWMode)
                    {
                        case 0:
                            var selectedCard = CardSelector.HeroCardSelection(w_Target);
                            if (selectedCard != CardSelector.Cards.None)
                            {
                                CardSelector.SelectCard(selectedCard);
                            }
                            break;
                        case 1:
                            CardSelector.SelectCard(CardSelector.Cards.Blue);
                            break;
                        case 2:
                            CardSelector.SelectCard(CardSelector.Cards.Red);
                            break;
                        case 3:
                            CardSelector.SelectCard(CardSelector.Cards.Yellow);
                            break;
                    }
                    if(Orbwalker.CanAttack() && w_Target.InAutoAttackRange())
                    {
                        Orbwalker.Attack(w_Target);
                    }
                }
            }

            if (ComboUseQ)
            {
                var qTarget = TargetSelector.GetTarget(
                    Q.Range,
                    DamageType.Magical);

                if (qTarget == null)
                    return;

                if (ComboUseQStun)
                    return;

                if (!Q.IsInRange(qTarget) || !Q.IsReady())
                    return;


                var pred = Q.GetPrediction(qTarget);

                if (pred.Hitchance >= HitChance.High)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }
        private void Harass()
        {
            if (Player.ManaPercent <= HarassMana)
                return;

            if (HarassUseW)
            {
                var w_Target =  TargetSelector.GetTargets(Player.GetRealAutoAttackRange() + 300f, DamageType.Magical, false).MaxOrDefault(x => TargetSelector.GetPriority(x));

                if (w_Target != null)
                {
                    switch (HarassUseWCard)
                    {
                        case 0:
                            var selectedCard = CardSelector.HeroCardSelectionHarass(w_Target);
                            if (selectedCard != CardSelector.Cards.None)
                            {
                                CardSelector.SelectCard(selectedCard);
                                if (Orbwalker.CanAttack() && w_Target.InAutoAttackRange())
                                {
                                    Orbwalker.Attack(w_Target);
                                }
                            }
                            else
                            {
                                //获取目标周围小兵
                                var Minions = Cache.GetMinions(Player.ServerPosition).Where(x => x.CanAttack && x.IsTargetable && x.IsValidTarget() && x.InAutoAttackRange()).Where(y => y.Distance(w_Target) <= 175f).MinOrDefault(x => x.Distance(w_Target));
                                if (Minions != null)
                                {
                                    CardSelector.SelectCard(CardSelector.Cards.Red);
                                    if (Orbwalker.CanAttack() && Minions.InAutoAttackRange())
                                    {
                                        Orbwalker.Attack(Minions);
                                    }
                                }
                                else //没目标直接黄牌
                                {
                                    CardSelector.SelectCard(CardSelector.Cards.Yellow);
                                    if (Orbwalker.CanAttack() && w_Target.InAutoAttackRange())
                                    {
                                        Orbwalker.Attack(w_Target);
                                    }
                                }
                            }
                            break;
                        case 1:
                            CardSelector.SelectCard(CardSelector.Cards.Blue);
                            if (Orbwalker.CanAttack() && w_Target.InAutoAttackRange())
                            {
                                Orbwalker.Attack(w_Target);
                            }
                            break;
                        case 2:
                            CardSelector.SelectCard(CardSelector.Cards.Red);
                            if (Orbwalker.CanAttack() && w_Target.InAutoAttackRange())
                            {
                                Orbwalker.Attack(w_Target);
                            }
                            break;
                        case 3:
                            CardSelector.SelectCard(CardSelector.Cards.Yellow);
                            if (Orbwalker.CanAttack() && w_Target.InAutoAttackRange())
                            {
                                Orbwalker.Attack(w_Target);
                            }
                            break;
                    }
                }
            }

            if (HarassUseQ)
            {
                var qTarget = TargetSelector.GetTarget(
                    Q.Range,
                    DamageType.Magical);

                if (qTarget == null)
                    return;

                if (HarassUseQStun)
                    return;

                if (!Q.IsInRange(qTarget) || !Q.IsReady())
                    return;

                var pred = Q.GetPrediction(qTarget);

                if (pred.Hitchance >= HitChance.High)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear)
                return;

            if (LaneClearUseQ && Q.IsReady())
            {
                var qMinion =
                    Cache.GetMinions(Player.ServerPosition, Q.Range).OrderBy(t => t.Health).ToList();

                var manaManagerQ = LaneClearUseQMana;

                if (Player.ManaPercent >= manaManagerQ)
                {
                    var minionPrediction = Q.GetLineFarmLocation(
                        qMinion);

                    if (minionPrediction.MinionsHit >= LaneClearUseQCount)
                    {
                        Q.Cast(minionPrediction.Position);
                    }
                }
            }

            if (LaneClearUseW)
            {
                var minion =
                    Cache.GetMinions(
                        Player.ServerPosition,
                        Player.GetRealAutoAttackRange() + 100).ToArray();
                if (!minion.Any())
                    return;

                switch (LaneClearUseWCard)
                {
                    case 0:
                        var redCardKillableMinions = minion.Count(
                            target => target.Distance(minion.MinOrDefault(x => x.Distance(target))) <= 200 &&
                                      target.Health <=
                                      CardDamage.GetRedDamage(target) + (Q.IsReady() ? Q.GetDamage(target) : 0));
                        var selectedCard = CardSelector.Cards.None;

                        if (selectedCard == CardSelector.Cards.None && Player.ManaPercent <= LaneClearUseWBlueMana)
                        {
                            selectedCard = CardSelector.Cards.Blue;
                        }

                        if (selectedCard == CardSelector.Cards.None && redCardKillableMinions >= 3)
                        {
                            selectedCard = CardSelector.Cards.Red;
                        }
                        if (selectedCard != CardSelector.Cards.None)
                        {
                            CardSelector.SelectCard(selectedCard);
                        }
                        break;
                    case 1:
                        CardSelector.SelectCard(CardSelector.Cards.Blue);
                        break;
                    case 2:
                        CardSelector.SelectCard(CardSelector.Cards.Red);
                        break;
                    case 3:
                        CardSelector.SelectCard(CardSelector.Cards.Yellow);
                        break;
                }
            }
        }
        private static void Killsteal()
        {
            if (KilluseQ && Q.IsReady())
            {
                var targets = Cache.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) && x.GetRealHeath(DamageType.Magical) < Q.GetDamage(x));
                var preds =
                targets.Select(i => Q.GetPrediction(i, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall }))
                    .Where(
                        i =>
                        i.Hitchance >= HitChance.High)
                    .ToList();
                if (preds.Count > 0)
                {
                    Q.Cast(preds.MaxOrDefault(i => i.Hitchance).CastPosition);
                }
            }
            if (KilluseW && W.IsReady())
            {
                var targets = Cache.EnemyHeroes.Where(x => x.IsValidTarget(Player.GetRealAutoAttackRange()) && x.GetRealHeath(DamageType.Magical) < CardDamage.GetBlueDamage(x)).MaxOrDefault(y => TargetSelector.GetPriority(y));
                if (targets != null)
                {
                    CardSelector.SelectCard(CardSelector.Cards.Blue);
                    if (Orbwalker.CanAttack() && targets.InAutoAttackRange())
                    {
                        Orbwalker.Attack(targets);
                    }
                }
            }
        }
        private static void AutoQ()
        {
            if (!Q.IsReady() || !autoQimm)
                return;

            foreach (var h in Cache.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
            {
                var pred = Q.GetPrediction(h, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                if (pred.Hitchance >= HitChance.Immobile || (h.HasBuffOfType(BuffType.Slow) && pred.Hitchance >= HitChance.VeryHigh))
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }
        private class CardDamage
        {
            public static bool lichbane = false;
            public static float lichbaneTimer = Variables.TickCount;
            public static bool Ludens = false;
            private static int last_item_update = 0;
            private static float checkItemDamage(AIBaseClient unit)
            {
                float endDamage = 0f;
                if (lichbane)
                {
                    if (Variables.TickCount >= lichbaneTimer || Player.HasBuff("lichbane"))
                    {
                        var baseDamage = (Player.BaseAttackDamage * 1.5f) + (Player.TotalMagicalDamage * 0.4f);
                        endDamage += (float)Player.CalculateMagicDamage(unit, baseDamage);
                    }
                }
                if (Ludens)
                {
                    var baseDamage = 100 + (Player.TotalMagicalDamage * 0.1f);
                    endDamage += (float)Player.CalculateMagicDamage(unit, baseDamage);
                }
                return endDamage;
            }
            public static void CheckItem()
            {
                if (Variables.TickCount > last_item_update)
                {
                    lichbane = Player.HasItem(ItemId.Lich_Bane);
                    Ludens = Player.HasItem(ItemId.Ludens_Tempest) && Player.CanUseItem(ItemId.Ludens_Tempest);
                    last_item_update = Variables.TickCount + 5000;
                }
            }
            private static float GetEDamage(AIBaseClient unit)
            {
                var hasBuff = Player.HasBuff("cardmasterstackparticle");
                float endDamage = 0f;
                if (hasBuff)
                {
                    var BaseDamage = 65 + (E.Level - 1) * 25;
                    endDamage = BaseDamage + (Player.TotalMagicalDamage * 0.5f);
                    return (float)Player.CalculateMagicDamage(unit, endDamage);
                }
                return endDamage;
            }
            public static float GetBlueDamage(AIBaseClient unit)
            {
                float BlueBaseDamage = 40 + (W.Level - 1) * 20;
                float endDamage = BlueBaseDamage + Player.TotalAttackDamage + (Player.TotalMagicalDamage * 0.9f);
                return (float)Player.CalculateMagicDamage(unit, endDamage) + GetEDamage(unit) + checkItemDamage(unit);
            }
            public static float GetRedDamage(AIBaseClient unit)
            {
                float RedBaseDamage = 30 + (W.Level - 1) * 15;
                float endDamage = RedBaseDamage + Player.TotalAttackDamage + (Player.TotalMagicalDamage * 0.6f);
                return (float)Player.CalculateMagicDamage(unit, endDamage) + GetEDamage(unit) + checkItemDamage(unit);
            }
            public static float GetYellowDamage(AIBaseClient unit)
            {
                float BlueBaseDamage = 15 + (W.Level - 1) * 7.5f;
                float endDamage = BlueBaseDamage + Player.TotalAttackDamage + (Player.TotalMagicalDamage * 0.5f);
                return (float)Player.CalculateMagicDamage(unit, endDamage) + GetEDamage(unit) + checkItemDamage(unit);
            }
        }
        private class CardSelector
        {
            public enum Cards
            {
                Red,
                Yellow,
                Blue,
                None,
            }
            public enum SelectStatus
            {
                Ready,
                Selecting,
                Selected,
                Cooldown,
            }

            public static Cards SelectedCard;
            public static int LastW;
            public static SelectStatus Status { get; set; }

            public static int Delay => new Random().Next(125, 200);

            static CardSelector()
            {
                AIBaseClient.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
                Game.OnUpdate += Game_OnGameUpdate;
            }
            public static int GetEStack()
            {
                return Player.GetBuffCount("cardmasterstackholder");
            }
            public static bool HasEBuff()
            {
                return Player.HasBuff("cardmasterstackparticle");
            }
            public static void StartSelecting(Cards card)
            {
                if (Player.Spellbook.GetSpell(SpellSlot.W).Name == "PickACard" && Status == SelectStatus.Ready)
                {
                    SelectedCard = card;
                    if (Environment.TickCount - LastW > 170 + Game.Ping / 2)
                    {
                        W.Cast();
                        LastW = Environment.TickCount;
                    }
                }
            }
            public static void Obj_AI_Base_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                if (!sender.IsMe)
                {
                    return;
                }

                if (args.SData.Name == "PickACard")
                {
                    Status = SelectStatus.Selecting;
                }

                if (args.SData.Name.ToLower() == "goldcardlock" || args.SData.Name.ToLower() == "bluecardlock" ||
                    args.SData.Name.ToLower() == "redcardlock")
                {
                    Status = SelectStatus.Selected;
                    SelectedCard = Cards.None;
                }
            }
            public static bool IsYTarget(AIHeroClient t)
            {
                return ChampionMenu["Combo"]["OnlyY"]["Cast." + t.CharacterName].GetValue<MenuBool>().Enabled;
            }
            private static void Game_OnGameUpdate(EventArgs args)
            {
                var wName = Player.Spellbook.GetSpell(SpellSlot.W).Name;
                var wState = Player.Spellbook.CanUseSpell(SpellSlot.W);

                if ((wState == SpellState.Ready &&
                     wName == "PickACard" &&
                     (Status != SelectStatus.Selecting || Environment.TickCount - LastW > 500)) ||
                    Player.IsDead)
                {
                    Status = SelectStatus.Ready;
                }
                else if (wState == SpellState.Cooldown &&
                         wName == "PickACard")
                {
                    SelectedCard = Cards.None;
                    Status = SelectStatus.Cooldown;
                }
                else if (wState == SpellState.Surpressed &&
                         !Player.IsDead)
                {
                    Status = SelectStatus.Selected;
                }

                if (SelectedCard == Cards.Blue && wName.ToLower() == "bluecardlock" && Environment.TickCount - Delay > LastW)
                {
                    Player.Spellbook.CastSpell(SpellSlot.W, false);
                }
                else if (SelectedCard == Cards.Yellow && wName.ToLower() == "goldcardlock" && Environment.TickCount - Delay > LastW)
                {
                    Player.Spellbook.CastSpell(SpellSlot.W, false);
                }
                else if (SelectedCard == Cards.Red && wName.ToLower() == "redcardlock" && Environment.TickCount - Delay > LastW)
                {
                    Player.Spellbook.CastSpell(SpellSlot.W, false);
                }
            }
            public static void SelectCard(Cards selectedCard)
            {
                if (selectedCard == Cards.Red)
                {
                    StartSelecting(Cards.Red);
                }
                else if (selectedCard == Cards.Yellow)
                {
                    StartSelecting(Cards.Yellow);
                }
                else if (selectedCard == Cards.Blue)
                {
                    StartSelecting(Cards.Blue);
                }
            }
            public static Cards HeroCardSelection(AIHeroClient t)
            {
                if (t != null && t.IsValidTarget())
                {
                    var card = Cards.None;
                    var alliesaroundTarget = t.ServerPosition.CountEnemyHerosInRangeFix(175f);
                    var RedCount = ComboUseWRedCount;
                    var manaW = ComboUseWBlueMana;
                    //一张牌直接打死时
                    if (ComboUseWBlueOnlyKill)
                    {
                        if (t.GetRealHeath(DamageType.Magical) < CardDamage.GetBlueDamage(t))
                        {
                            card = Cards.Blue;
                            return card;
                        }
                    }
                    //黄牌锁定目标
                    if (IsYTarget(t))
                    {
                        card = Cards.Yellow;
                        return card;
                    }
                    //回蓝
                    if (!ComboUseWBlueOnlyKill && Player.ManaPercent <= manaW)
                    {
                        card = Cards.Blue;
                        return card;
                    }
                    //以多打少判断黄牌
                    if (Player.ServerPosition.CountAllysHerosInRangeFix(500) - 1 >= Player.ServerPosition.CountEnemyHerosInRangeFix(600))
                    {
                        card = Cards.Yellow;
                        return card;
                    }
                    //红牌AOE
                    if (alliesaroundTarget >= RedCount)
                    {
                        card = Cards.Red;
                        return card;
                    }
                    if (Q.IsReady())
                    {
                        card = Cards.Yellow;
                        return card;
                    }
                    card = Cards.Yellow;
                    return card;
                }
                return Cards.None;
            }
            public static Cards HeroCardSelectionHarass(AIHeroClient t)
            {
                if (t != null && t.IsValidTarget())
                {
                    var card = Cards.None;
                    var manaW = HarassUseWBlueMana;
                    //一张牌直接打死时
                    if (ComboUseWBlueOnlyKill)
                    {
                        if (t.GetRealHeath(DamageType.Magical) < CardDamage.GetBlueDamage(t))
                        {
                            card = Cards.Blue;
                            return card;
                        }
                    }
                    if (Player.ManaPercent <= manaW)
                    {
                        card = Cards.Blue;
                        return card;
                    }
                    if (HasEBuff())
                    {
                        card = Cards.Blue;
                        return card;
                    }
                    if (GetEStack() == 2)
                    {
                        card = Cards.Yellow;
                        return card;
                    }
                    if (Q.IsReady())
                    {
                        card = Cards.Yellow;
                        return card;
                    }
                }
                return Cards.None;
            }
        }
    }
}
