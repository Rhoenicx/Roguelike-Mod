using Microsoft.Xna.Framework;
using Terraria;
using System.IO;
using Terraria.ID;
using Roguelike.Common.Systems.ArtifactSystem;
using Roguelike.Contents.Items.Consumable.Potion;
using Roguelike.Contents.Items.Consumable.SpecialReward;
using Roguelike.Contents.Transfixion.Artifacts;
using Roguelike.Common.Global;
using Roguelike.Common.Systems.ObjectSystem;
using Roguelike.Contents.Items.NoneSynergy;
using Roguelike.Contents.Transfixion.Perks;
using Roguelike.Contents.Transfixion.Skill;

namespace Roguelike {
	partial class Roguelike {
		internal enum MessageType : byte {
			SkillIssuePlayer,
			DrugSyncPlayer,
			NoHitBossNum,
			GambleAddiction,
			GodUltimateChallenge,
			Perk,
			Skill,
			Artifact,
			PlayerStatsHandle,
			SyncModObject,
			RequestModObject
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			MessageType msgType = (MessageType)reader.ReadByte();
			byte playernumber = reader.ReadByte();
			switch (msgType) {
				case MessageType.NoHitBossNum:
					NoHitPlayerHandle nohitplayer = Main.player[playernumber].GetModPlayer<NoHitPlayerHandle>();
					nohitplayer.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						nohitplayer.SyncPlayer(-1, whoAmI, false);
					}
					break;
				case MessageType.SkillIssuePlayer:
					SkillIssuedArtifactPlayer SkillISsue = Main.player[playernumber].GetModPlayer<SkillIssuedArtifactPlayer>();
					SkillISsue.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						SkillISsue.SyncPlayer(-1, whoAmI, false);
					}
					break;
				case MessageType.DrugSyncPlayer:
					WonderDrugPlayer drugplayer = Main.player[playernumber].GetModPlayer<WonderDrugPlayer>();
					drugplayer.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						drugplayer.SyncPlayer(-1, whoAmI, false);
					}
					break;
				case MessageType.GambleAddiction:
					GamblePlayer gamble = Main.player[playernumber].GetModPlayer<GamblePlayer>();
					gamble.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						gamble.SyncPlayer(-1, whoAmI, false);
					}
					break;
				case MessageType.GodUltimateChallenge:
					ModdedPlayer moddedplayer = Main.player[playernumber].GetModPlayer<ModdedPlayer>();
					moddedplayer.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						moddedplayer.SyncPlayer(-1, whoAmI, false);
					}
					break;
				case MessageType.Perk:
					PerkPlayer perkplayer = Main.player[playernumber].GetModPlayer<PerkPlayer>();
					perkplayer.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						perkplayer.SyncPlayer(-1, whoAmI, false);
					}
					break;
				//case MessageType.Skill:
				//	SkillHandlePlayer skillplayer = Main.player[playernumber].GetModPlayer<SkillHandlePlayer>();
				//	skillplayer.ReceivePlayerSync(reader);
				//	if (Main.netMode == NetmodeID.Server) {
				//		skillplayer.SyncPlayer(-1, whoAmI, false);
				//	}
				//	break;
				case MessageType.Artifact:
					ArtifactPlayer artifactPlayer = Main.player[playernumber].GetModPlayer<ArtifactPlayer>();
					artifactPlayer.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						artifactPlayer.SyncPlayer(-1, whoAmI, false);
					}
					break;
				case MessageType.PlayerStatsHandle:
					PlayerStatsHandle statplayer = Main.player[playernumber].GetModPlayer<PlayerStatsHandle>();
					statplayer.ReceivePlayerSync(reader);
					if (Main.netMode == NetmodeID.Server) {
						statplayer.SyncPlayer(-1, whoAmI, false);
					}
					break;

				case MessageType.SyncModObject: {
					// Read all the data from the packet
					ushort index = reader.ReadUInt16();
					bool active = reader.ReadBoolean();
					short type = reader.ReadInt16();
					var posistion = reader.ReadVector2();
					var velocity = reader.ReadVector2();
					float rotation = reader.ReadSingle();

					// Get the object
					var modObject = ObjectSystem.Objects[index];

					// The object got killed (active on this client but packet says not active)
					if (modObject.active && !active) {
						modObject.OnKill();
						modObject.active = false;
					}

					// The object is newly created or not of the correct type
					else if (active && !modObject.active || modObject.Type != type) {
						ObjectSystem.Objects[index] = ObjectSystem.GetModObject(type);
						modObject = ObjectSystem.Objects[index];
						modObject.SetDefaults();
						modObject.active = true;
					}

					// Apply the received data to the object
					modObject.position = posistion;
					modObject.velocity = velocity;
					modObject.rotation = rotation;

					// ModObject specific data
					modObject.ReceiveExtraData(reader);
				}
				break;

				case MessageType.RequestModObject: {
					// Read all the data from the packet
					var position = reader.ReadVector2();
					var velocity = reader.ReadVector2();
					short type = reader.ReadInt16();
					
					// Spawn the ModObject (on the server)
					if (Main.dedServ) {
						ModObject.NewModObject(
							null,
							position,
							velocity,
							type);
					}
				}
				break;
			}
		}
	}
}
