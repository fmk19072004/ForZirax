using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI;
using Terraria.GameContent.ItemDropRules;
using System;
using HoloFungusIsolated.Content.Projectiles;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace HoloFungusIsolated.Content.NPCs
{
    public class HoloFungus : ModNPC  //the fungus but not
    {
        private bool secondPhaseTriggered = false;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3; //The frames are plentiful on this one
        }

        public override void SetDefaults()
        {
            NPC.width = 240;
            NPC.height = 255;
            NPC.damage = 110;
            NPC.defense = 16;
            NPC.lifeMax = 9460;
            NPC.value = 0f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1; // Custom AI, no clue wtf the others even do
            NPC.boss = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false; // do not fall through world. Thanks: You.
            NPC.lavaImmune = true;
            Music = MusicID.Boss2;
        }

        public override void AI()
        {
            NPC.TargetClosest();
            //keep track of phase
            if (!secondPhaseTriggered && NPC.life < NPC.lifeMax / 2)
            {
                secondPhaseTriggered = true;

                //surge of attacks immediately upon phase 2
                HolographyAttack();
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + 50, (int)NPC.position.Y + 90, ModContent.NPCType<HoloMinion>());
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X - 50, (int)NPC.position.Y + 80, ModContent.NPCType<HoloMinion>());
            }

            if (NPC.target >= 0 && NPC.target < Main.maxPlayers)
            {
                Player player = Main.player[NPC.target];

                // when fungus is a bit obstructed make it engage the player
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

                        int numNewProjectiles = 36;
                        for (int i = 0; i < numNewProjectiles; i++)
                        {
                            float angle = MathHelper.ToRadians(360f / numNewProjectiles * i);
                            Vector2 spawnVelocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 2f;
                            //below is the projectile creation, typecasting will round down to 27
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, spawnVelocity, ModContent.ProjectileType<GlitchingLaser>(), (int)NPC.damage / 4, 1f);
                        }

                        NPC.ai[1] = 0;
                    }

                    NPC.ai[2]++;
                    if (NPC.ai[2] >= 180)
                    {
                        int spawnX = (int)NPC.position.X + Main.rand.Next(-50, 50);
                        int spawnY = (int)NPC.position.Y + 50;

                        int minionID = NPC.NewNPC(NPC.GetSource_FromAI(), spawnX, spawnY, ModContent.NPCType<HoloMinion>());  //summon the several

                        Main.npc[minionID].target = NPC.target; //apparently this is how to target the player
                        Main.npc[minionID].velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), -3f); //jump towards the player

                        NPC.ai[2] = 0;  // do not the forget this.
                    }

                    NPC.ai[3]++;
                    if (NPC.ai[3] % 50 == 0 && secondPhaseTriggered)
                    {
                        Vector2 direction = player.Center - NPC.Center;
                        direction.Normalize();
                        Vector2 velocityVector = direction * 2f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocityVector, ModContent.ProjectileType<EnergyBlast>(), (int)(NPC.damage / 2.5f), 1f);
                    }
                    if (NPC.ai[3] >= 3000 && NPC.life < NPC.lifeMax / 2) // when below half we start 
                    {
                        HolographyAttack();

                        NPC.ai[3] = 0;
                    }
                }
            }
            else
            {
                NPC.TargetClosest();
            }
        }

        private int frameCounter;

        public override void FindFrame(int frameHeight)
        {
            frameCounter++;
            if (frameCounter >= 10) //switch to next sprite in spritehseet every 10 frames (high level animation)
            {
                frameCounter = 0;
                NPC.frame.Y += frameHeight; //next sprite

                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight) //letzes erreicht, geh von vorne
                {
                    NPC.frame.Y = 0;
                }
            }
        }



        //helpers
        // this stuff has some AI code that I pasted in when I started to get a bit frustrated hoping for a magical solution but the AI wasn't as magical as I hoped :(
        int hologramCount = 4; //keep in mind that one extra hologram is created in initial position, so  this is 4 + 1 main boss
        int spread = 1000;
        float protectionBox = 320f;
        List<Vector2> spawnPositions;
        private void HolographyAttack()
        {
            Vector2 originalPosition = NPC.Center;

            List<Vector2> candidates = GetDonutPositions(originalPosition, protectionBox, spread, 16);

            // try finding a valid location for boss
            Vector2 bossNewPosition = FindValidSpawnPosition(candidates, NPC.width, NPC.height);

            if (bossNewPosition == Vector2.Zero)
            {
                // boxed in, kill the tiles.
                bossNewPosition = originalPosition + new Vector2(Main.rand.Next(-spread, spread), Main.rand.Next(-spread, spread));
                ClearTileCircle(bossNewPosition, NPC.width / 2 + 16); // radius slightly larger than boss
            }

            //move
            NPC.Center = bossNewPosition;
            NPC.netUpdate = true;

            //if the thing floats freely this is the time to make cloud
            PlaceSupportIfNeeded(bossNewPosition, NPC.width);

            //spawn hologram at old location
            NPC.NewNPC(NPC.GetSource_FromAI(), (int)originalPosition.X, (int)originalPosition.Y, ModContent.NPCType<HoloFungusHologram>());

            //pick hologram locations
            List<Vector2> holoPositions = PickHologramPositions(candidates, hologramCount, protectionBox);
            foreach (var pos in holoPositions)
            {
                PlaceSupportIfNeeded(pos, NPC.width);
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)pos.X, (int)pos.Y, ModContent.NPCType<HoloFungusHologram>());
            }
        }

        
        //this is the one that gives positions in range
        List<Vector2> GetDonutPositions(Vector2 center, float innerRadius, float outerRadius, float step)
        {
            List<Vector2> positions = new(); // list of vectors that all lead to valid positions on the donut

            for (float x = -outerRadius; x <= outerRadius; x += step)  // von allen x auf der einen seite bis zu allen x auf der anderen
            {
                for (float y = -outerRadius; y <= outerRadius; y += step)  //von allen y auf der einen seite zu allen y auf der anderen
                {
                    Vector2 offset = new Vector2(x, y);
                    float dist = offset.Length();       //self explanatory, just gets a potentially valid point (rectangle) and the distance from centre

                    if (dist >= innerRadius && dist <= outerRadius)  // checks for validty on donut
                    {
                        Vector2 candidate = center + offset;
                        positions.Add(candidate);
                    }
                }
            }

            return positions;
        }

        //this is the one to check for terrain clipping
        private bool IsUnobstructed(Vector2 pos, int width, int height)
        {
            Rectangle area = new((int)(pos.X - width / 2), (int)(pos.Y - height / 2), width, height);
            for (int x = area.Left / 16; x < area.Right / 16; x++)
            {
                for (int y = area.Top / 16; y < area.Bottom / 16; y++)
                {
                    if (!WorldGen.InWorld(x, y)) return false;

                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //this is the one that asks the other one if there is clipping
        private Vector2 FindValidSpawnPosition(List<Vector2> positions, int width, int height)
        {
            foreach (Vector2 pos in positions.OrderBy(_ => Main.rand.Next()))
            {
                if (IsUnobstructed(pos, width, height))
                {
                    return pos;
                }
            }
            return Vector2.Zero; // default case if no valid found
        }

        //the one that checks for floating and fixes floating if needed
        private void PlaceSupportIfNeeded(Vector2 pos, int width)
        {
            int groundCheckX = (int)(pos.X / 16);
            int groundCheckY = (int)(pos.Y / 16) + 1 + (NPC.height / 16);

            bool hasGround = false;
            for (int x = groundCheckX - 1; x <= groundCheckX + 1; x++)
            {
                for (int y = groundCheckY; y <= groundCheckY + 2; y++)
                {
                    if (!WorldGen.InWorld(x, y)) continue;

                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        hasGround = true;
                        break;
                    }
                }
            }

            if (!hasGround)
            {
                for (int x = groundCheckX - width / 32; x <= groundCheckX + width / 32; x++)
                {
                    for (int y = groundCheckY; y <= groundCheckY + 1; y++)
                    {
                        if (WorldGen.InWorld(x, y))
                        {
                            WorldGen.PlaceTile(x, y, TileID.Cloud, true, true);
                        }
                    }
                }
            }
        }

        //The one that clears an obstructed area if it is needed
        private void ClearTileCircle(Vector2 center, int radius)
        {
            int startX = (int)(center.X - radius) / 16;
            int endX = (int)(center.X + radius) / 16;
            int startY = (int)(center.Y - radius) / 16;
            int endY = (int)(center.Y + radius) / 16;

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (!WorldGen.InWorld(x, y)) continue;

                    float dist = Vector2.Distance(center, new Vector2(x * 16, y * 16));
                    if (dist <= radius)
                    {
                        WorldGen.KillTile(x, y, false, false, true);
                    }
                }
            }
        }


        List<Vector2> PickHologramPositions(List<Vector2> candidates, int count, float exclusionRadius)
        {
            List<Vector2> results = new();
            float exclusionRadiusSq = exclusionRadius * exclusionRadius; // wow you found out how to square

            while (candidates.Count > 0 && results.Count < count)  // would be nice if we run out of holograms to make before we run out of candidates to ue
            {
                Vector2 chosen = candidates[Main.rand.Next(candidates.Count)]; //a lucky winner
                results.Add(chosen);


                candidates = candidates
                    .Where(p => Vector2.DistanceSquared(p, chosen) > exclusionRadiusSq)  //kill all bastards that are too close to chosen
                    .ToList(); // we need: list
            }

            return results;
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