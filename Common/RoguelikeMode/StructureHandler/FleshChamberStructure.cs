using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roguelike.Common.Global;
using Roguelike.Common.Systems.ObjectSystem;
using Roguelike.Common.Systems.ObjectSystem.Contents;
using Roguelike.Common.Utils;
using Roguelike.Common.Wrapper;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Roguelike.Common.RoguelikeMode.StructureHandler;
public class Structure_FleshChamber : ModStructure {
	public override void CreateStructure(Mod mod, RogueLikeWorldGen system) {
		int X = 22 * RogueLikeWorldGen.GridPart_X + RogueLikeWorldGen.Rand.Next(RogueLikeWorldGen.GridPart_X / 2);
		int Y = 19 * RogueLikeWorldGen.GridPart_Y + RogueLikeWorldGen.Rand.Next(RogueLikeWorldGen.GridPart_Y / 2);
		var data = ModWrapper.Get_StructureData("Assets/BossChamber/FleshChamber", mod);
		int Width = data.width / 2;
		int Height = data.height / 2;
		Point16 point = new(X - Width, Y - Height);
		Rectangle FleshStructure = new(point.X, point.Y, data.width, data.height);
		if (ModWrapper.IsInBound(data, point)) {
			ModWrapper.GenerateFromData(data, point);
			system.ZoneToBeIgnored.Add(FleshStructure);
			system.Set_MapIgnoredZoneIntoWorldGen(FleshStructure);
			system.SaveStructureLocation("FleshChamber", FleshStructure);
		}
	}
}
internal class FleshChamberStructure : ModSystem {
	public Point Point_ModObject => new Point(25, 29);
	public Rectangle Pos_structure => ModContent.GetInstance<RogueLikeWorldGen>().GetStructure("FleshChamber");
	public bool IsWithinRange = false;
	public override void PostUpdateEverything() {
		if (!RoguelikeWorldProperty.RoguelikeWorld || Main.dedServ) {
			return;
		}
		var player = Main.LocalPlayer;
		if (player.Center.IsCloseToPosition(Pos_structure.Center().ToWorldCoordinates(), 1500)) {
			if (!IsWithinRange) {
				var worldPos = (Pos_structure.Location + Point_ModObject).ToWorldCoordinates();
				
				switch (Main.netMode) {
					case NetmodeID.SinglePlayer:
						ModObject.NewModObject(player.GetSource_FromThis(), worldPos, Vector2.Zero, ModObject.GetModObjectType<Sealed_Eye>());
					break;

					case NetmodeID.MultiplayerClient: {
						var packet = Mod.GetPacket();
						packet.Write((byte)Roguelike.MessageType.RequestModObject);
						packet.WriteVector2(worldPos);
						packet.WriteVector2(Vector2.Zero);
						packet.Write((short)ModObject.GetModObjectType<Sealed_Eye>());
						packet.Send();
						break;
					}
				}
				
				
			}
			IsWithinRange = true;
		}
		else {
			IsWithinRange = false;
		}
	}
}
public class Fix_SusEye : GlobalItem {
	public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
		return entity.type == ItemID.SlimeCrown;
	}
	public override bool CanUseItem(Item item, Player player) {
		return (ModContent.GetInstance<FleshChamberStructure>().Pos_structure.Center.ToWorldCoordinates() - player.Center).LengthSquared() <= 360000;
	}
	public override bool? UseItem(Item item, Player player) {
		var spawnPosotion = ModContent.GetInstance<FleshChamberStructure>().Pos_structure.Location.ToWorldCoordinates().ToPoint();
		NPC.SpawnBoss(spawnPosotion.X, spawnPosotion.Y, NPCID.EyeofCthulhu, player.whoAmI);
		return true;
	}
}
public class Sealed_Eye : NPCSealedObject {
	public int AuraCounter = 0;
	public bool Switch = false;
	public override int NPCTypeToFollow => NPCID.EyeofCthulhu;
	public override void NPCObject_SetDefaults() {
		frame = 6;
	}
	public override void Inner_AI() {
		if (AuraCounter < 100 && !Switch) {
			AuraCounter++;
		}
		if (AuraCounter > 0 && Switch) {
			AuraCounter--;
		}
		if (AuraCounter >= 100) {
			Switch = true;
		}
		if (AuraCounter <= 0) {
			Switch = false;
		}
		if (++Counter >= 10) {
			frameCounter = ModUtils.Safe_SwitchValue(frameCounter, 3);
			Counter = 0;
		}
		if (NPC.downedBoss1 || NPC.AnyNPCs(NPCID.EyeofCthulhu)) {
			Kill();
		}
	}
	public override void Inner_Draw(SpriteBatch spritebatch) {
		Main.instance.LoadNPC(NPCTypeToFollow);
		var texture = TextureAssets.Npc[NPCTypeToFollow].Value;
		var origin = texture.Size() * .5f;
		var drawpos = position - Main.screenPosition + origin * .75f;
		var color = Color.White;
		frameCounter = Math.Clamp(frameCounter, 0, frame);
		spritebatch.Draw(texture, drawpos, texture.Frame(1, frame, 0, frameCounter), color, 0, origin, 1f, SpriteEffects.None, 1);
		BasicSealAuraEffect(spritebatch, AuraCounter, Vector2.UnitX * origin.X * .75f - Vector2.UnitY * origin.Y / frame * .5f, Color.IndianRed, 4f);
	}
}
