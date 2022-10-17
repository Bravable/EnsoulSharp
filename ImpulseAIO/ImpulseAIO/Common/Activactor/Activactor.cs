using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;
namespace ImpulseAIO.Common.Activactor
{
    internal class Activactor : Base
    {
        private static bool HaveGaleforce = false;//狂风之力
        private static Menu GaleforceMenu;
        private const int GaleforceRange = 410;
        private const int GaleforceSpeed = 1350;

        private static bool HaveZhonyas = false;//舒瑞亚的战歌
        private static Menu ZhonyasMenu;

        #region 菜单选项
        private static bool Galeforce_Enable => ActivatorMenu["Galeforce"]["enable"].GetValue<MenuBool>().Enabled;
        private static bool Galeforce_AntiMelee => ActivatorMenu["Galeforce"]["antimelee"].GetValue<MenuBool>().Enabled;
        private static int Galeforce_AntiGap => ActivatorMenu["Galeforce"]["antiGap"].GetValue<MenuList>().Index;
        private static int Galeforce_AntiGapdist => ActivatorMenu["Galeforce"]["antiGapdist"].GetValue<MenuSlider>().Value;

        private static bool Zhonyas_Enable => ActivatorMenu["Zhonyas"]["enable"].GetValue<MenuBool>().Enabled;
        private static bool Zhonyas_healthPred => ActivatorMenu["Zhonyas"]["healthPred"].GetValue<MenuBool>().Enabled;
        #endregion
        public Activactor()
        {
            ActivatorMenu = new Menu("Activator", Program.Chinese ? Program.ScriptName + ":装备活化" : Program.ScriptName + ":Iteam Activator",true).Attach();
            ActivatorMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Activator));
            GaleforceMenu = ActivatorMenu.Add(new Menu("Galeforce", Program.Chinese ? "狂风之力" : "Galeforce"));
            {
                GaleforceMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Galeforce));
                GaleforceMenu.Add(new MenuBool("enable", Program.Chinese ? "启用 狂风之力" : "Enable Galaforce"));
                GaleforceMenu.Add(new MenuBool("antimelee", Program.Chinese ? "被近战英雄贴脸时逃离使用" : "Use if Player in Melee Hero Attack Range"));
                GaleforceMenu.Add(new MenuList("antiGap", Program.Chinese ? "反突进" : "AntiGapCloser", new string[] { "Target", "Line", "All", "Disable" },2));
                GaleforceMenu.Add(new MenuSlider("antiGapdist", "-> Use Galaforce if GapCloser enemy Distance To Player <= X", 300, 0, 500));
            }
            ZhonyasMenu = ActivatorMenu.Add(new Menu("Zhonyas", Program.Chinese ? "中娅沙漏" : "Zhonyas"));
            {
                ZhonyasMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Zhonyas));
                ZhonyasMenu.Add(new MenuBool("enable", Program.Chinese ? "启用 中娅沙漏" : "Enable Zhonyas"));
                ZhonyasMenu.Add(new MenuBool("healthPred", Program.Chinese ? "自己快歇逼时 使用血量预测逻辑(Beta不建议开启)" : "Protect Me if was dead",false));
            }
            AntiGapcloser.Attach(GaleforceMenu, true, "GalaforceAntiGap", Program.Chinese ? "狂风之力 反突进列表" : "Galaforce AntiGapList");
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += OnGapCloaser;
        }
        private void OnGapCloaser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (!Galeforce_Enable || !HaveGaleforce || !Player.CanUseItem(ItemId.Galeforce)) return;

            if (sender.IsEnemy && args.StartPosition.DistanceToPlayer() > args.EndPosition.DistanceToPlayer() && args.EndPosition.DistanceToPlayer() <= GaleforceRange)
            {
                if(Galeforce_AntiGap != 3)
                {
                    if(Galeforce_AntiGap == 0 && args.Type == AntiGapcloser.GapcloserType.Targeted && args.Target.IsMe ||
                       Galeforce_AntiGap == 1 && args.Type == AntiGapcloser.GapcloserType.SkillShot && sender.IsValidTarget(Galeforce_AntiGapdist) ||
                       Galeforce_AntiGap == 2 && (args.Type == AntiGapcloser.GapcloserType.Targeted || (args.Type == AntiGapcloser.GapcloserType.SkillShot && sender.IsValidTarget(Galeforce_AntiGapdist))))
                    {
                        var newDashPos = GetBestDash();
                        if (newDashPos.IsValid())
                        {
                            CastGaleforce(newDashPos);
                        }
                    }
                }
            }
        }
        private void Game_OnUpdate(EventArgs args)
        {
            CheckItem();

            if (Galeforce_Enable && HaveGaleforce)
            {
                if (Galeforce_AntiMelee && Player.CanUseItem(ItemId.Galeforce))
                {
                    AntiMelee();
                }
            }
            if(Zhonyas_Enable && HaveZhonyas)
            {
                if (Zhonyas_healthPred && HealthPrediction.GetPrediction(Player,200) <= 0)
                {
                    if (Player.CanUseItem(ItemId.Zhonyas_Hourglass))
                    {
                        CastZhonya();
                    }
                }
            }
        }
        private void AntiMelee()
        {
            if (InMelleAttackRange(Player.Position))
            {
                var newDashPos = GetBestDash();
                if (newDashPos.IsValid())
                {
                    CastGaleforce(newDashPos);
                }
            }
        }
        private void CheckItem()
        {
            HaveGaleforce = Player.HasItem(ItemId.Galeforce);
            HaveZhonyas = Player.HasItem(ItemId.Zhonyas_Hourglass);
        }
        private Vector3 GetBestDash()
        {
            var pos = new Geometry.Circle(Player.Position, GaleforceRange).Points.Select(x => x.ToVector3()).Where(x =>
            IsGoodGalaforcePosition(x)).MinOrDefault(x => x.DistanceToCursor());
            if(pos != null && pos.IsValid())
            {
                return pos;
            }
            return Vector3.Zero;
        }
        private bool IsGoodGalaforcePosition(Vector3 dashPos)
        {
            float segment = GaleforceRange / 5;
            for (int i = 1; i <= 5; i++)
            {
                if (Player.ServerPosition.Extend(dashPos, i * segment).IsWall())
                    return false;
            }

            if (dashPos.IsUnderEnemyTurret())
                return false;

            if (InMelleAttackRange(dashPos))
            {
                return false;
            }

            var enemyCountDashPos = dashPos.CountEnemyHerosInRangeFix(400f);//获取位移终点敌人

            if (enemyCountDashPos <= 1)
                return true;

            var enemyCountPlayer = Player.CountEnemyHerosInRangeFix(400);

            if (enemyCountDashPos <= enemyCountPlayer)
                return true;
            return false;
        }
        private bool CastGaleforce(Vector3 castPos)
        {
            return Player.UseItem((int)ItemId.Galeforce, castPos);
        }
        private bool CastZhonya()
        {
            if (!Player.CanUseItem(ItemId.Zhonyas_Hourglass)) return false;

            return Player.UseItem((int)ItemId.Zhonyas_Hourglass);
        }
    }
}
