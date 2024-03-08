using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Receiver2;

namespace Iskra2Patch
{
	[BepInDependency("pl.szikaka.receiver_2_modding_kit", "1.4.0")]
	[BepInPlugin("pl.szikaka.iskra2", "Iskra 2 Patch", "2.0.0")]
	public class Plugin : BaseUnityPlugin {
		internal const int gun_model = 1001;

		internal static Plugin plugin_instance;

		public static ConfigEntry<string> sight_type;
		public static ConfigEntry<bool> use_custom_sounds;
		public static ConfigEntry<bool> indicator_active;
		public static ConfigEntry<Color> indicator_background_color;
		public static ConfigEntry<Color> indicator_color;

		private void Awake() {
			Logger.LogInfo("Loaded Iskra 2 Plugin!");

			indicator_active = Config.Bind("Scope settings", "Zoom indicator", true, "Show a zoom indicator on the top of scope's viewport");

			indicator_background_color = Config.Bind("Scope settings", "Zoom indicator background color", new Color(0.102f, 0.102f, 0.102f), "Color of zoom indicator background");

			indicator_color = Config.Bind("Scope settings", "Zoom indicator needle color", new Color(0.439f, 0.439f, 0.439f), "Color of zoom indicator needle");

			sight_type = Config.Bind(
				new ConfigDefinition("Gun settings", "Sight type"),
				Iskra2WeaponProperties.notchSight,
				new ConfigDescription("What type of sight do you want to use", new AcceptableValueList<string>(
					new string[]
					{
						Iskra2WeaponProperties.notchSight,
						Iskra2WeaponProperties.apertureSight,
						Iskra2WeaponProperties.scope
					}
				))
			);

			plugin_instance = this;

			Harmony.CreateAndPatchAll(this.GetType());
		}

		[HarmonyPatch(typeof(LocalAimHandler), "GetCurrentLoadout")]
		[HarmonyPostfix]
		private static void PatchLAHGetLoadout(ref PlayerLoadout __result)
		{
			if (__result.gun_internal_name == "szikaka.iskra")
			{
				PlayerLoadoutEquipment equipment = new PlayerLoadoutEquipment();

				equipment.chance_of_presence = 1;
				equipment.randomize_slot = false;
				equipment.randomize_loaded_ammo_count = false;

				LocalAimHandler.player_instance.TryGetGun(out GunScript gunScript);

				SimpleMagazineScript mag = gunScript.GetComponentInChildren<SimpleMagazineScript>();

				equipment.internal_name = "Iskra_2_Magazine";
				equipment.persistent_data = mag.GetPersistentData();
				equipment.equipment_type = (EquipmentType)4;

				__result.equipment.Add(equipment);
			}
		}
	}
}
