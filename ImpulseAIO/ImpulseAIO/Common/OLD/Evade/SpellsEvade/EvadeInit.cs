using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK.MenuUI;
namespace QSharp.Common.Evade.SpellsEvade
{
    public class EvadeInit : Base
    {
        public static bool IsDebugMode => EvadeMenu["Debug"].GetValue<MenuBool>().Enabled;

        public EvadeInit()
        {
            EvadeMenu = CommonMenu.Add(new Menu("SpellsEvade", Program.Chinese ? "躲避管理列表" : "Evade Manager", true));
            {
                EvadeMenu.Add(new MenuBool("Debug", Program.Chinese ? "启用技能测试" : "Enable Spell Test", false));
            }
            Evade.Init();
        }
    }
}
