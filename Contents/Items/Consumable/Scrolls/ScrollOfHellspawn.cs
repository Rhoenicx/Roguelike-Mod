using Microsoft.Xna.Framework;
using Roguelike.Common.Systems.ObjectSystem;
using Roguelike.Common.Utils;
using Roguelike.Texture;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Roguelike.Contents.Items.Consumable.Scrolls;

class ScrollOfHellspawn : ModItem {
	public override void SetStaticDefaults() {
		ModItemLib.LootboxPotion.Add(Item);
	}
	public override void SetDefaults() {
		Item.BossRushDefaultToConsume(32, 32);
		Item.maxStack = 99;
	}
	public override bool? UseItem(Player player) {
		if (player.ItemAnimationJustStarted) {
			var limitedSpawningPosition = Main.MouseWorld;
			var distance = player.Center - limitedSpawningPosition;
			if (distance.Length() > 350f) {
				limitedSpawningPosition = player.Center - distance.SafeNormalize(Vector2.Zero) * 350;
			}
			
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ModObject.NewModObject(Item.GetSource_FromThis(), limitedSpawningPosition, Vector2.Zero, ModObject.GetModObjectType<HellSpawnObject>());
		}
		return true;
	}
}
public class HellSpawnObject : ModObject {
	public override void SetDefaults() {
		timeLeft = ModUtils.ToSecond(20);
	}
	public override void AI() {
		float progress = timeLeft;
		for (int i = 0; i < 4; i++) {
			var dust = Dust.NewDustDirect(Center, 0, 0, DustID.LavaMoss);
			dust.velocity = Vector2.One.RotatedBy(MathHelper.ToRadians(progress + 90 * i)) * 2;
			dust.noGravity = true;
			dust.scale += 1;
		}
		Lighting.AddLight(Center, Color.Red.ToVector3());
		if (progress > ModUtils.ToSecond(19)) {
			return;
		}
		if (progress % 100 == 0) {
			for (int i = 0; i < 100; i++) {
				var dust = Dust.NewDustDirect(Center, 0, 0, DustID.DemonTorch);
				dust.velocity = Main.rand.NextVector2CircularEdge(10, 10);
				dust.noGravity = true;
				dust.scale += 1;
			}
			int NPCToSpawn = Main.rand.Next([NPCID.FireImp, NPCID.Lavabat, NPCID.LavaSlime, NPCID.Demon]);
			NPC.NewNPC(new EntitySource_WorldGen(), (int)Center.X, (int)Center.Y, NPCToSpawn);
		}
	}
}
