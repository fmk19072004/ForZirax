using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace HoloFungusIsolated.Content.NPCs
{
    public class HoloMinion : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 33;
            NPC.defense = 10;
            NPC.lifeMax = 110;
            NPC.value = 0f;
            NPC.knockBackResist = 0.5f;
            NPC.aiStyle = NPCAIStyleID.Bat; // Flying AI
            NPC.noGravity = true;
            NPC.scale = 1.25f;
            NPC.noTileCollide = true;
        }

        public override void AI()
        {
            Player target = Main.player[NPC.target];

            if (!target.active || target.dead)
            {
                NPC.TargetClosest();
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    NPC.velocity.Y = 0.1f; //if there's no action to perform, perform: Gravity (yes + means down for some reason)
                    return;
                }
            }

            // Chase the player
            Vector2 direction = target.Center - NPC.Center;
            direction.Normalize();
            direction *= 3f; // Adjust speed

            NPC.velocity = (NPC.velocity + direction) / 1.2f;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            target.AddBuff(BuffID.Electrified, 60); //get zapped idiot

            //after hitting once make sure it fucking dies
            NPC.life = -1;
            NPC.HitEffect();
            NPC.checkDead();
            NPC.active = false;
        }
    }
}
