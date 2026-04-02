using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.DataStructures;
using System;
using Terraria.Audio;
using Microsoft.Build.Evaluation;

namespace HoloFungusIsolated.Content.Projectiles
{
    public class EnergyBlast : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = ProjAIStyleID.SmallFlying;
        }

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Vector2 directionVector = Projectile.velocity;
            directionVector.Normalize();
            Projectile.velocity = directionVector * 11f;
        }

        public override void AI()
        {
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            float homingSpeed = 8f;
            float lerpFrames = 12f;
            float maxDistance = 800f;

            Player target = FindClosestPlayer(Projectile.Center, maxDistance);

            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homingSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 1f / lerpFrames);
            }      
        }
        
        Player FindClosestPlayer(Vector2 position, float maxDistance)
        {
            Player closest = null;
            float closestDist = maxDistance;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float dist = Vector2.Distance(player.Center, position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = player;
                    }
                }
            }

            return closest;
        }
    }
}
