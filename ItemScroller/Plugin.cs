// Derived from MasterAli2/ItemScroller; maintained changes dated 2026-10-21.
// Licensed under GPL-3.0; see LICENSE and CHANGELOG.md.
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ItemScrollerContinued.Patches;

namespace ItemScrollerContinued
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInIncompatibility("MasterAli2.ItemScroller")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string GUID = "LeonYew.ItemScrollerContinued";
        public const string NAME = "Item Scroller Continued";
        public const string VERSION = "1.0.0";

        internal static Plugin Instance { get; private set; } = null!;
        internal ConfigEntry<float> ScrollThreshold { get; private set; } = null!;
        internal ConfigEntry<float> MaxHoldTimeForScrollables { get; private set; } = null!;
        private Harmony? harmony;

        private void Awake()
        {
            Instance = this;
            ScrollThreshold = Config.Bind("Scrolling", "ScrollThreshold", 0.75f,
                new ConfigDescription("Minimum scroll input required to switch an item.",
                    new AcceptableValueRange<float>(0.01f, 10f)));
            MaxHoldTimeForScrollables = Config.Bind("Scrolling", "MaxHoldTime", 0.75f,
                new ConfigDescription("Seconds after equipping a scrollable item before the wheel controls that item instead of switching slots.",
                    new AcceptableValueRange<float>(0f, 5f)));

            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(ScrollSwitching));
            Logger.LogInfo($"{NAME} v{VERSION} loaded");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}
