using System;
using System.Collections.Generic;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using ImpulseAIO.Common;

namespace ImpulseAIO
{
    class Program : Base
    {
        static bool isLoad = true;
        public static bool Chinese = false;
        private static readonly string Version = "3.3.2";
        public static readonly string ScriptName = "Impulse";
        private static readonly Dictionary<string, Func<object>> Plugins = new Dictionary<string, Func<object>>
                                                                               {
                                                                                   { "Lucian", () => new Champion.Lucian.Lucian() },
                                                                                   { "Viktor", () => new Champion.Viktor.Viktor()},
                                                                                   { "Senna", () => new Champion.Senna.Senna()},
                                                                                   { "Orianna", () => new Champion.Orianna.Orianna()},
                                                                                   { "Kaisa", () => new Champion.Kaisa.Kaisa()},
                                                                                   { "Vayne", () => new Champion.Vayne.Vayne()},
                                                                                   { "Jinx", () => new Champion.Jinx.Jinx()},
                                                                                   { "Twitch", () => new Champion.Twitch.Twitch()},
                                                                                   { "Irelia", () => new Champion.Irelia.Irelia()},
                                                                                   { "Blitzcrank", () => new Champion.Blitzcrank.Blitzcrank()},
                                                                                   { "Ezreal", () => new Champion.Ezreal.Ezreal()},
                                                                                   { "Kalista", () => new Champion.Kalista.Kalista()},
                                                                                   { "Zeri", () => new Champion.Zeri.Zeri()},
                                                                                   { "Thresh", () => new Champion.Thresh.Thresh()},
                                                                                   { "Kindred", () => new Champion.Kindred.Kindred()},
                                                                                   { "TwistedFate", () => new Champion.TwistedFate.TwistedFate()},
                                                                                   { "Jhin", () => new Champion.Jhin.Jhin()},
                                                                                   { "Caitlyn", () => new Champion.Caitlyn.Caitlyn()},
                                                                                   { "Renata", () => new Champion.Renata.Renata()},
                                                                                   { "Tristana", () => new Champion.Tristana.Tristana()},
                                                                                   { "Samira", () => new Champion.Samira.Samira()},
                                                                                   { "Xerath", () => new Champion.Xerath.Xerath()},
                                                                                   { "Yasuo", () => new Champion.Yasuo.Yasuo()},
                                                                                   { "Syndra", () => new Champion.Syndra.Syndra()}


                                                                               };
        public static void Main(string[] args)
        {
            GameEvent.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad()
        {
            Chinese = EnsoulSharp.SDK.Core.Config.Language == "zh_CN";
            if (!Plugins.ContainsKey(Player.CharacterName))
            {
                Game.Print(ScriptName + "Not Support : {0}", Player.CharacterName);
                isLoad = false;
            }
            InitMenu();
        }

        private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValid)
            {
                return;
            }

            var unit = missile.SpellCaster as AIHeroClient;

            if (unit == null || !unit.IsValid)
            {
                return;

            }
            Console.WriteLine(missile.Name.ToLower());
        }

        private static void InitMenu()
        {
            Base.InitCCTracker();
            CommonMenu = new Menu("QCommon", ScriptName + ":" + (Chinese ? "脚本加载器" : "Main") + "[2022-04-22]" + Version.ToString(), true).Attach();
            CommonMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.ABOS));
            {
                CommonMenu.Add(new MenuSeparator("AQ1", Chinese ? "核心增强类" : "Core Power"));
                CommonMenu.Add(new MenuBool("InvokeOrb", Chinese ? "启动[增强走砍]" : "imp Orbwalker"));
                CommonMenu.Add(new MenuBool("InvokeTS", Chinese ? "启动[目标选择器]" : "imp TargetSelector"));
                if (isLoad)
                {
                    CommonMenu.Add(new MenuSeparator("AQ2", Chinese ? "英雄脚本类" : "Champion"));
                    CommonMenu.Add(new MenuBool("InvokeChamp", Chinese ? "启动[英雄脚本]" : "Champion Script"));
                }
                CommonMenu.Add(new MenuSeparator("AQ3", Chinese ? "实用功能类" : "Utility"));
                //CommonMenu.Add(new MenuBool("InvokeMapHack", Chinese ? "启动[神意识]" : "MapHack"));
                //CommonMenu.Add(new MenuBool("InvokeActivactor", Chinese ? "启动[装备活化]" : "Items Activactor"));
                CommonMenu.Add(new MenuBool("Prevent", Chinese ? "解除ens限制" : "Up EnsoulSharp Power",false));
                CommonMenu.Add(new MenuSeparator("AQ4", Chinese ? "选完记得F5重载.或者游戏重进" : "Please F5 if you change setting."));
            }
            Hacks.AntiDisconnectFlags.SetFlags(AntiDisconnectFlags.CastSpell, CommonMenu["Prevent"].GetValue<MenuBool>().Enabled);
            Hacks.AntiDisconnectFlags.SetFlags(AntiDisconnectFlags.IssueOrder, CommonMenu["Prevent"].GetValue<MenuBool>().Enabled);
            var testSlot = GameObjects.Player.GetSpellSlotFromName("summonerflash");
            if (testSlot != SpellSlot.Unknown)
            {
                Flash = testSlot;
            }
            if (CommonMenu["InvokeOrb"].GetValue<MenuBool>().Enabled)
            {
                Orbwalker.GetOrbwalker("SDK").Dispose();
                Orbwalker.AddOrbwalker("NewOrbwalker", new Common.Overwirte.NewOrbwalker());
                Orbwalker.SetOrbwalker("NewOrbwalker");
            }
            if (CommonMenu["InvokeTS"].GetValue<MenuBool>().Enabled)
            {
                TargetSelector.GetTargetSelector("SDK").Dispose();
                TargetSelector.AddTargetSelector("NewTargetSelector", new Common.Overwirte.NewTargetSelector());
                TargetSelector.SetTargetSelector("NewTargetSelector");
            }
            if (isLoad && CommonMenu["InvokeChamp"].GetValue<MenuBool>().Enabled)
            {
                ChampionMenu = new Menu("imp_" + Player.CharacterName, Chinese ? ScriptName + ":英雄合集" : ScriptName + ":Champion",true).Attach();
                ChampionMenu.Add(new MenuKeyBind("LT", Chinese ? "是否技能清线/清野" : "Use Spell Farm/Jungle", Keys.J, KeyBindType.Toggle)).AddPermashow();

                Plugins[Player.CharacterName].Invoke();
            }
            //if (CommonMenu["InvokeMapHack"].GetValue<MenuBool>().Enabled)
            //{
            //    new Common.Wareness.Main();
            //    //Common.MasterMind.MasterMind.EnableMind();
            //}
            //if (CommonMenu["InvokeActivactor"].GetValue<MenuBool>().Enabled)
            //{
            //    new Common.Activactor.Activactor();
            //}
        }
    }
}
