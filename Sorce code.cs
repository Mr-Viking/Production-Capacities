using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;

namespace ValheimProductionCapacities
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ProductionCapacitiesPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "com.Viking.valheim.productioncapacities";
        private const string PluginName = "ProductionCapacities";
        private const string PluginVersion = "1.3.0";

        private readonly Harmony harmony = new Harmony(PluginGUID);

        // Configuration Entries
        public static ConfigEntry<bool> IsConfigLocked;
        public static ConfigEntry<bool> ModEnabled;

        private static ConfigEntry<int> _smelterMaxOre;
        private static ConfigEntry<int> _smelterMaxCoal;
        private static ConfigEntry<int> _blastFurnaceMaxOre;
        private static ConfigEntry<int> _blastFurnaceMaxCoal;
        private static ConfigEntry<int> _kilnMaxWood;
        private static ConfigEntry<int> _windmillMaxBarley;
        private static ConfigEntry<int> _spinningWheelMaxFlax;
        private static ConfigEntry<int> _refineryMaxTissue;
        private static ConfigEntry<int> _refineryMaxSap;
        public static ConfigEntry<int> _frigidKilnMaxIce; // Changed to public for ease of reference
        private static ConfigEntry<int> _frostFoundryMaxLiquidFrost;

        // Smart Properties
        public static int SmelterMaxOre => _smelterMaxOre.Value;
        public static int SmelterMaxCoal => _smelterMaxCoal.Value;
        public static int BlastFurnaceMaxOre => _blastFurnaceMaxOre.Value;
        public static int BlastFurnaceMaxCoal => _blastFurnaceMaxCoal.Value;
        public static int KilnMaxWood => _kilnMaxWood.Value;
        public static int WindmillMaxBarley => _windmillMaxBarley.Value;
        public static int SpinningWheelMaxFlax => _spinningWheelMaxFlax.Value;
        public static int RefineryMaxTissue => _refineryMaxTissue.Value;
        public static int RefineryMaxSap => _refineryMaxSap.Value;
        public static int FrigidKilnMaxIce => _frigidKilnMaxIce.Value;
        public static int FrostFoundryMaxLiquidFrost => _frostFoundryMaxLiquidFrost.Value;

        private void Awake()
        {
            // 0 - General Settings
            IsConfigLocked = Config.Bind("0 - General", "Lock Configuration", true,
                new ConfigDescription("If true, configuration settings will be locked to server-side values via ConditionalConfigSync for non-admin players.", null, new { order = 210 }));

            ModEnabled = Config.Bind("0 - General", "Mod Enabled", true,
                new ConfigDescription("If false, all custom capacity modifications are ignored and game default values are used. [Synced with Server]", null, new { order = 200 }));
            ModEnabled.SettingChanged += OnSettingChanged;

            // 1 - Smelter
            _smelterMaxOre = BindConfig("1 - Smelter", "Max Ore", 50, "Maximum amount of ore the standard Smelter can hold. [Synced with Server]", 100, 1, 250);
            _smelterMaxCoal = BindConfig("1 - Smelter", "Max Coal", 100, "Maximum amount of coal the standard Smelter can hold. [Synced with Server]", 99, 1, 250);

            // 2 - Blast Furnace
            _blastFurnaceMaxOre = BindConfig("2 - Blast Furnace", "Max Ore", 50, "Maximum amount of ore the Blast Furnace can hold. [Synced with Server]", 90, 1, 250);
            _blastFurnaceMaxCoal = BindConfig("2 - Blast Furnace", "Max Coal", 100, "Maximum amount of coal the Blast Furnace can hold. [Synced with Server]", 89, 1, 250);

            // 3 - Crafting Stations
            _kilnMaxWood = BindConfig("3 - Kiln", "Max Wood", 50, "Maximum amount of wood the Charcoal Kiln can hold. [Synced with Server]", 80, 1, 250);
            _windmillMaxBarley = BindConfig("4 - Windmill", "Max Barley", 100, "Maximum amount of barley the Windmill can hold. [Synced with Server]", 70, 1, 250);
            _spinningWheelMaxFlax = BindConfig("5 - Spinning Wheel", "Max Flax", 100, "Maximum amount of flax the Spinning Wheel can hold. [Synced with Server]", 60, 1, 250);
            _refineryMaxTissue = BindConfig("6 - Eitr Refinery", "Max Soft Tissue", 50, "Maximum amount of soft tissue the Eitr Refinery can hold. [Synced with Server]", 50, 1, 250);
            _refineryMaxSap = BindConfig("6 - Eitr Refinery", "Max Sap", 50, "Maximum amount of sap the Eitr Refinery can hold. [Synced with Server]", 49, 1, 250);

            // 7 - Deep North Stations
            _frigidKilnMaxIce = BindConfig("7 - Frigid Kiln", "Max Ice", 50, "Maximum amount of ice the Frigid Kiln can hold. [Synced with Server]", 40, 1, 250);
            _frostFoundryMaxLiquidFrost = BindConfig("8 - Frost Foundry", "Max Liquid Frost", 50, "Maximum amount of liquid frost fuel the Frost Foundry can hold. [Synced with Server]", 29, 1, 250);

            harmony.PatchAll();
        }

        private ConfigEntry<T> BindConfig<T>(string group, string name, T value, string description, int order, T min, T max) where T : IComparable
        {
            ConfigDescription configDesc = new ConfigDescription(
                description,
                new AcceptableValueRange<T>(min, max),
                new { order }
            );

            ConfigEntry<T> configEntry = Config.Bind(group, name, value, configDesc);
            configEntry.SettingChanged += OnSettingChanged;
            return configEntry;
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            UpdateAllExistingStations();
        }

        public static void UpdateAllExistingStations()
        {
            foreach (var smelter in FindObjectsByType<Smelter>(FindObjectsSortMode.None))
            {
                SmelterPatch.ApplySmelterCapacities(smelter);
            }

            foreach (var cookingStation in FindObjectsByType<CookingStation>(FindObjectsSortMode.None))
            {
                FrostFoundryPatch.ApplyFoundryCapacities(cookingStation);
            }
        }
    }

    [HarmonyPatch(typeof(Smelter), "Awake")]
    public static class SmelterPatch
    {
        public static void Postfix(Smelter __instance)
        {
            ApplySmelterCapacities(__instance);
        }

        public static void ApplySmelterCapacities(Smelter instance)
        {
            if (!ProductionCapacitiesPlugin.ModEnabled.Value) return;

            string name = instance.m_name;
            string cleanPrefabName = instance.gameObject != null ? instance.gameObject.name.Replace("(Clone)", "").Trim() : "";

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
            // FIXED: Applies capacity modifications to BOTH m_maxOre and m_maxFuel fields to circumvent fuel-only architecture assignments
            else if (name == "$piece_frostkiln" ||
                     name == "$piece_frigidkiln" ||
                     string.Equals(cleanPrefabName, "piece_FrostKiln", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(cleanPrefabName, "piece_FrigidKiln", StringComparison.OrdinalIgnoreCase) ||
                     cleanPrefabName.ToLower().Contains("frostkiln") ||
                     cleanPrefabName.ToLower().Contains("frigidkiln"))
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.FrigidKilnMaxIce;
                //instance.m_maxFuel = ProductionCapacitiesPlugin.FrigidKilnMaxIce;
            }
        }
    }

    [HarmonyPatch(typeof(CookingStation), "Awake")]
    public static class FrostFoundryPatch
    {
        public static void Postfix(CookingStation __instance)
        {
            ApplyFoundryCapacities(__instance);
        }

        public static void ApplyFoundryCapacities(CookingStation instance)
        {
            if (!ProductionCapacitiesPlugin.ModEnabled.Value) return;

            string name = instance.m_name;
            string cleanPrefabName = instance.gameObject != null ? instance.gameObject.name.Replace("(Clone)", "").Trim() : "";

            if (name == "$piece_frostfoundry" ||
                string.Equals(cleanPrefabName, "piece_FrostFoundry", StringComparison.OrdinalIgnoreCase) ||
                cleanPrefabName.ToLower().Contains("frostfoundry"))
            {
                instance.m_maxFuel = ProductionCapacitiesPlugin.FrostFoundryMaxLiquidFrost;
            }
        }
    }
}
