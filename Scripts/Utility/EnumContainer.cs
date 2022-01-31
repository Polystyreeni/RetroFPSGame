using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnumContainer
{
    [System.Serializable]
    public enum DAMAGETYPE
    {
        Undefined,
        Melee,
        Bullet,
        Explosion,
        Fire
    }

    [System.Serializable]
    public enum DIFFICULTY
    {
        EASY,
        NORMAL,
        MEDIUM,
        HARD,
        INSANE
    }

    [System.Serializable]
    public enum PLAYERABILITY   // TODO: Add more abilities
    {
        DAMAGE,
        SPEED,
        TIME
    }

    public struct DamageInflictor
    {
        public int damage;
        public Transform attacker;
        public EnumContainer.DAMAGETYPE damageType;

        public DamageInflictor(int d, Transform t, EnumContainer.DAMAGETYPE dT)
        {
            damage = d;
            attacker = t;
            damageType = dT;
        }
    }
}
