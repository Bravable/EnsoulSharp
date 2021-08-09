using SharpDX;

namespace EnsoulSharp.SDK.ThirdParty.Evades
{
    public interface ISkillshot
    {
        int DangerLevel { get; }

        float StartTime { get; }
        float EndTime { get; }

        float Delay { get; }
        float Radius { get; }
        float Range { get; }

        string ChampionName { get; }
        string SpellName { get; }

        Vector2 StartPosition { get; }
        Vector2 EndPosition { get; }
        Vector2 CurrentPosition { get; }

        SkillshotType Type { get; }

        bool IsInSide(Vector2 position, float extraRadius, bool checkCollision = true);

        bool WillHit(Vector2 position, float delay);
    }
}
