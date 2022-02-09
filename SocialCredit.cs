using System.Linq;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Localization; 
using Terraria.Utilities;
using Terraria.GameContent.Dyes;
using Terraria.GameContent.UI;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;
using Terraria.UI.Chat;

namespace SocialCredit
{
	public class SocialCredit : Mod
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int rulerLayerIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Ruler"));
			if (rulerLayerIndex != -1) {
				layers.Insert(rulerLayerIndex, new LegacyGameInterfaceLayer(
					"SocialCredit: SocialBar",
					delegate {
							if (Main.netMode == NetmodeID.SinglePlayer) {drawDedCount(Main.LocalPlayer);}
							else {for (int i = 0; i < Main.maxPlayers; i++){drawDedCount(Main.player[i]);}}
						return true;
					},
					InterfaceScaleType.Game)
				);
			}
		}
		void drawDedCount(Player player) {
			if (player != null && !player.dead && player.active) {
				int socialCredit = player.GetModPlayer<SocialPlayer>().socialCredit;
				int maxSocialCredit = player.GetModPlayer<SocialPlayer>().maxSocialCredit;
				string text = $"{socialCredit} / {maxSocialCredit}";
				Vector2 messageSize = ChatManager.GetStringSize(Main.fontMouseText, text, Vector2.One);
				Vector2 pos = player.Center - Main.screenPosition + new Vector2(0,player.height + 10);
				pos.X -= messageSize.X/2f;
				ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, 
				Main.fontMouseText, text, pos, Color.LightYellow, 0, Vector2.Zero, Vector2.One);
			}
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			Main.player[reader.ReadByte()].GetModPlayer<SocialPlayer>().HandlePacket(reader);
		}
		public class SocialPlayer : ModPlayer
		{
			public int socialCredit;
			public int maxSocialCredit = 999;
			public void Bad(int bad) {
				socialCredit -= bad;
				CombatText.NewText(player.getRect(),Color.Red,$"-{bad} social credit");
			}
			public void Good(int good) {
				socialCredit += good;
				CombatText.NewText(player.getRect(),Color.LightGreen,$"+{good} social credit");
			}
			bool sayGood;
			public override void PostUpdateMiscEffects() {
				if (socialCredit > maxSocialCredit) {socialCredit = maxSocialCredit;}
				if (socialCredit <= -1 && player.active && !player.dead) {
					player.Hurt(PlayerDeathReason.ByCustomReason($"{player.name} got nuked by the goverment"), 99999999, player.direction);
					Nuke();
					player.dead = true;
				}
				if (player.chatOverhead.chatText != null && player.chatOverhead.chatText != "" && player.chatOverhead.timeLeft > 0) {
					string text = player.chatOverhead.chatText.ToUpper();
					if (!sayGood && text.Contains("TAIWAN") && text.Contains("COUNTRY")) {
						if (text.Contains("NOT")) {Good(10);}
						else {Bad(999999);}
						sayGood = true;
					}
					if (!sayGood && text.Contains("TIANMEN SQUARE")) {
						if (text.Contains("NOTHING")) {Good(10);}
						else {Bad(999999);}
						sayGood = true;
					}
					if (!sayGood && text == "I HAVE 2 KIDS") {
						Bad(100);
						sayGood = true;
					}
					if (!sayGood && text == "CHINA IS BAD") {
						Bad(100);
						sayGood = true;
					}
					if (!sayGood && text == "CHINA BAD") {
						Bad(100);
						sayGood = true;
					}
					if (!sayGood && text == "CHINA IS GREAT") {
						Good(10);
						sayGood = true;
					}
					if (!sayGood && text == "CHINA IS GOOD") {
						Good(10);
						sayGood = true;
					}
					if (!sayGood && text == "CHINA IS LIFE") {
						Good(10);
						sayGood = true;
					}
					if (!sayGood && text == "GLORY TO THE CHINA") {
						Good(10);
						sayGood = true;
					}
					if (!sayGood && text.Contains("WINNIE THE POOH")) {
						Bad(100);
						sayGood = true;
					}
				}
				else {
					sayGood = false;
				}
			}
			public void HandlePacket(BinaryReader reader) {
				//read
				socialCredit = reader.ReadInt32();
				maxSocialCredit = reader.ReadInt32();
			}
			public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)player.whoAmI);
				packet.Write(socialCredit);
				packet.Write(maxSocialCredit);
				packet.Send(toWho, fromWho);
			}

			//Save and Load
			public override TagCompound Save() {
				TagCompound tag = new TagCompound();
				tag.Add("socialCredit",socialCredit);
				tag.Add("maxSocialCredit",maxSocialCredit);
				return tag;
			}
			public override void Load(TagCompound tag) {
				socialCredit = tag.GetInt("socialCredit");
				maxSocialCredit = tag.GetInt("maxSocialCredit");
			}
			public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit) {
				int dam = (int)damage/2;
				Bad(dam);
			}
			public override void OnRespawn(Player player) {
				player.GetModPlayer<SocialPlayer>().socialCredit = 100;
				Mod mod = ModLoader.GetMod("CalamityMod");
				if (mod != null) {
					player.GetModPlayer<SocialPlayer>().Bad(80);
				}
				mod = ModLoader.GetMod("CommunistTerraria");
				if (mod != null) {
					player.GetModPlayer<SocialPlayer>().Good(69);
				}
				mod = ModLoader.GetMod("Luiafk");
				if (mod != null) {
					player.GetModPlayer<SocialPlayer>().Bad(10);
				}
			}
			public override void OnEnterWorld(Player player) {
				Main.NewText("Please dont do anything that may result in subtracting social credit !",Color.Red);
				player.GetModPlayer<SocialPlayer>().OnRespawn(player);
			}
			public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
				int ran = Main.rand.Next(9,12);
				Main.NewText($"Reduced {player.name} max social credit by {ran}",Color.Red);
				maxSocialCredit -= ran;
			}
			public override void OnHitNPCWithProj(Projectile proj, NPC target, int damage, float knockback, bool crit) {
				OnHitNPC(null,target, damage, knockback, crit);
			}
			public override void OnHitNPC(Item item, NPC npc, int damage, float knockback, bool crit) {
				if (npc.type == NPCID.Nurse) {Good(1);}
				if (npc.type == NPCID.Worm) {Bad(1);}
				if (npc.type == NPCID.Bunny) {
					Bad(10);
				}
				else if (npc.life <= 0) {
					Good(Main.rand.Next(1,3));
					if (npc.type == NPCID.Nurse) {
						Good(100);
					}
					if (npc.boss) {
						Good(20);
					}
				}
			}
			public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item) {
				Bad(1);
			}
			public override void PostNurseHeal(NPC nurse, int health, bool removeDebuffs, int price) {
				Bad(50);
			}
			void Nuke() {
				Projectile projectile = new Projectile();
				projectile.position = player.position;
				projectile.width = 100;
				projectile.height = 100;
				Main.PlaySound(SoundID.Item15, projectile.position);
				// Smoke Dust spawn
				for (int i = 0; i < 50; i++) {
					int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 31, 0f, 0f, 100, default(Color), 2f);
					Main.dust[dustIndex].velocity *= 1.4f;
				}
				// Fire Dust spawn
				for (int i = 0; i < 80; i++) {
					int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 6, 0f, 0f, 100, default(Color), 3f);
					Main.dust[dustIndex].noGravity = true;
					Main.dust[dustIndex].velocity *= 5f;
					dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 6, 0f, 0f, 100, default(Color), 2f);
					Main.dust[dustIndex].velocity *= 3f;
				}
				// Large Smoke Gore spawn
				for (int g = 0; g < 2; g++) {
					int goreIndex = Gore.NewGore(new Vector2(projectile.position.X + (float)(projectile.width / 2) - 24f, projectile.position.Y + (float)(projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), 1f);
					Main.gore[goreIndex].scale = 1.5f;
					Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1.5f;
					Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1.5f;
					goreIndex = Gore.NewGore(new Vector2(projectile.position.X + (float)(projectile.width / 2) - 24f, projectile.position.Y + (float)(projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), 1f);
					Main.gore[goreIndex].scale = 1.5f;
					Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1.5f;
					Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1.5f;
					goreIndex = Gore.NewGore(new Vector2(projectile.position.X + (float)(projectile.width / 2) - 24f, projectile.position.Y + (float)(projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), 1f);
					Main.gore[goreIndex].scale = 1.5f;
					Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1.5f;
					Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y - 1.5f;
					goreIndex = Gore.NewGore(new Vector2(projectile.position.X + (float)(projectile.width / 2) - 24f, projectile.position.Y + (float)(projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), 1f);
					Main.gore[goreIndex].scale = 1.5f;
					Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1.5f;
					Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y - 1.5f;
				}
				// reset size to normal width and height.
				projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
				projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
				projectile.width = 10;
				projectile.height = 10;
				projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
				projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);

				// TODO, tmodloader helper method
				int explosionRadius = 20;
				int minTileX = (int)(projectile.position.X / 16f - (float)explosionRadius);
				int maxTileX = (int)(projectile.position.X / 16f + (float)explosionRadius);
				int minTileY = (int)(projectile.position.Y / 16f - (float)explosionRadius);
				int maxTileY = (int)(projectile.position.Y / 16f + (float)explosionRadius);
				if (minTileX < 0) {
					minTileX = 0;
				}
				if (maxTileX > Main.maxTilesX) {
					maxTileX = Main.maxTilesX;
				}
				if (minTileY < 0) {
					minTileY = 0;
				}
				if (maxTileY > Main.maxTilesY) {
					maxTileY = Main.maxTilesY;
				}
				bool canKillWalls = false;
				for (int x = minTileX; x <= maxTileX; x++) {
					for (int y = minTileY; y <= maxTileY; y++) {
						float diffX = Math.Abs((float)x - projectile.position.X / 16f);
						float diffY = Math.Abs((float)y - projectile.position.Y / 16f);
						double distance = Math.Sqrt((double)(diffX * diffX + diffY * diffY));
						if (distance < (double)explosionRadius && Main.tile[x, y] != null && Main.tile[x, y].wall == 0) {
							canKillWalls = true;
							break;
						}
					}
				}
				for (int i = minTileX; i <= maxTileX; i++) {
					for (int j = minTileY; j <= maxTileY; j++) {
						float diffX = Math.Abs((float)i - projectile.position.X / 16f);
						float diffY = Math.Abs((float)j - projectile.position.Y / 16f);
						double distanceToTile = Math.Sqrt((double)(diffX * diffX + diffY * diffY));
						if (distanceToTile < (double)explosionRadius) {
							bool canKillTile = true;
							if (Main.tile[i, j] != null && Main.tile[i, j].active()) {
								canKillTile = true;
								if (Main.tileDungeon[(int)Main.tile[i, j].type] || Main.tile[i, j].type == 88 || Main.tile[i, j].type == 21 || Main.tile[i, j].type == 26 || Main.tile[i, j].type == 107 || Main.tile[i, j].type == 108 || Main.tile[i, j].type == 111 || Main.tile[i, j].type == 226 || Main.tile[i, j].type == 237 || Main.tile[i, j].type == 221 || Main.tile[i, j].type == 222 || Main.tile[i, j].type == 223 || Main.tile[i, j].type == 211 || Main.tile[i, j].type == 404) {
									canKillTile = false;
								}
								if (!Main.hardMode && Main.tile[i, j].type == 58) {
									canKillTile = false;
								}
								if (!TileLoader.CanExplode(i, j)) {
									canKillTile = false;
								}
								if (canKillTile) {
									WorldGen.KillTile(i, j, false, false, false);
									if (!Main.tile[i, j].active() && Main.netMode != NetmodeID.SinglePlayer) {
										NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, (float)i, (float)j, 0f, 0, 0, 0);
									}
								}
							}
							if (canKillTile) {
								for (int x = i - 1; x <= i + 1; x++) {
									for (int y = j - 1; y <= j + 1; y++) {
										if (Main.tile[x, y] != null && Main.tile[x, y].wall > 0 && canKillWalls && WallLoader.CanExplode(x, y, Main.tile[x, y].wall)) {
											WorldGen.KillWall(x, y, false);
											if (Main.tile[x, y].wall == 0 && Main.netMode != NetmodeID.SinglePlayer) {
												NetMessage.SendData(MessageID.TileChange, -1, -1, null, 2, (float)x, (float)y, 0f, 0, 0, 0);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}