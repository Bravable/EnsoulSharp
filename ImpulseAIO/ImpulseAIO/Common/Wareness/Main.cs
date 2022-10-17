using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

using ImpulseAIO.Common.Wareness;
namespace ImpulseAIO.Common.Wareness
{
    internal class Menus
    {
        public static MenuItemSettings ItemPanel = new MenuItemSettings();
        public static MenuItemSettings Tracker = new MenuItemSettings();
        public static MenuItemSettings Detector = new MenuItemSettings();
        public static MenuItemSettings CloneTracker = new MenuItemSettings(new CloneTracker()); //Works
        public static MenuItemSettings VisionDetector = new MenuItemSettings(new HiddenObject());
        public class MenuItemSettings
        {
            public bool ForceDisable;
            public dynamic Item;
            public EnsoulSharp.SDK.MenuUI.Menu Menu;
            public List<MenuItem> MenuItems = new List<MenuItem>();
            public String Name;
            public List<MenuItemSettings> SubMenus = new List<MenuItemSettings>();
            public Type Type;

            public MenuItemSettings(Type type, dynamic item)
            {
                Type = type;
                Item = item;
            }

            public MenuItemSettings(dynamic item)
            {
                Item = item;
            }

            public MenuItemSettings(Type type)
            {
                Type = type;
                Game.Print(type.Namespace);
            }

            public MenuItemSettings(String name)
            {
                Name = name;
            }

            public MenuItemSettings()
            {
            }

            public MenuItemSettings AddMenuItemSettings(String displayName, String name)
            {
                SubMenus.Add(new MenuItemSettings(name));
                MenuItemSettings tempSettings = GetMenuSettings(name);
                if (tempSettings == null)
                {
                    throw new NullReferenceException(name + " not found");
                }
                tempSettings.Menu = Menu.Add(new EnsoulSharp.SDK.MenuUI.Menu(displayName, name));
                return tempSettings;
            }

            public bool GetActive()
            {
                if (Menu == null)
                    return false;
                foreach (var item in Menu.Components)
                {
                    if (item.Key == "Active")
                    {
                        if (item.Value.GetValue<MenuBool>().Enabled)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                return false;
            }

            public void SetActive(bool active)
            {
                if (Menu == null)
                    return;
                foreach (var item in Menu.Components)
                {
                    if (item.Key == "Active")
                    {
                        item.Value.GetValue<MenuBool>().SetValue(active);
                        return;
                    }
                }
            }

            public MenuItem GetMenuItem(String menuName)
            {
                if (Menu == null)
                    return null;
                foreach (var item in Menu.Components)
                {
                    if (item.Key == menuName)
                    {
                        return item.Value.GetValue<MenuItem>();
                    }
                }
                return null;
            }
            public MenuItemSettings GetMenuSettings(String name)
            {
                foreach (MenuItemSettings menu in SubMenus)
                {
                    if (menu.Name.Contains(name))
                        return menu;
                }
                return null;
            }
        }
    }
    
    internal class Main
    {
		public static Menu mainMenu;
		public Main()
        {
            Game_OnGameLoad();
        }
		private void Game_OnGameLoad()
		{
			mainMenu = new Menu("IMPWareness", Program.ScriptName + ":多合一工具", true);
			mainMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.MapHack));

            Menus.Tracker.Menu = mainMenu.Add(new Menu("Tracker", "英雄跟踪"));

            Menus.CloneTracker.Menu =
                    Menus.Tracker.Menu.Add(new Menu("CloneTracker", "克隆(分身)跟踪"));
            Menus.CloneTracker.MenuItems.Add(
                Menus.CloneTracker.Menu.Add(new MenuBool("Active", "激活跟踪").SetValue(false)));

            Menus.Tracker.MenuItems.Add(
                    Menus.Tracker.Menu.Add(new MenuBool("Active", "激活 英雄跟踪器").SetValue(false)));

            //Not crashing
            Menus.Detector.Menu = mainMenu.Add(new Menu("Detector", "道具探测"));
            Menus.VisionDetector.Menu =
                Menus.Detector.Menu.Add(new Menu("VisionDetector","非可视单位探测"));
            Menus.VisionDetector.MenuItems.Add(
                Menus.VisionDetector.Menu.Add(
                    new MenuBool("SAwarenessVisionDetectorDrawRange", "画出范围").SetValue(false)));
            Menus.VisionDetector.MenuItems.Add(
                Menus.VisionDetector.Menu.Add(
                    new MenuBool("Active", "激活 非可视单位探测").SetValue(false)));
            Menus.Detector.MenuItems.Add(
                    Menus.Detector.Menu.Add(new MenuBool("Active", "激活 道具探测").SetValue(false)));

            mainMenu.Attach();
            /*Tracker = mainMenu.Add(new Menu("Tracker", "敌人探测"));
            {
				Tracker.Add(new MenuBool("WaypointTracker", "显示移动路径"));
				Tracker.Add(new MenuBool("DestinationTracker", "显示移动目的地"));
				Tracker.Add(new MenuBool("CloneTracker", "克隆单位探测"));
				UITracker = Tracker.Add(new Menu("UITracker", "UI探测面板"));
                {
					UITracker.Add(new MenuSlider("Scale", "面板大小", 100, 0, 100));

				}

				Tracker.Add(new MenuSlider("RemindTime", "提醒时间", 0, 50, 100));
				Tracker.Add(new MenuList("ChatChoice", "提醒方式",new string[] { "不提醒","本地","服务器"} ,1));
				Tracker.Add(new MenuBool("JungleTimer", "野怪提示"));
				Tracker.Add(new MenuBool("JungleTimer", "野怪提示"));
			}*/
        }
	}
}
