using SharpDX;

namespace EnsoulSharp.SDK.ThirdParty.Evades
{
    /// <summary>
    ///     Evade Skillshot interface.
    /// </summary>
    public interface ISkillshot
    {
        /// <summary>
        ///    Get the skillshot DangerLevel.
        /// </summary>
        int DangerLevel { get; }

        /// <summary>
        ///    Gets or sets the skillshot start time.
        /// </summary>
        float StartTime { get; }

        /// <summary>
        ///    Get the skillshot end time.
        /// </summary>
        float EndTime { get; }

        /// <summary>
        ///    Get the skillshot delay.
        /// </summary>
        float Delay { get; }

        /// <summary>
        ///    Get the skillshot radius.
        /// </summary>
        float Radius { get; }

        /// <summary>
        ///    Get the skillshot range.
        /// </summary>
        float Range { get; }

        /// <summary>
        ///    Get the skillshot from champion name.
        /// </summary>
        string ChampionName { get; }

        /// <summary>
        ///    Get the skillshot name.
        /// </summary>
        string SpellName { get; }

        /// <summary>
        ///    Get the skillshot start position.
        /// </summary>
        Vector2 StartPosition { get; }

        /// <summary>
        ///    Get the skillshot end position.
        /// </summary>
        Vector2 EndPosition { get; }

        /// <summary>
        ///    Get the skillshot current position.
        /// </summary>
        Vector2 CurrentPosition { get; }

        /// <summary>
        ///    Get the skillshot type.
        /// </summary>
        EvadeSkillshotType Type { get; }

        /// <summary>
        ///    The Position is in Skillshot.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="radius">The Radius.</param>
        /// <param name="checkCollision">Check spell Collision.</param>
        /// <returns></returns>
        bool IsInSide(Vector2 position, float radius, bool checkCollision = true);

        /// <summary>
        ///    Get Position hit skillshot time.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        float GetSpellHitTime(Vector2 position);
    }
}
