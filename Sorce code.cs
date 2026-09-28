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
        private const string PluginVersion = "1.2.0";

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

        // Smart Properties
        public static int SmelterMaxOre => ModEnabled.Value ? _smelterMaxOre.Value : 10;
        public static int SmelterMaxCoal => ModEnabled.Value ? _smelterMaxCoal.Value : 20;
        public static int BlastFurnaceMaxOre => ModEnabled.Value ? _blastFurnaceMaxOre.Value : 10;
        public static int BlastFurnaceMaxCoal => ModEnabled.Value ? _blastFurnaceMaxCoal.Value : 20;
        public static int KilnMaxWood => ModEnabled.Value ? _kilnMaxWood.Value : 25;
        public static int WindmillMaxBarley => ModEnabled.Value ? _windmillMaxBarley.Value : 50;
        public static int SpinningWheelMaxFlax => ModEnabled.Value ? _spinningWheelMaxFlax.Value : 40;
        public static int RefineryMaxTissue => ModEnabled.Value ? _refineryMaxTissue.Value : 20;
        public static int RefineryMaxSap => ModEnabled.Value ? _refineryMaxSap.Value : 20;

        private void Awake()
        {
            // 0 - General Settings
            IsConfigLocked = Config.Bind("0 - General", "Lock Configuration", true,
                new ConfigDescription("If true, configuration settings will be locked to server-side values via ConditionalConfigSync for non-admin players.", null, new { order = 210 }));

            ModEnabled = BindConfig("0 - General", "Mod Enabled", true, "If false, all custom capacity modifications are ignored and game default values are used. [Synced with Server]", 200);

            // 1 - Smelter
            _smelterMaxOre = BindConfig("1 - Smelter", "Max Ore", 50, "Maximum amount of ore the standard Smelter can hold. [Synced with Server]", 100);
            _smelterMaxCoal = BindConfig("1 - Smelter", "Max Coal", 100, "Maximum amount of coal the standard Smelter can hold. [Synced with Server]", 99);

            // 2 - Blast Furnace
            _blastFurnaceMaxOre = BindConfig("2 - Blast Furnace", "Max Ore", 50, "Maximum amount of ore the Blast Furnace can hold. [Synced with Server]", 90);
            _blastFurnaceMaxCoal = BindConfig("2 - Blast Furnace", "Max Coal", 100, "Maximum amount of coal the Blast Furnace can hold. [Synced with Server]", 89);

            // 3 - Crafting Stations
            _kilnMaxWood = BindConfig("3 - Kiln", "Max Wood", 50, "Maximum amount of wood the Charcoal Kiln can hold. [Synced with Server]", 80);
            _windmillMaxBarley = BindConfig("4 - Windmill", "Max Barley", 100, "Maximum amount of barley the Windmill can hold. [Synced with Server]", 70);
            _spinningWheelMaxFlax = BindConfig("5 - Spinning Wheel", "Max Flax", 100, "Maximum amount of flax the Spinning Wheel can hold. [Synced with Server]", 60);
            _refineryMaxTissue = BindConfig("6 - Eitr Refinery", "Max Soft Tissue", 50, "Maximum amount of soft tissue the Eitr Refinery can hold. [Synced with Server]", 50);
            _refineryMaxSap = BindConfig("6 - Eitr Refinery", "Max Sap", 50, "Maximum amount of sap the Eitr Refinery can hold. [Synced with Server]", 49);

            harmony.PatchAll();
        }

        private ConfigEntry<T> BindConfig<T>(string group, string name, T value, string description, int order)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, new ConfigDescription(description, null, new { order }));
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
            string name = instance.m_name;

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
        }
    }
}

