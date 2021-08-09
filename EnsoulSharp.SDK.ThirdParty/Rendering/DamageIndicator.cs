using SharpDX;
using System;

namespace EnsoulSharp.SDK.ThirdParty.Rendering
{
    public static class DamageIndicator
    {
        public delegate double DamageToUnitDelegate(AIBaseClient hero);

        private static readonly bool init;

        private static readonly Render.Line Line;
        private static readonly Render.Line Line2;

        private static DamageToUnitDelegate _damageToUnit;

        public static DamageToUnitDelegate DamageToUnit
        {
            get => _damageToUnit;
            set => _damageToUnit = value;
        }

        static DamageIndicator()
        {
            if (init)
            {
                return;
            }

            init = true;

            Line = new Render.Line(Vector2.Zero, Vector2.Zero, 1, Color.Lime);
            Line2 = new Render.Line(Vector2.Zero, Vector2.Zero, 1, Color.Goldenrod);
        }

        public static void Draw(AIHeroClient hero, double damage = 0d)
        {
            if (hero == null || !hero.IsValid || hero.IsDead || !hero.IsVisible || !hero.IsVisibleOnScreen)
            {
                return;
            }

            if (damage == 0d && _damageToUnit != null)
            {
                damage = _damageToUnit(hero);
            }

            if (damage <= 0d)
            {
                return;
            }

            Vector2 barPos = hero.HPBarPosition - new Vector2(55, 45);

            double percentHealthAfterDamage = Math.Max(0, hero.Health - damage) / hero.MaxHealth;

            float xPosDamage = barPos.X + 10 + (float)(103 * percentHealthAfterDamage);
            float xPosCurrentHp = barPos.X + 10 + 103 * hero.Health / hero.MaxHealth;
            float yPos = barPos.Y + 20;

            DrawLine(Line, xPosDamage, yPos, xPosDamage, yPos + 11);

            float differenceInHp = xPosCurrentHp - xPosDamage;
            float pos1 = barPos.X + 9 + (float)(107 * percentHealthAfterDamage);

            for (int i = 0; i < differenceInHp; i++)
            {
                DrawLine(Line2, pos1 + i, yPos, pos1 + i, yPos + 11);
                Line2.Draw();
            }
        }

        private static void DrawLine(Render.Line line, float x1, float y1, float x2, float y2)
        {
            line.Start = new Vector2(x1, y1);
            line.End = new Vector2(x2, y2);
            line.Draw();
        }
    }
}
