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
        private const string PluginVersion = "1.1.0";

        private readonly Harmony harmony = new Harmony(PluginGUID);

        // Configuration Entries
        public static ConfigEntry<int> SmelterMaxOre;
        public static ConfigEntry<int> SmelterMaxCoal;
        public static ConfigEntry<int> BlastFurnaceMaxOre;
        public static ConfigEntry<int> BlastFurnaceMaxCoal;
        public static ConfigEntry<int> KilnMaxWood;
        public static ConfigEntry<int> WindmillMaxBarley;
        public static ConfigEntry<int> SpinningWheelMaxFlax;
        public static ConfigEntry<int> OvenMaxWood;
        public static ConfigEntry<int> RefineryMaxTissue;
        public static ConfigEntry<int> RefineryMaxSap;

        private void Awake()
        {
            // Bind configs and immediately chain the OnSettingChanged method to listen for live edits
            SmelterMaxOre = Config.Bind("1 - Smelter", "Max Ore", 50, "Maximum amount of ore the standard Smelter can hold.");
            SmelterMaxOre.SettingChanged += OnSettingChanged;

            SmelterMaxCoal = Config.Bind("1 - Smelter", "Max Coal", 100, "Maximum amount of coal the standard Smelter can hold.");
            SmelterMaxCoal.SettingChanged += OnSettingChanged;

            BlastFurnaceMaxOre = Config.Bind("2 - Blast Furnace", "Max Ore", 50, "Maximum amount of ore the Blast Furnace can hold.");
            BlastFurnaceMaxOre.SettingChanged += OnSettingChanged;

            BlastFurnaceMaxCoal = Config.Bind("2 - Blast Furnace", "Max Coal", 100, "Maximum amount of coal the Blast Furnace can hold.");
            BlastFurnaceMaxCoal.SettingChanged += OnSettingChanged;

            KilnMaxWood = Config.Bind("3 - Charcoal Kiln", "Max Wood", 100, "Maximum amount of wood the Charcoal Kiln can hold.");
            KilnMaxWood.SettingChanged += OnSettingChanged;

            WindmillMaxBarley = Config.Bind("4 - Windmill", "Max Barley", 100, "Maximum amount of barley the Windmill can hold.");
            WindmillMaxBarley.SettingChanged += OnSettingChanged;

            SpinningWheelMaxFlax = Config.Bind("5 - Spinning Wheel", "Max Flax", 100, "Maximum amount of flax the Spinning Wheel can hold.");
            SpinningWheelMaxFlax.SettingChanged += OnSettingChanged;

            OvenMaxWood = Config.Bind("6 - Stone Oven", "Max Wood", 50, "Maximum amount of wood the Stone Oven can hold.");
            OvenMaxWood.SettingChanged += OnSettingChanged;

            RefineryMaxTissue = Config.Bind("7 - Eitr Refinery", "Max Soft Tissue", 50, "Maximum amount of Soft Tissue the Eitr Refinery can hold.");
            RefineryMaxTissue.SettingChanged += OnSettingChanged;

            RefineryMaxSap = Config.Bind("7 - Eitr Refinery", "Max Sap", 50, "Maximum amount of Sap the Eitr Refinery can hold.");
            RefineryMaxSap.SettingChanged += OnSettingChanged;

            // Apply Harmony patches
            harmony.PatchAll();
            Logger.LogInfo($"{PluginName} loaded successfully with Live Config Updates enabled!");
        }

        // Triggered instantly when you change a setting in the F1 Configuration Manager
        private void OnSettingChanged(object sender, EventArgs e)
        {
            // Ensure a local player exists and a world is loaded before attempting to cycle objects
            if (Player.m_localPlayer == null) return;

            // Fixed Unity 2022 namespace ambiguity by explicitly calling UnityEngine.Object
            Smelter[] activeSmelters = UnityEngine.Object.FindObjectsOfType<Smelter>();
            foreach (Smelter instance in activeSmelters)
            {
                if (instance != null)
                {
                    // Re-run the update logic over existing instances instantly
                    Smelter_Awake_Patch.UpdateSmelterCapacities(instance);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Smelter), "Awake")]
    public static class Smelter_Awake_Patch
    {
        // Intercepts structural instantiation (placing a building or loading an area)
        public static void Postfix(Smelter __instance)
        {
            UpdateSmelterCapacities(__instance);
        }

        // Shared logic used both during initial load AND during live F1 config tweaks
        public static void UpdateSmelterCapacities(Smelter instance)
        {
            if (instance == null || instance.gameObject == null) return;

            string name = instance.gameObject.name.ToLower();

            if (name.Contains("smelter") && !name.Contains("blastfurnace"))
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.SmelterMaxOre.Value;
                instance.m_maxFuel = ProductionCapacitiesPlugin.SmelterMaxCoal.Value;
            }
            else if (name.Contains("blastfurnace"))
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.BlastFurnaceMaxOre.Value;
                instance.m_maxFuel = ProductionCapacitiesPlugin.BlastFurnaceMaxCoal.Value;
            }
            else if (name.Contains("charcoal_kiln"))
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.KilnMaxWood.Value;
            }
            else if (name.Contains("windmill"))
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.WindmillMaxBarley.Value;
            }
            else if (name.Contains("spinningwheel"))
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.SpinningWheelMaxFlax.Value;
            }
            else if (name.Contains("stone_oven") || name.Contains("piece_oven")) // Matches the structural naming rules
            {
                instance.m_maxFuel = ProductionCapacitiesPlugin.OvenMaxWood.Value;
            }
            else if (name.Contains("eitrrefinery"))
            {
                instance.m_maxOre = ProductionCapacitiesPlugin.RefineryMaxTissue.Value;
                instance.m_maxFuel = ProductionCapacitiesPlugin.RefineryMaxSap.Value;
            }
        }
    }
}
