using System;
using System.Collections.Generic;
using EnsoulSharp;
using EnsoulSharp.SDK;
using SharpDX;

namespace Overwrite
{
    public class NewTargetSelector : ITargetSelector
    {
        public AIHeroClient SelectedTarget { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int GetDefaultPriority(AIHeroClient target)
        {
            throw new NotImplementedException();
        }

        public int GetPriority(AIHeroClient target)
        {
            throw new NotImplementedException();
        }

        public AIHeroClient GetTarget(IEnumerable<AIHeroClient> possibleTargets, DamageType damageType, bool ignoreShields = true, Vector3? checkFrom = null)
        {
            throw new NotImplementedException();
        }

        public AIHeroClient GetTarget(float range, DamageType damageType, bool ignoreShields = true, Vector3? checkFrom = null, IEnumerable<AIHeroClient> ignoreChampions = null)
        {
            throw new NotImplementedException();
        }

        public List<AIHeroClient> GetTargets(float range, DamageType damageType, bool ignoreShields = true, Vector3? checkFrom = null, IEnumerable<AIHeroClient> ignoreChampions = null)
        {
            throw new NotImplementedException();
        }
    }
}
