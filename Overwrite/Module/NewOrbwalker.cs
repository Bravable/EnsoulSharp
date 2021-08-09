using System;
using EnsoulSharp;
using EnsoulSharp.SDK;
using SharpDX;

namespace Overwrite
{
    public class NewOrbwalker : IOrbwalker
    {
        public AttackableUnit ForceTarget { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AttackableUnit LastTarget { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public OrbwalkerMode ActiveMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int LastAutoAttackTick { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int LastMovementTick { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool AttackEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool MoveEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Attack(AttackableUnit target)
        {
            throw new NotImplementedException();
        }

        public bool CanAttack()
        {
            throw new NotImplementedException();
        }

        public bool CanAttack(float extraWindup)
        {
            throw new NotImplementedException();
        }

        public bool CanMove()
        {
            throw new NotImplementedException();
        }

        public bool CanMove(float extraWindup, bool disableMissileCheck)
        {
            throw new NotImplementedException();
        }

        public AttackableUnit GetTarget()
        {
            throw new NotImplementedException();
        }

        public bool IsAutoAttack(string name)
        {
            throw new NotImplementedException();
        }

        public bool IsAutoAttackReset(string name)
        {
            throw new NotImplementedException();
        }

        public void Move(Vector3 position)
        {
            throw new NotImplementedException();
        }

        public void Orbwalk(AttackableUnit target, Vector3 position)
        {
            throw new NotImplementedException();
        }

        public void ResetAutoAttackTimer()
        {
            throw new NotImplementedException();
        }

        public void SetAttackPauseTime(int time)
        {
            throw new NotImplementedException();
        }

        public void SetAttackServerPauseTime()
        {
            throw new NotImplementedException();
        }

        public void SetMovePauseTime(int time)
        {
            throw new NotImplementedException();
        }

        public void SetMoveServerPauseTime()
        {
            throw new NotImplementedException();
        }

        public void SetOrbwalkerPosition(Vector3 position)
        {
            throw new NotImplementedException();
        }

        public void SetPauseTime(int time)
        {
            throw new NotImplementedException();
        }

        public void SetServerPauseTime()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
