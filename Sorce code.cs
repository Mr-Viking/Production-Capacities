using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using ServerSync;

namespace ValheimProductionCapacities
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ProductionCapacitiesPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "com.Viking.valheim.productioncapacities";
        private const string PluginName = "ProductionCapacities";
        private const string PluginVersion = "1.6.0";

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private static ConfigSync configSync;

        // Configuration Entries
        public static SyncedConfigEntry<bool> IsConfigLocked;
        public static SyncedConfigEntry<bool> ModEnabled;

        public static SyncedConfigEntry<int> _smelterMaxOre;
        public static SyncedConfigEntry<int> _smelterMaxCoal;
        public static SyncedConfigEntry<int> _blastFurnaceMaxOre;
        public static SyncedConfigEntry<int> _blastFurnaceMaxCoal;
        public static SyncedConfigEntry<int> _kilnMaxWood;
        public static SyncedConfigEntry<int> _windmillMaxBarley;
        public static SyncedConfigEntry<int> _spinningWheelMaxFlax;
        public static SyncedConfigEntry<int> _refineryMaxTissue;
        public static SyncedConfigEntry<int> _refineryMaxSap;
        public static SyncedConfigEntry<int> _frigidKilnMaxIce;
        public static SyncedConfigEntry<int> _frostFoundryMaxLiquidFrost;

        // Smart Properties
        public static bool IsModEnabled => ModEnabled != null && ModEnabled.Value;
        public static int SmelterMaxOre => _smelterMaxOre != null ? _smelterMaxOre.Value : 50;
        public static int SmelterMaxCoal => _smelterMaxCoal != null ? _smelterMaxCoal.Value : 100;
        public static int BlastFurnaceMaxOre => _blastFurnaceMaxOre != null ? _blastFurnaceMaxOre.Value : 50;
        public static int BlastFurnaceMaxCoal => _blastFurnaceMaxCoal != null ? _blastFurnaceMaxCoal.Value : 100;
        public static int KilnMaxWood => _kilnMaxWood != null ? _kilnMaxWood.Value : 50;
        public static int WindmillMaxBarley => _windmillMaxBarley != null ? _windmillMaxBarley.Value : 100;
        public static int SpinningWheelMaxFlax => _spinningWheelMaxFlax != null ? _spinningWheelMaxFlax.Value : 100;
        public static int RefineryMaxTissue => _refineryMaxTissue != null ? _refineryMaxTissue.Value : 50;
        public static int RefineryMaxSap => _refineryMaxSap != null ? _refineryMaxSap.Value : 50;
        public static int FrigidKilnMaxIce => _frigidKilnMaxIce != null ? _frigidKilnMaxIce.Value : 50;
        public static int FrostFoundryMaxLiquidFrost => _frostFoundryMaxLiquidFrost != null ? _frostFoundryMaxLiquidFrost.Value : 50;

        private void Awake()
        {
            configSync = new ConfigSync(PluginGUID)
            {
                DisplayName = PluginName,
                CurrentVersion = PluginVersion,
                MinimumRequiredVersion = PluginVersion
            };

            IsConfigLocked = configSync.AddLockingConfigEntry(Config.Bind("0 - General", "Lock Configuration", true,
                new ConfigDescription("If true, configuration settings will be locked to server-side values for non-admin players.", null, new { order = 210 })));

            ConfigEntry<bool> modEnabledRaw = Config.Bind("0 - General", "Mod Enabled", true,
                new ConfigDescription("If false, all custom capacity modifications are ignored and game default values are used. [Synced with Server]", null, new { order = 200 }));
            ModEnabled = configSync.AddConfigEntry(modEnabledRaw);
            ModEnabled.SynchronizedConfig = true;
            modEnabledRaw.SettingChanged += (s, e) => TriggerSettingChanged();

            _smelterMaxOre = BindConfig("1 - Smelter", "Max Ore", 50, "Maximum amount of ore the standard Smelter can hold. [Synced with Server]", 100, 1, 250);
            _smelterMaxCoal = BindConfig("1 - Smelter", "Max Coal", 100, "Maximum amount of coal the standard Smelter can hold. [Synced with Server]", 99, 1, 250);
            _blastFurnaceMaxOre = BindConfig("2 - Blast Furnace", "Max Ore", 50, "Maximum amount of ore the Blast Furnace can hold. [Synced with Server]", 90, 1, 250);
            _blastFurnaceMaxCoal = BindConfig("2 - Blast Furnace", "Max Coal", 100, "Maximum amount of coal the Blast Furnace can hold. [Synced with Server]", 89, 1, 250);
            _kilnMaxWood = BindConfig("3 - Kiln", "Max Wood", 50, "Maximum amount of wood the Charcoal Kiln can hold. [Synced with Server]", 80, 1, 250);
            _windmillMaxBarley = BindConfig("4 - Windmill", "Max Barley", 100, "Maximum amount of barley the Windmill can hold. [Synced with Server]", 70, 1, 250);
            _spinningWheelMaxFlax = BindConfig("5 - Spinning Wheel", "Max Flax", 100, "Maximum amount of flax the Spinning Wheel can hold. [Synced with Server]", 60, 1, 250);
            _refineryMaxTissue = BindConfig("6 - Eitr Refinery", "Max Soft Tissue", 50, "Maximum amount of soft tissue the Eitr Refinery can hold. [Synced with Server]", 50, 1, 250);
            _refineryMaxSap = BindConfig("6 - Eitr Refinery", "Max Sap", 50, "Maximum amount of sap the Eitr Refinery can hold. [Synced with Server]", 49, 1, 250);
            _frigidKilnMaxIce = BindConfig("7 - Frigid Kiln", "Max Ice", 50, "Maximum amount of ice the Frigid Kiln can hold. [Synced with Server]", 40, 1, 250);
            _frostFoundryMaxLiquidFrost = BindConfig("8 - Frost Foundry", "Max Liquid Frost", 50, "Maximum amount of liquid frost fuel the Frost Foundry can hold. [Synced with Server]", 29, 1, 250);

            harmony.PatchAll();
        }

        public void TriggerSettingChanged()
        {
            UpdateAllExistingStations();
        }

        public static void UpdateAllExistingStations()
        {
            UpdateMasterDatabasePrefabs();

            foreach (var smelter in FindObjectsByType<Smelter>(FindObjectsSortMode.None))
            {
                SmelterPatch.ApplySmelterCapacities(smelter);
            }

            foreach (var cookingStation in FindObjectsByType<CookingStation>(FindObjectsSortMode.None))
            {
                FrostFoundryPatch.ApplyFoundryCapacities(cookingStation);
            }
        }

        public static void UpdateMasterDatabasePrefabs()
        {
            if (ZNetScene.instance == null) return;

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null) continue;

                if (prefab.TryGetComponent<Smelter>(out Smelter smelter))
                {
                    SmelterPatch.ApplySmelterCapacities(smelter);
                }
                else if (prefab.TryGetComponent<CookingStation>(out CookingStation cookingStation))
                {
                    FrostFoundryPatch.ApplyFoundryCapacities(cookingStation);
                }
            }
        }

        private SyncedConfigEntry<T> BindConfig<T>(string group, string name, T value, string description, int order, T min, T max) where T : IComparable
        {
            ConfigDescription configDesc = new ConfigDescription(
                description,
                new AcceptableValueRange<T>(min, max),
                new { order }
            );

            ConfigEntry<T> configEntry = Config.Bind(group, name, value, configDesc);
            SyncedConfigEntry<T> syncedEntry = configSync.AddConfigEntry(configEntry);
            syncedEntry.SynchronizedConfig = true;

            configEntry.SettingChanged += (s, e) => TriggerSettingChanged();
            return syncedEntry;
        }
    }

    [HarmonyPatch(typeof(Smelter))]
    public static class SmelterPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Postfix(Smelter __instance)
        {
            ApplySmelterCapacities(__instance);
        }

        // FIXED: Re-added continuous counter hooks to ensure slider value adjustments register instantly live in-game
        [HarmonyPatch("RPC_AddOre")]
        [HarmonyPatch("RPC_AddFuel")]
        [HarmonyPatch("GetQueueSize")]
        [HarmonyPatch("GetFuel")]
        [HarmonyPrefix]
        public static void UpdatePrefix(Smelter __instance)
        {
            ApplySmelterCapacities(__instance);
        }

        public static void ApplySmelterCapacities(Smelter instance)
        {
            if (instance == null) return;
            string name = instance.m_name;
            string cleanPrefabName = instance.gameObject != null ? instance.gameObject.name.Replace("(Clone)", "").Trim() : "";

            if (!ProductionCapacitiesPlugin.IsModEnabled)
            {
                if (name == "$piece_smelter") { instance.m_maxOre = 10; instance.m_maxFuel = 20; }
                else if (name == "$piece_blastfurnace") { instance.m_maxOre = 10; instance.m_maxFuel = 20; }
                else if (name == "$piece_charcoalkiln") { instance.m_maxOre = 25; }
                else if (name == "$piece_windmill") { instance.m_maxOre = 50; }
                else if (name == "$piece_spinningwheel") { instance.m_maxOre = 40; }
                else if (name == "$piece_eitrrefinery") { instance.m_maxOre = 20; instance.m_maxFuel = 20; }
                else if (name == "$piece_frostkiln" || name == "$piece_frigidkiln" || cleanPrefabName.ToLower().Contains("frostkiln") || cleanPrefabName.ToLower().Contains("frigidkiln"))
                {
                    instance.m_maxFuel = 25; // Safely rolls back only the fuel layer when mod is turned off
                }
                return;
            }

            if (name == "$piece_smelter")
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.SmelterMaxOre;
                instance.m_maxFuel = ProductionCapacitiesPlugin.SmelterMaxCoal;
            }
            else if (name == "$piece_blastfurnace")
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.BlastFurnaceMaxOre;
                instance.m_maxFuel = ProductionCapacitiesPlugin.BlastFurnaceMaxCoal;
            }
            else if (name == "$piece_charcoalkiln")
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.KilnMaxWood;
            }
            else if (name == "$piece_windmill")
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.WindmillMaxBarley;
            }
            else if (name == "$piece_spinningwheel")
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.SpinningWheelMaxFlax;
            }
            else if (name == "$piece_eitrrefinery")
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.RefineryMaxTissue;
                instance.m_maxFuel = ProductionCapacitiesPlugin.RefineryMaxSap;
            }
            else if (name == "$piece_frostkiln" ||
            name == "$piece_frigidkiln" ||
            string.Equals(cleanPrefabName, "piece_FrostKiln", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(cleanPrefabName, "piece_FrigidKiln", StringComparison.OrdinalIgnoreCase) ||
            cleanPrefabName.ToLower().Contains("frostkiln") ||
            cleanPrefabName.ToLower().Contains("frigidkiln"))
            {
                // FIXED: Removed m_maxOre assignment entirely. Now targets ONLY m_maxFuel to preserve processing integrity.
                instance.m_maxFuel = ProductionCapacitiesPlugin.FrigidKilnMaxIce;
            }
        }
    }
    [HarmonyPatch(typeof(CookingStation))]
    public static class FrostFoundryPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Postfix(CookingStation __instance)
        {
            ApplyFoundryCapacities(__instance);
        }
        // FIXED: Re-added continuous counter hooks to ensure slider value adjustments register instantly live in-game
        [HarmonyPatch("RPC_AddFuel")]
        [HarmonyPatch("GetFuel")]
        [HarmonyPatch("GetFreeSlot")]
        [HarmonyPrefix]
        public static void UpdatePrefix(CookingStation __instance)
        {
            ApplyFoundryCapacities(__instance);
        }
        public static void ApplyFoundryCapacities(CookingStation instance)
        {
            if (instance == null) return;
            string name = instance.m_name;
            string cleanPrefabName = instance.gameObject != null ? instance.gameObject.name.Replace("(Clone)", "").Trim() : "";
            if (!ProductionCapacitiesPlugin.IsModEnabled)
            {
                if (name == "$piece_frostfoundry" || cleanPrefabName.ToLower().Contains("frostfoundry"))
                {
                    instance.m_maxFuel = 10;
                }
                return;
            }
            if (name == "$piece_frostfoundry" ||
            string.Equals(cleanPrefabName, "piece_FrostFoundry", StringComparison.OrdinalIgnoreCase) ||
            cleanPrefabName.ToLower().Contains("frostfoundry"))
            {
                instance.m_maxFuel = ProductionCapacitiesPlugin.FrostFoundryMaxLiquidFrost;
            }
        }
    }
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDBPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            ProductionCapacitiesPlugin.UpdateMasterDatabasePrefabs();
        }
    }
}
