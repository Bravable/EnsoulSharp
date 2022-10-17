using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
namespace ImpulseAIO.Common.Evade
{
    public class MissileManager
    {
        private const float PercentOffset = 1.25f;
        public static List<MissileClient> Missiles = new List<MissileClient>();
        public static bool WillHit(AIBaseClient i)
        {
            foreach (var m in Missiles.Where(m => m.IsValid))
            {
                var canCast = false;
                if (m.Target != null)
                {
                    canCast = m.Target.IsMe;
                }
                if (!m.EndPosition.IsZero && m.SData.LineWidth > 0)
                {
                    var width = ((m.SData.TargetingType == SpellDataTargetType.Location
                        ? m.SData.CastRadius
                        : m.SData.LineWidth) + i.BoundingRadius) * PercentOffset;
                    var extendedEndPos = m.EndPosition + (m.EndPosition - m.StartPosition).Normalized() * width;

                    canCast =
                        i.Position.ToVector2()
                            .Distance(m.StartPosition.ToVector2(), extendedEndPos.ToVector2(), true, true) <= width * width;
                }
                if (canCast)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool MissileWillHitMyHero
        {
            get
            {
                foreach (var m in Missiles.Where(m => m.IsValid))
                {
                    var canCast = false;
                    if (m.Target != null)
                    {
                        canCast = m.Target.IsMe;
                    }
                    if (!m.EndPosition.IsZero && m.SData.LineWidth > 0)
                    {
                        var width = ((m.SData.TargetingType == SpellDataTargetType.Location
                            ? m.SData.CastRadius
                            : m.SData.LineWidth) + GameObjects.Player.BoundingRadius) * PercentOffset;
                        var extendedEndPos = m.EndPosition + (m.EndPosition - m.StartPosition).Normalized() * width;

                        canCast =
                            GameObjects.Player.Position.ToVector2()
                                .Distance(m.StartPosition.ToVector2(), extendedEndPos.ToVector2(), true, true) <= width * width;
                    }
                    if (canCast)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public static void Initialize()
        {
            GameObject.OnCreate += delegate (GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile != null)
                {
                    var spellCaster = missile.SpellCaster;
                    if (spellCaster.Team != GameObjects.Player.Team && !spellCaster.IsMinion())
                    {
                        Missiles.Add(missile);
                    }
                }
            };
            GameObject.OnDelete += delegate (GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile != null)
                {
                    Missiles.RemoveAll(m => m.Equals(missile));
                }
            };
        }
    }
}
