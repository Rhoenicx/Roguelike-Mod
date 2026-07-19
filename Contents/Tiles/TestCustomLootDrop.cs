using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roguelike.Common.Systems.ObjectSystem;
using Roguelike.Common.Utils;
using Roguelike.Contents.Items.Lootbox;
using Roguelike.Texture;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Roguelike.Contents.Tiles;
public class TestCustomLootDropItem : ModItem {
	public override string Texture => ModTexture.MissingTexture_Default;
	public override void SetDefaults() {
		Item.width = Item.height = 30;
		Item.useTime = Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.createTile = ModContent.TileType<TestCustomLootDropModTile>();
	}
}
internal class TestCustomLootDropModTile : ModTile {
	public override string Texture => ModTexture.MissingTexture_Default;
	public override string HighlightTexture => ModTexture.MissingTexture_Default;
	public override void SetStaticDefaults() {
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileLighted[Type] = true;
		DustType = DustID.Stone;
		AddMapEntry(new Color(200, 200, 200));
	}
	public override bool RightClick(int i, int j) {
		WorldGen.KillTile(i, j, noItem: true);

		switch (Main.netMode) {
			case NetmodeID.SinglePlayer:
				ModObject.NewModObject(null, new Vector2(i, j).ToWorldCoordinates(), -Vector2.UnitY * 2,
					ModObject.GetModObjectType<TileDropModObject>());
			break;

			case NetmodeID.MultiplayerClient: {
				var packet = Mod.GetPacket();
				packet.Write((byte)Roguelike.MessageType.RequestModObject);
				packet.WriteVector2(new Vector2(i, j).ToWorldCoordinates());
				packet.WriteVector2(-Vector2.UnitY * 2);
				packet.Write((short)ModObject.GetModObjectType<TileDropModObject>());
				packet.Send();
				break;
			}
		}

		return base.RightClick(i, j);
	}
}
public class TileDropModObject : ModObject {
	public override void SetDefaults() {
		timeLeft = 360;
	}
	public override void AI() {
		velocity *= .98f;
		for (int i = 0; i < 2; i++) {
			Dust dust = Dust.NewDustDirect(Center, 0, 0, DustID.WhiteTorch);
			dust.noGravity = true;
			dust.fadeIn = 1.1f;
			dust.color = new(255, 255, 255, 0);
			dust.velocity = Main.rand.NextVector2CircularEdge(10, 1).RotatedBy(MathHelper.ToRadians(timeLeft * 2));
		}
		for (int i = 0; i < 4; i++) {
			Dust dust = Dust.NewDustDirect(Center, 0, 0, DustID.WhiteTorch);
			dust.noGravity = true;
			dust.fadeIn = 1.1f;
			dust.color = new(255, 255, 255, 0);
			dust.velocity = Main.rand.NextVector2Circular(2, 2);
		}
		if (timeLeft > 120) {
			return;
		}
		//Test lootdrop
		if (timeLeft % 10 == 0) {
			for (int i = 0; i < 25; i++) {
				Dust dust = Dust.NewDustDirect(Center, 0, 0, DustID.WhiteTorch);
				dust.noGravity = true;
				dust.color = new(255, 255, 255, 0);
				dust.velocity = Main.rand.NextVector2CircularEdge(10, 10);
				dust.scale += Main.rand.NextFloat(.21f, .5f);
			}
			SoundEngine.PlaySound(SoundID.Item9, position);
			ModUtils.GetWeapon(out int weapon, out int amount);
			int item = Item.NewItem(new EntitySource_Misc("SpawnFromModObject"), Center, weapon, amount);
			Main.item[item].velocity = -Vector2.UnitY.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-30, 31))) * Main.rand.NextFloat(3, 5);
		}
	}
	public override void Draw(SpriteBatch spritebatch) {

	}
	public override void OnKill() {
		for (int i = 0; i < 200; i++) {
			Dust dust = Dust.NewDustDirect(Center, 0, 0, DustID.WhiteTorch);
			dust.noGravity = true;
			dust.fadeIn = 1.1f;
			dust.color = new(255, 255, 255, 0);
			dust.velocity = Main.rand.NextVector2CircularEdge(10, 10);
			if (i > 100) {
				continue;
			}
			Dust dust2 = Dust.NewDustDirect(Center, 0, 0, DustID.WhiteTorch);
			dust2.noGravity = true;
			dust2.fadeIn = 1.1f;
			dust2.color = new(255, 255, 255, 0);
			dust2.velocity = Main.rand.NextVector2CircularEdge(3, 10);

			Dust dust3 = Dust.NewDustDirect(Center, 0, 0, DustID.WhiteTorch);
			dust3.noGravity = true;
			dust3.fadeIn = 1.1f;
			dust3.color = new(255, 255, 255, 0);
			dust3.velocity = Main.rand.NextVector2CircularEdge(10, 3);
		}
	}
}
