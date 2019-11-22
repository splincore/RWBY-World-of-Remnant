﻿using RimWorld;
using Verse;
using Verse.Sound;

namespace RWBYRemnant
{
    public class ThingDef_HookBullet : ThingDef
    {
        public SoundDef hookSound;
        public bool hookOnlyHostile;
        public ThingDef returnBullet;
    }

    public class Projectile_HookBullet : Bullet
    {
        public ThingDef_HookBullet Def
        {
            get
            {
                return def as ThingDef_HookBullet;
            }
        }

        protected override void Impact(Thing hitThing)
        {
            if (Def != null && hitThing != null && hitThing is Pawn hitPawn)
            {
                if (Def.hookOnlyHostile && !hitPawn.Faction.HostileTo(launcher.Faction))
                {
                    return;
                }
                SoundInfo info = SoundInfo.InMap(new TargetInfo(launcher.PositionHeld, launcher.MapHeld, false), MaintenanceType.None);
                Def.hookSound?.PlayOneShot(info);
                Projectile projectile = (Projectile)GenSpawn.Spawn(Def.returnBullet, hitPawn.Position, hitPawn.Map);
                projectile.Launch(hitPawn, launcher.Position, launcher.Position, ProjectileHitFlags.IntendedTarget);
            }
            Destroy(DestroyMode.Vanish);
        }
    }
}
