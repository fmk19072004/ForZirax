using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI;
using Terraria.GameContent.ItemDropRules;
using System;
using HoloFungusIsolated.Content.Projectiles;


namespace HoloFungusIsolated.Content.NPCs
{
    public class HoloFungusHologram : ModNPC  //the fungus but not
    {
        private int thresholdToDespawn = 700;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4; //The frames are plentiful on this one
        }

        public override void SetDefaults()
        {
            NPC.width = 240;
            NPC.height = 255;
            NPC.damage = 1;
            NPC.defense = 0;
            NPC.lifeMax = 9460;
            NPC.value = 0f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1; // Custom AI, no clue wtf the others even do
            NPC.boss = false;
            NPC.noGravity = false;
            NPC.noTileCollide = false; // do not fall through world. Thanks: You.
            NPC.lavaImmune = true;
            Music = MusicID.Boss2;
        }

        public override void AI()
        {
            if (NPC.target >= 0 && NPC.target < Main.maxPlayers)
            {
                Player player = Main.player[NPC.target];

                int clippingTiles = CountClippingTiles(NPC);
                if (clippingTiles > 40)
                {
                    NPC.position.Y += 4;
                }

                if (NPC.ai[0] == 0)
                {
                    NPC.ai[0] = 1;

                    int tileX = (int)(NPC.position.X / 16); // Convert world X to tile X
                    int tileY = (int)(NPC.position.Y / 16); // Convert world Y to tile Y


                    while (tileY < Main.maxTilesY - 1 && !Main.tile[tileX, tileY].HasTile)
                    {
                        tileY++;
                    }

                    // Set boss position to be right above the solid tile idk if the code here is goofy or if the sprite is to bug but it still visually clips at least
                    NPC.position.Y = tileY * 16 - NPC.height;
                }


                //make pew pew at play play
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.ai[1]++;
                    if (NPC.ai[1] >= 90) //attck rate
                    {
                        Vector2 targetPosition = player.Center;
                        Vector2 direction = targetPosition - NPC.Center;
                        direction.Normalize();
                        direction *= 10f; //proj speed

                        int numNewProjectiles = 32;
                        for (int i = 0; i < numNewProjectiles; i++)
                        {
                            float angle = MathHelper.ToRadians(360f / numNewProjectiles * i);
                            Vector2 spawnVelocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 2f;
                            //1d projectiles
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, spawnVelocity, ModContent.ProjectileType<GlitchingLaser>(), 1, 1f);
                        }

                        NPC.ai[1] = 0;
                    }


                    NPC.ai[2]++;
                    if (NPC.ai[2] >= 2400) //duration
                    {
                        NPC.active = false;
                    }

                    NPC.ai[3]++;
                    if (NPC.ai[3] >= 100)
                    {
                        Vector2 direction = player.Center - NPC.Center;
                        direction.Normalize();
                        Vector2 velocityVector = direction * 2f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocityVector, ModContent.ProjectileType<EnergyBlast>(), (int)1, 1f);

                        NPC.ai[3] = 0;
                    }
                }
            }
            else
            {
                NPC.TargetClosest();
            }

            if (NPC.life < NPC.lifeMax - thresholdToDespawn)
            {
                NPC.active = false;
            }
        }



        //animation stiff
        private int frameCounter;

        public override void FindFrame(int frameHeight)
        {
            frameCounter++;
            if (frameCounter >= 10) 
            {
                frameCounter = 0;
                NPC.frame.Y += frameHeight; 
                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight) 
                {
                    NPC.frame.Y = 0;
                }
            }
        }
        
        int CountClippingTiles(NPC npc)
        {
            int count = 0;

            int startX = (int)(npc.position.X / 16);
            int startY = (int)(npc.position.Y / 16);
            int endX = (int)((npc.position.X + npc.width) / 16);
            int endY = (int)((npc.position.Y + npc.height) / 16);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (!WorldGen.InWorld(x, y))
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    {
                        // Check if the tile actually intersects the NPC’s hitbox
                        Rectangle tileHitbox = new Rectangle(x * 16, y * 16, 16, 16);
                        if (npc.Hitbox.Intersects(tileHitbox))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }
    }
}