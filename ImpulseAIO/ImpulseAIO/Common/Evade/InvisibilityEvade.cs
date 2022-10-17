using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
namespace ImpulseAIO.Common.Evade.Invisibility
{
    public class InvsSpell
    {
        public Spell spell = null;
        public bool IsDashSpell = false;
    }
    internal class InvisibilityEvade : Base
    {
        #region Static Fields
        private static readonly List<TargetSpellData> Spells = new List<TargetSpellData>();
        private static InvsSpell InvSpell = null;
        private static string SlotName;
        private static Dash dashh = null; 
        Func<bool> CanEvade;
        #endregion
        public InvisibilityEvade(InvsSpell arg,Dash dashinfo)
        {
            dashh = dashinfo;
            InvSpell = arg;
            SlotName = InvSpell.spell.Slot.ToString();
            LoadSpellData();
            InvisibilityEvadeMenu = ChampionMenu.Add(new Menu("EvadeTarget", Program.Chinese ? "隐身技能躲避:" : "Stealth Evade" + Player.CharacterName, true));
            {
                InvisibilityEvadeMenu.Add(new MenuBool(SlotName, "Use" + SlotName + "Evade Spell"));
                var aaMenu = InvisibilityEvadeMenu.Add(new Menu("AA", "Base Attack"));
                {
                    aaMenu.Add(new MenuBool("B", "Base Attack Evade"));
                    aaMenu.Add(new MenuSlider("BHpU", "-> When Health =< (%)", 35));
                    aaMenu.Add(new MenuBool("C", "Crit Attack Evade"));
                    aaMenu.Add(new MenuSlider("CHpU", "-> When Health =< (%)", 40));
                }
                foreach (var hero in
                    GameObjects.Get<AIHeroClient>().Where(
                        i => i.IsEnemy &&
                        Spells.Any(
                            a =>
                            string.Equals(
                                a.ChampionName,
                                i.CharacterName,
                                StringComparison.InvariantCultureIgnoreCase))))
                {
                    InvisibilityEvadeMenu.Add(new Menu(hero.CharacterName.ToLowerInvariant(), "-> " + hero.CharacterName));
                }
                foreach (var spell in
                    Spells.Where(
                        i =>
                        GameObjects.Get<AIHeroClient>().Any(
                            a => a.IsEnemy &&
                            string.Equals(
                                a.CharacterName,
                                i.ChampionName,
                                StringComparison.InvariantCultureIgnoreCase))))
                {
                    ((Menu)InvisibilityEvadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(new MenuBool(
                        spell.MissileName,
                        spell.MissileName + " (" + spell.Slot + ")",
                        true));
                    ((Menu)InvisibilityEvadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(new MenuSlider(
                        spell.MissileName + "H",
                        "^-Evade Spell When Health =< X%",
                        spell.HealthEvade, 0, 100));
                }
            }
            AIHeroClient.OnDoCast += OnDoCastBack;
        }
        private void LoadSpellData()
        {
            //new Spells

            //盖伦Q
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Garen", SpellNames = new[] { "garenqattack", "沉默打击" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            //诺手R + W
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Darius", SpellNames = new[] { "dariusexecute", "诺克萨斯断头台" }, Slot = SpellSlot.R, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Darius", SpellNames = new[] { "dariusnoxiantacticsonhattack", "致残打击" }, Slot = SpellSlot.W, HealthEvade = 100 });

            //李青 R
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Leesin", SpellNames = new[] { "blindmonkrkick", "猛龙摆尾" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //小炮 E + R
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Tristana", SpellNames = new[] { "Tristana_Base_E_explosion".ToLower(), "爆炸火花" }, Slot = SpellSlot.E, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Tristana", SpellNames = new[] { "tristanar", "加农炮" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //皇子R
            Spells.Add(
                new TargetSpellData
                { ChampionName = "JarvanIV", SpellNames = new[] { "jarvanivcataclysm", "天崩地裂" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //蝎子R
            Spells.Add(
                new TargetSpellData
                { ChampionName = "skarner", SpellNames = new[] { "detonatingshot", "蝎子R" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //滑板鞋E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "kalista", SpellNames = new[] { "detonatingshot", "拔矛" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //螳螂Q
            Spells.Add(
                new TargetSpellData
                { ChampionName = "khazix", SpellNames = new[] { "khazixq", "品尝恐惧" }, Slot = SpellSlot.Q, HealthEvade = 80 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "khazix", SpellNames = new[] { "khazixqlong", "进化 - 品尝恐惧" }, Slot = SpellSlot.Q, HealthEvade = 80 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "nocturne", SpellNames = new[] { "NocturneParanoia2".ToLower(), "鬼影重重" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //狗熊 Q + W
            Spells.Add(
                new TargetSpellData
                { ChampionName = "volibear", SpellNames = new[] { "volibearqattack", "授首一击" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "volibear", SpellNames = new[] { "volibearw", "暴怒撕咬" }, Slot = SpellSlot.W, HealthEvade = 100 });
            //辛吉德 E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Singed", SpellNames = new[] { "fling", "举高高" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //塞拉斯W
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Sylas", SpellNames = new[] { "SylasW".ToLower(), "弑君突刺" }, Slot = SpellSlot.W, HealthEvade = 100 });
            //机器人E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Blitzcrank", SpellNames = new[] { "powerfistattack", "能量铁拳" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //鳄鱼W
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Renekton", SpellNames = new[] { "renektonexecute", "冷酷追捕" }, Slot = SpellSlot.W, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Renekton", SpellNames = new[] { "renektonsuperexecute", "红怒 - 冷酷追捕" }, Slot = SpellSlot.W, HealthEvade = 100 });
            //女警R
            Spells.Add(
                new TargetSpellData
                {
                    ChampionName = "Caitlyn",
                    SpellNames = new[] { "caitlynaceintheholemissile", "完美一击" },
                    Slot = SpellSlot.R
                });
            //船长Q
            Spells.Add(
                new TargetSpellData { ChampionName = "Gangplank", SpellNames = new[] { "parley", "枪火谈判" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            //女枪Q
            Spells.Add(
                new TargetSpellData
                {
                    ChampionName = "MissFortune",
                    SpellNames = new[] { "missfortunericochetshot", "女枪Q" },
                    Slot = SpellSlot.Q,
                    HealthEvade = 100
                });
            //潘森W
            Spells.Add(
                new TargetSpellData { ChampionName = "Pantheon", SpellNames = new[] { "pantheonw", "斗盾跃击" }, Slot = SpellSlot.W, HealthEvade = 100 });
            //卡牌黄牌
            Spells.Add(
                new TargetSpellData
                { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack", "黄牌" }, Slot = SpellSlot.W, HealthEvade = 100 });
            //薇恩E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Vayne", SpellNames = new[] { "vaynycondemn", "恶魔审判" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //男刀Q
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Talon", SpellNames = new[] { "talonqattack", "诺克萨斯式外交" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            //蔚 R
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Vi", SpellNames = new[] { "vir", "天霸横空烈轰" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //牛头 W
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Alistar", SpellNames = new[] { "headbutt", "野蛮冲撞" }, Slot = SpellSlot.W, HealthEvade = 100 });
            //乌迪尔 巨熊姿态
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Udyr", SpellNames = new[] { "udyrbearattack", "巨熊姿态 普攻" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //剑圣Q 双刀
            Spells.Add(
                new TargetSpellData
                { ChampionName = "MasterYi", SpellNames = new[] { "alphastrike", "阿尔法突袭" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "MasterYi", SpellNames = new[] { "masteryidoublestrike", "剑圣被动双刀" }, Slot = SpellSlot.Unknown, HealthEvade = 100 });
            //狮子狗Q
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Rengar", SpellNames = new[] { "rengarqattack", "残忍无情" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            //克烈W 
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Kled", SpellNames = new[] { "kledwattack", "暴烈秉性 第四下普攻" }, Slot = SpellSlot.Unknown, HealthEvade = 100 });
            //赵信E+Q
            Spells.Add(
                new TargetSpellData
                { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoqthrust1", "三重爪击 第一段" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoqthrust2", "三重爪击 第二段" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoqthrust3", "三重爪击 第三段" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoe", "无畏冲锋" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //奎因E 
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Quinn", SpellNames = new[] { "quinne", "旋翔掠杀" }, Slot = SpellSlot.E, HealthEvade = 65 });
            //奥巴马Q
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Lucian", SpellNames = new[] { "lucianq", "透体圣光" }, Slot = SpellSlot.Q, HealthEvade = 20 });
            //杰斯Q+E 
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Jayce", SpellNames = new[] { "jaycetotheskies", "锤 - 苍穹之跃" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Jayce", SpellNames = new[] { "jaycethunderingblow", "锤 - 雷霆一击" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //挖掘机E+R
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Reksai", SpellNames = new[] { "reksaie", "狂野之噬" }, Slot = SpellSlot.E, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Reksai", SpellNames = new[] { "reksair", "虚空猛冲" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //凯隐R 要改
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Kayn", SpellNames = new[] { "kaynr", "裂舍影" }, Slot = SpellSlot.R, HealthEvade = 100 });
            //人马E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Kayn", SpellNames = new[] { "hecarimrampattack", "毁灭冲锋" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //千珏E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Kindred", SpellNames = new[] { "kindrede", "横生惧意" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //蒙多E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "DrMundo", SpellNames = new[] { "drmundoeattack", "大力行医" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //猴子Q E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "MonkeyKing", SpellNames = new[] { "monkeykingqattack", "粉碎打击" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            Spells.Add(
                new TargetSpellData
                { ChampionName = "MonkeyKing", SpellNames = new[] { "monkeykingnimbus", "腾云突击" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //牧魂人 Q 要改
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Yorick", SpellNames = new[] { "yorickqattack", "临终仪式" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            //波比E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Poppy", SpellNames = new[] { "poppye", "英勇冲锋" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //狼人Q
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Warwick", SpellNames = new[] { "warwickQ", "野兽之口" }, Slot = SpellSlot.Q, HealthEvade = 100 });
            //龙龟E
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Rammus", SpellNames = new[] { "puncturingtaunt", "狂乱嘲讽" }, Slot = SpellSlot.E, HealthEvade = 100 });
            //小法R
            Spells.Add(
                new TargetSpellData
                { ChampionName = "Veigar", SpellNames = new[] { "VeigarR", "能量爆破" }, Slot = SpellSlot.E, HealthEvade = 100 });
        }
        private void OnDoCastBack(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!CanEvade() || !InvisibilityEvadeMenu[SlotName].GetValue<MenuBool>().Enabled) 
                return;
            if (sender != null && args != null)
            {
                var SpellName = args.SData.Name;
                var HeroName = sender.CharacterName;

                if (sender is AIHeroClient && sender.IsEnemy)
                {
                    if (InvSpell.spell.IsReady())
                    {
                        var spellData = Spells.FirstOrDefault(i => i.SpellNames[0].Contains(SpellName.ToLower())); //是否存在技能名
                        if (spellData != null)
                        {
                            if (InvisibilityEvadeMenu[spellData.ChampionName.ToLower()][spellData.MissileName].GetValue<MenuBool>().Enabled &&
                                Player.HealthPercent <= InvisibilityEvadeMenu[spellData.ChampionName.ToLower()][spellData.MissileName + "H"].GetValue<MenuSlider>().Value)
                            {
                                if (args.Target.IsMe)
                                {
                                    SmartEvadeLogic(InvSpell);
                                    return;
                                }

                            }
                        }
                        if (spellData == null && Orbwalker.IsAutoAttack(args.SData.Name) && args.Target.IsMe) //如果没找到技能 而且这个是普攻
                        {
                            if (args.SData.Name.ToLower().Contains("crit") && InvisibilityEvadeMenu["AA"]["B"].GetValue<MenuBool>().Enabled)
                            {
                                if (Player.HealthPercent < InvisibilityEvadeMenu["AA"]["BHpU"].GetValue<MenuSlider>().Value)
                                {
                                    SmartEvadeLogic(InvSpell);
                                    return;
                                }
                            }
                            if (args.SData.Name.ToLower().Contains("basic") && InvisibilityEvadeMenu["AA"]["C"].GetValue<MenuBool>().Enabled)
                            {
                                if (Player.HealthPercent < InvisibilityEvadeMenu["AA"]["CHpU"].GetValue<MenuSlider>().Value)
                                {
                                    SmartEvadeLogic(InvSpell);
                                    return;
                                }
                            }
                        }
                    }
                }

            }
        }
        private void SmartEvadeLogic(InvsSpell info)
        {
            if (info.IsDashSpell && dashh != null)
            {
                var bestpos = dashh.CastDash(true);
                if (bestpos.IsValid())
                {
                    info.spell.Cast(bestpos);
                }
                return;
            }
            info.spell.Cast();
        }
        public void CanEvadeBool(Func<bool> func)
        {
            CanEvade = func;
        }
        private class TargetSpellData
        {
            #region Fields

            public string ChampionName;

            public SpellSlot Slot;

            public string[] SpellNames = { };

            public int HealthEvade;

            #endregion

            #region Public Properties

            public string MissileName => this.SpellNames.LastOrDefault();

            #endregion
        }
    }
}
