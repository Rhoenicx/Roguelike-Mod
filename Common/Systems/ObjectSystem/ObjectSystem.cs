using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Roguelike.Common.Systems.ObjectSystem;

/// <summary>
/// This is a object system where it handle player created object<br/>
/// While we can use Projectile, NPC to handle some event, because of certain system and mechanic within the mod<br/>
/// It can be broken or have unintended consequence, as such Object system is created to take care of that<br/>
/// <br/>
/// For more detail on what Object class can do, check the actual object class
/// </summary>
public class ObjectSystem : ModSystem {
	public const int MaxObjects = 1000;
	public static ModObject[] Objects = new ModObject[MaxObjects];
	private static readonly List<ModObject> ModObjectSample = new();
	public static int TotalCount => ModObjectSample.Count;
	public static ModObject GetModObject(int type) {
		return type >= 0 && type < ModObjectSample.Count ? ModObjectSample[type].Clone() : null;
	}
	public static int Register(ModObject obj) {
		ModTypeLookup<ModObject>.Register(obj);
		ModObjectSample.Add(obj);
		return ModObjectSample.Count - 1;
	}
	public override void Load() {
		Objects = new ModObject[MaxObjects];
	}
	public override void Unload() {
		for (int i = 0; i < ModObjectSample.Count; i++) {
			ModObjectSample[i].Unload();
		}
		Objects = null;
		ModObjectSample.Clear();
	}
	public override void PostSetupContent() {
		for (int i = 0; i < ModObjectSample.Count; i++) {
			ModObjectSample[i].Load();
		}
	}
	public static bool AnyModObjects(int type) {
		for (int i = 0; i < Objects.Length; i++) {
			ModObject modobject = Objects[i];
			if (modobject == null) {
				continue;
			}
			if (modobject.Type == type && modobject.active) {
				return true;
			}
		}
		return false;
	}
	public override void PostUpdateWorld() {
		for (int i = 0; i < Objects.Length; i++) {
			ModObject modobject = Objects[i];
			if (modobject == null) {
				continue;
			}
			if (modobject.active) {
				modobject.AI();
				modobject.position += modobject.velocity;
				if (modobject.timeLeft > 0) {
					modobject.timeLeft--;
				}
				else {
					modobject.OnKill();
					modobject.active = false;
				}
			}

			if (Main.netMode == NetmodeID.Server && modobject.netUpdate) {
				modobject.SendData();
				modobject.netUpdate = false;
			}
		}
	}
	public override void PostDrawTiles() {
		Main.spriteBatch.Begin();
		for (int i = 0; i < Objects.Length; i++) {
			ModObject modobject = Objects[i];
			if (modobject == null) {
				continue;
			}
			if (modobject.active) {
				modobject.Draw(Main.spriteBatch);
			}
		}
		Main.spriteBatch.End();
	}
	public override void OnWorldUnload() {
		for (int i = 0; i < Objects.Length; i++) {
			ModObject modobject = Objects[i];
			if (modobject == null) {
				continue;
			}
			modobject.Kill();
		}
	}
}
/// <summary>
/// This system is completely client side so using <see cref="Main.LocalPlayer"/> is completely valid<br/>
/// If you however want to check for collision with Projectile or NPC, do a manual radious check using <see cref="Main.ActiveProjectiles"/>, <see cref="Main.ActiveNPCs"/><br/>
/// This system is not to be confused with particle system, as this system is not uses to be as such
/// </summary>
public class ModObject : IModType, ILoadable {
	public ModObject() {
		whoAmI = -1;
		timeLeft = 3600;
		rotation = 0;
		velocity = Vector2.Zero;
		position = Vector2.Zero;
	}
	public int width, height, direction;
	/// <summary>
	/// The position of this Entity in world coordinates. Note that this corresponds to the top left corner of the entity. Use <see cref="Center"/> instead for logic that needs the position at the center of the entity.
	/// </summary>
	public Vector2 position;

	/// <summary>
	/// The velocity of this Entity in world coordinates per tick.
	/// </summary>
	public Vector2 velocity;
	/// <summary>
	/// The index of this Entity within its specific array. These arrays track the entities in the world.
	/// <br/> Item: unused
	/// <br/> Projectile: <see cref="Main.projectile"/>
	/// <br/> NPC: <see cref="Main.npc"/>
	/// <br/> Player: <see cref="Main.player"/>
	/// <para/> Note that Projectile.whoAmI is not consistent between clients in multiplayer for the same projectile.
	/// </summary>
	public int whoAmI;
	/// <summary>
	/// If true, the Entity actually exists within the game world. Within the specific entity array, if active is false, the entity is junk data. Always check active if iterating over the entity array. Another option for iterating is to use <see cref="Main.ActivePlayers"/>, <see cref="Main.ActiveNPCs"/>, <see cref="Main.ActiveProjectiles"/>, or <see cref="Main.ActiveItems"/> instead for simpler code.
	/// </summary>
	public bool active;
	/// <summary>
	/// Existing time of a object, default at 60s<br/>
	/// Note : the <see cref="AI"/> will still run at timeleft hitting 0 for 1 tick
	/// </summary>
	public int timeLeft = 3600;
	/// <summary>
	/// Type of the Mod Object
	/// </summary>
	public int Type = -1;
	public float rotation = 0;
	/// <summary>
	/// Net update request for this Mod Object, when this is set to true the Mod Object will be synced on the network.
	/// </summary>
	public bool netUpdate = false;
	public Mod Mod { get; internal set; }
	public string Name => GetType().Name;
	public string FullName => $"{Mod?.Name ?? "Terraria"}/{Name}";
	/// <summary>
	/// Use this to create new Mod Object in the world.<br/>
	/// The position that this mod object use to be created is World Coord, not tile coord
	/// </summary>
	/// <param name="spawnSource"></param>
	/// <param name="position"></param>
	/// <param name="velocity"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public static int NewModObject(IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type) {
		// Only allow spawning ModObjects on the server
		if (Main.netMode == NetmodeID.MultiplayerClient) {
			return -1;
		}

		int index = -1;
		
		// Search for an empty slot in the object array
		for (int i = 0; i < ObjectSystem.Objects.Length; i++) {
			// Get the object on the current slot
			var modObject = ObjectSystem.Objects[i];

			// Check if the object is already active
			if (modObject is { active: true }) {
				continue;
			}

			// Not active, save the free index
			index = i;
			break;
		}
		
		// No free slot was found, replace the oldest modObject
		if (index == -1) {
			index = FindOldestModObject();
		}
		
		ObjectSystem.Objects[index] = ObjectSystem.GetModObject(type);
		var newModObject = ObjectSystem.Objects[index];
		newModObject.SetDefaults();
		newModObject.active = true;
		newModObject.position = position;
		newModObject.velocity = velocity;
		newModObject.whoAmI = index;
		newModObject.OnSpawn(spawnSource);
		
		// When this code runs on the server, sync the ModObject on the network
		if (Main.dedServ) {
			newModObject.SendData();
		}

		// Pull down the netUpdate bool
		newModObject.netUpdate = false;
		
		// Return the index
		return index;
	}

	public static int FindOldestModObject() {
		int whoAmI = ObjectSystem.MaxObjects;
		int timeLeft = int.MaxValue;

		for (int i = 0; i < ObjectSystem.Objects.Length; i++) {
			// Get the object on the current slot
			var modObject = ObjectSystem.Objects[i];
			
			// Slot is free, instantly return
			if (modObject == null) {
				return i;
			}

			// Check if the time is lower
			if (modObject.timeLeft >= timeLeft) {
				continue;
			}

			// Assign the new time and whoAmI index
			whoAmI = modObject.whoAmI;
			timeLeft = modObject.timeLeft;
		}

		return whoAmI;
	}

	public static int GetModObjectType<T>() where T : ModObject {
		return ModContent.GetInstance<T>().Type;
	}
	void ILoadable.Load(Mod mod) {
		Mod = mod;
		Register();
	}
	protected void Register() {
		Type = ObjectSystem.Register(this);
	}
	public virtual void SetDefaults() {

	}

	public virtual void OnSpawn(IEntitySource source) {
		
	}

	/// <summary>
	/// Kill the object from active<br/>
	/// Active flag is actually a marker that is used to make sure that the object will be killed.
	/// </summary>
	public void Kill() {
		OnKill();
		active = false;
		timeLeft = 0;
		netUpdate = true;
	}
	/// <summary>
	/// The AI of the object, can be used to spawning NPC, projectile etc
	/// </summary>
	public virtual void AI() {

	}
	/// <summary>
	/// When the object is on its last tick or during the AI call <see cref="Kill"/>
	/// </summary>
	public virtual void OnKill() {

	}
	/// <summary>
	/// This is to draw the object in the game world if needed
	/// </summary>
	public virtual void Draw(SpriteBatch spritebatch) {

	}

	public virtual void Load() {
	}

	public void Unload() {
	}
	public ModObject Clone() {
		ModObject clone = (ModObject)MemberwiseClone();
		clone.whoAmI = -1;
		clone.timeLeft = 3600;
		clone.rotation = 0;
		clone.velocity = Vector2.Zero;
		clone.position = Vector2.Zero;
		return clone;
	}
	/// <summary>
	/// The center position of this entity in world coordinates. Calculated from <see cref="position"/>, <see cref="width"/>, and <see cref="height"/>.
	/// </summary>
	public Vector2 Center {
		get {
			return new Vector2(position.X + (float)(width / 2), position.Y + (float)(height / 2));
		}
		set {
			position = new Vector2(value.X - (float)(width / 2), value.Y - (float)(height / 2));
		}
	}

	public Vector2 Left {
		get {
			return new Vector2(position.X, position.Y + (float)(height / 2));
		}
		set {
			position = new Vector2(value.X, value.Y - (float)(height / 2));
		}
	}

	public Vector2 Right {
		get {
			return new Vector2(position.X + (float)width, position.Y + (float)(height / 2));
		}
		set {
			position = new Vector2(value.X - (float)width, value.Y - (float)(height / 2));
		}
	}

	public Vector2 Top {
		get {
			return new Vector2(position.X + (float)(width / 2), position.Y);
		}
		set {
			position = new Vector2(value.X - (float)(width / 2), value.Y);
		}
	}

	public Vector2 TopLeft {
		get {
			return position;
		}
		set {
			position = value;
		}
	}

	public Vector2 TopRight {
		get {
			return new Vector2(position.X + (float)width, position.Y);
		}
		set {
			position = new Vector2(value.X - (float)width, value.Y);
		}
	}

	public Vector2 Bottom {
		get {
			return new Vector2(position.X + (float)(width / 2), position.Y + (float)height);
		}
		set {
			position = new Vector2(value.X - (float)(width / 2), value.Y - (float)height);
		}
	}

	public Vector2 BottomLeft {
		get {
			return new Vector2(position.X, position.Y + (float)height);
		}
		set {
			position = new Vector2(value.X, value.Y - (float)height);
		}
	}

	public Vector2 BottomRight {
		get {
			return new Vector2(position.X + (float)width, position.Y + (float)height);
		}
		set {
			position = new Vector2(value.X - (float)width, value.Y - (float)height);
		}
	}

	public Vector2 Size {
		get {
			return new Vector2(width, height);
		}
		set {
			width = (int)value.X;
			height = (int)value.Y;
		}
	}

	public Rectangle Hitbox {
		get {
			return new Rectangle((int)position.X, (int)position.Y, width, height);
		}
		set {
			position = new Vector2(value.X, value.Y);
			width = value.Width;
			height = value.Height;
		}
	}

	public float AngleTo(Vector2 Destination) => (float)Math.Atan2(Destination.Y - Center.Y, Destination.X - Center.X);
	public float AngleFrom(Vector2 Source) => (float)Math.Atan2(Center.Y - Source.Y, Center.X - Source.X);
	public float Distance(Vector2 Other) => Vector2.Distance(Center, Other);
	public float DistanceSQ(Vector2 Other) => Vector2.DistanceSquared(Center, Other);
	public Vector2 DirectionTo(Vector2 Destination) => Vector2.Normalize(Destination - Center);
	public Vector2 DirectionFrom(Vector2 Source) => Vector2.Normalize(Center - Source);
	public bool WithinRange(Vector2 Target, float MaxRange) => Vector2.DistanceSquared(Center, Target) <= MaxRange * MaxRange;

	public void SendData() {
		try {
			// Get a new ModPacket for the data
			var packet = Mod.GetPacket();
			packet.Write((byte)Roguelike.MessageType.SyncModObject);
			packet.Write((ushort)whoAmI);
			packet.Write(active);
			packet.Write((short)Type);
			packet.WriteVector2(position);
			packet.WriteVector2(velocity);
			packet.Write(rotation);
			
			using var binarySteam = new MemoryStream();
			using var binaryWriter = new BinaryWriter(binarySteam);

			SendExtraData(binaryWriter);
			
			packet.Write(binarySteam.ToArray());
			packet.Send();
		}
		catch { /* Don't care */ }
	}

	public virtual void SendExtraData(BinaryWriter writer) {
		
	}

	public virtual void ReceiveExtraData(BinaryReader reader) {
		
	}
}
