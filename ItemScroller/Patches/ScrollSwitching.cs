// Derived from MasterAli2/ItemScroller; maintained changes dated 2026-10-21.
// Licensed under GPL-3.0; see LICENSE and CHANGELOG.md.
using HarmonyLib;
using UnityEngine;
using Zorro.ControllerSupport;
using Zorro.Core;

namespace ItemScrollerContinued.Patches
{
    internal static class ScrollSwitching
    {
        [HarmonyPatch(typeof(CharacterItems), nameof(CharacterItems.DoSwitching))]
        [HarmonyPrefix]
        internal static void BeforeSwitching(CharacterItems __instance)
        {
            Character character = __instance.character;
            if (character == null || !character.IsLocal || character.input == null)
                return;

            // Do not inspect held items on idle frames. Exceptions in this prefix
            // would also prevent the game's subsequent dropping/using updates.
            float scroll = character.input.scrollInput;
            if (float.IsNaN(scroll) || float.IsInfinity(scroll) ||
                Mathf.Abs(scroll) < Plugin.Instance.ScrollThreshold.Value)
                return;

            if (character.data == null || character.player == null || character.player.itemSlots == null ||
                !character.CanDoInput() || !character.data.fullyConscious ||
                character.data.isClimbing || character.data.isRopeClimbing ||
                character.data.currentStickyItem != null ||
                Time.time <= __instance.lastEquippedSlotTime + __instance.equippedSlotCooldown)
                return;

            // Let explicit slot keys keep their normal priority in DoSwitching.
            if (character.input.selectSlotForwardWasPressed || character.input.selectSlotBackwardWasPressed ||
                character.input.unselectSlotWasPressed)
                return;
            for (byte slot = 0; slot < 4; slot++)
            {
                if (character.input.SelectSlotWasPressed(slot))
                    return;
            }

            Item item = character.data.currentItem;
            if (item != null && (item.progress > 0f || item.lastFinishedCast + 0.1f > Time.time))
                return;

            // Use the actual equip time, including number-key selection, and
            // keep the timer local to this CharacterItems instance.
            if (HasScrollFunction(item) &&
                Time.time >= __instance.lastEquippedSlotTime + Plugin.Instance.MaxHoldTimeForScrollables.Value)
                return;

            byte current = __instance.currentSelectedSlot.IsSome ? __instance.currentSelectedSlot.Value :
                (__instance.lastSelectedSlot.IsSome ? __instance.lastSelectedSlot.Value : byte.MaxValue);
            byte? next = FindNextSlot(scroll < 0f, current, character.player);
            if (!next.HasValue)
                return;

            __instance.lastSwitched = Time.time;
            __instance.timesSwitchedRecently++;
            __instance.EquipSlot(Optionable<byte>.Some(next.Value));
        }

        internal static bool HasScrollFunction(Item? item)
        {
            if (item == null)
                return false;

            bool isMouseInput = InputHandler.GetCurrentUsedInputScheme() == InputScheme.KeyboardMouse;
            if (item.OnScrolled != null || (isMouseInput && item.OnScrolledMouseOnly != null))
                return true;

            // Some held items have not initialized their component array yet.
            // Handlers above still take precedence when that array is absent.
            if (item.itemComponents == null)
                return false;

            foreach (ItemComponent component in item.itemComponents)
            {
                if (component is RopeSpool)
                    return true;
            }

            return false;
        }

        internal static byte? FindNextSlot(bool forward, byte current, Player player)
        {
            const int slotCount = 4; // Three hotbar slots plus the separate backpack slot.
            int start = current < slotCount ? current : (forward ? slotCount - 1 : 0);
            for (int offset = 1; offset <= slotCount; offset++)
            {
                byte candidate = (byte)((start + (forward ? offset : -offset) + slotCount) % slotCount);
                if (candidate == current || (candidate < 3 &&
                    (player.itemSlots == null || candidate >= player.itemSlots.Length)))
                    continue;

                ItemSlot? itemSlot = player.GetItemSlot(candidate);
                if (itemSlot != null && !itemSlot.IsEmpty())
                    return candidate;
            }

            return null;
        }
    }
}
