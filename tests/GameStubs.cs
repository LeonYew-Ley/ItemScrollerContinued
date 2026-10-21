// Managed stand-ins only. No Unity native runtime, Photon networking or game assets.
using System;
using ItemScrollerContinued.Patches;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name) { } }
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class HarmonyPrefix : Attribute { }
}
namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static bool operator ==(Object a, Object b) =>
            (ReferenceEquals(a, null) || a.Destroyed) && (ReferenceEquals(b, null) || b.Destroyed)
            || ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => base.GetHashCode();
    }
    internal static class Mathf { public static float Abs(float value) => Math.Abs(value); }
    internal static class Time { public static float time = 10f; }
}
namespace Zorro.ControllerSupport
{
    public enum InputScheme { KeyboardMouse, Gamepad, Unknown }
    public static class InputHandler
    {
        public static InputScheme Current;
        public static int Queries;
        public static InputScheme GetCurrentUsedInputScheme() { Queries++; return Current; }
    }
}
namespace Zorro.Core
{
    public struct Optionable<T>
    {
        public bool IsSome;
        public T Value;
        public static Optionable<T> Some(T value) => new Optionable<T> { IsSome = true, Value = value };
    }
}
namespace ItemScrollerContinued
{
    internal sealed class Setting { public float Value; }
    internal sealed class Plugin
    {
        public static Plugin Instance = new Plugin();
        public Setting ScrollThreshold = new Setting { Value = 0.75f };
        public Setting MaxHoldTimeForScrollables = new Setting { Value = 0.75f };
    }
}
public class ItemComponent { }
public class RopeSpool : ItemComponent { }
public class Item : UnityEngine.Object
{
    public ItemComponent[] itemComponents;
    public Action<float> OnScrolled, OnScrolledMouseOnly;
    public float progress, lastFinishedCast;
}
public class ItemSlot
{
    public Item prefab;
    public bool IsEmpty() => prefab == null;
}
public class Player : UnityEngine.Object
{
    public ItemSlot[] itemSlots = new ItemSlot[3];
    public ItemSlot backpackSlot;
    public ItemSlot GetItemSlot(byte slot) => slot == 3 ? backpackSlot :
        (itemSlots != null && slot < itemSlots.Length ? itemSlots[slot] : null);
}
public class CharacterData
{
    public bool fullyConscious = true, isClimbing, isRopeClimbing;
    public Item currentItem;
    public UnityEngine.Object currentStickyItem;
}
public class CharacterInput
{
    public float scrollInput;
    public bool selectSlotForwardWasPressed, selectSlotBackwardWasPressed, unselectSlotWasPressed;
    public bool[] SelectedSlots = new bool[4];
    public bool SelectSlotWasPressed(byte slot) => SelectedSlots[slot];
}
public class Character : UnityEngine.Object
{
    public bool IsLocal = true, InputAllowed = true;
    public CharacterInput input = new CharacterInput();
    public CharacterData data = new CharacterData();
    public Player player = new Player();
    public bool CanDoInput() => InputAllowed;
}
public class CharacterItems
{
    public Character character = new Character();
    public float lastEquippedSlotTime = 9f, equippedSlotCooldown = 0.1f, lastSwitched;
    public int timesSwitchedRecently, VanillaSwitchingCalls, DroppingCalls, UsingCalls, EquipCalls;
    public Zorro.Core.Optionable<byte> currentSelectedSlot = Zorro.Core.Optionable<byte>.Some(0);
    public Zorro.Core.Optionable<byte> lastSelectedSlot = Zorro.Core.Optionable<byte>.Some(0);
    public byte? EquippedSlot;
    public void EquipSlot(Zorro.Core.Optionable<byte> slot) { EquipCalls++; EquippedSlot = slot.Value; }
    public void DoSwitching() { ScrollSwitching.BeforeSwitching(this); VanillaSwitchingCalls++; }
    // Mirrors the relevant call order verified in the installed game's Update.
    public void UpdateFrame() { DoSwitching(); DroppingCalls++; UsingCalls++; }
}
