using System;
using System.Linq;
using ItemScrollerContinued;
using ItemScrollerContinued.Patches;
using Zorro.ControllerSupport;
using Zorro.Core;

internal static class Program
{
    private static int checks;
    private static void Equal<T>(T expected, T actual, string description)
    {
        if (!Equals(expected, actual)) throw new Exception($"{description}: expected {expected}, got {actual}");
        checks++;
    }
    private static CharacterItems Create()
    {
        UnityEngine.Time.time = 10f;
        Plugin.Instance.MaxHoldTimeForScrollables.Value = 0.75f;
        InputHandler.Current = InputScheme.KeyboardMouse;
        var items = new CharacterItems();
        items.character.input.scrollInput = -1f;
        for (int slot = 0; slot < 3; slot++)
            items.character.player.itemSlots[slot] = new ItemSlot { prefab = new Item() };
        return items;
    }
    private static void Frame(CharacterItems items, byte? expected, string description)
    {
        items.UpdateFrame();
        Equal(expected, items.EquippedSlot, description);
        Equal(1, items.VanillaSwitchingCalls, description + ": vanilla switching continues");
        Equal(1, items.DroppingCalls, description + ": dropping continues");
        Equal(1, items.UsingCalls, description + ": using/clicking continues");
    }
    private static void Main()
    {
        foreach (InputScheme scheme in Enum.GetValues(typeof(InputScheme)))
        {
            InputHandler.Current = scheme;
            Action<float> handler = _ => { };
            Check(null, false, "null item");
            Check(new Item { Destroyed = true }, false, "destroyed item");
            Check(new Item(), false, "null components");
            Check(new Item { itemComponents = Array.Empty<ItemComponent>() }, false, "empty components");
            Check(new Item { itemComponents = new[] { new ItemComponent() } }, false, "ordinary components");
            Check(new Item { itemComponents = new ItemComponent[] { null } }, false, "null element");
            Check(new Item { itemComponents = new ItemComponent[] { null, new RopeSpool() } }, true, "rope spool");
            Check(new Item { OnScrolled = handler }, true, "general handler without components");
            Check(new Item { OnScrolledMouseOnly = handler }, scheme == InputScheme.KeyboardMouse, "mouse handler without components");
            Check(new Item { OnScrolledMouseOnly = handler, itemComponents = new[] { new ItemComponent() } },
                scheme == InputScheme.KeyboardMouse, "mouse handler with components");
            Check(new Item { OnScrolledMouseOnly = handler, itemComponents = new ItemComponent[] { new RopeSpool() } }, true, "mouse handler with rope");
            void Check(Item item, bool expected, string description) =>
                Equal(expected, ScrollSwitching.HasScrollFunction(item), $"{scheme}/{description}");
        }

        // Exhaustive occupied/empty combinations, both directions, normal and special slots.
        for (int mask = 0; mask < 16; mask++)
        foreach (byte current in new byte[] { 0, 1, 2, 3, 250, 255 })
        foreach (bool forward in new[] { false, true })
        {
            var player = new Player();
            for (int slot = 0; slot < 4; slot++)
            {
                ItemSlot itemSlot = new ItemSlot { prefab = (mask & (1 << slot)) != 0 ? new Item() : null };
                if (slot == 3) player.backpackSlot = itemSlot; else player.itemSlots[slot] = itemSlot;
            }
            var occupied = Enumerable.Range(0, 4).Where(slot => slot != current && (mask & (1 << slot)) != 0);
            int? expected = current >= 4 ? (forward ? occupied.Cast<int?>().FirstOrDefault() : occupied.Reverse().Cast<int?>().FirstOrDefault()) :
                occupied.OrderBy(slot => forward ? (slot - current + 4) % 4 : (current - slot + 4) % 4).Cast<int?>().FirstOrDefault();
            Equal(expected.HasValue ? (byte?)expected.Value : null,
                ScrollSwitching.FindNextSlot(forward, current, player), $"mask={mask}/current={current}/forward={forward}");
        }

        var idle = Create();
        idle.character.data.currentItem = new Item();
        idle.character.input.scrollInput = 0f;
        InputHandler.Queries = 0;
        Frame(idle, null, "issue #1: null-component passport on idle frame");
        Equal(0, InputHandler.Queries, "idle frame does not inspect held item");
        var scrollPassport = Create();
        scrollPassport.character.data.currentItem = new Item();
        Frame(scrollPassport, 1, "issue #1: null-component passport while scrolling");
        Frame(Create(), 1, "scroll forward");
        var reverse = Create(); reverse.character.input.scrollInput = 1f;
        Frame(reverse, 2, "scroll backward skips missing backpack");
        var noSelection = Create();
        noSelection.currentSelectedSlot = default; noSelection.lastSelectedSlot = default;
        Frame(noSelection, 0, "missing optionable selection starts at first occupied slot");

        Block("remote character", x => x.character.IsLocal = false);
        Block("missing character", x => x.character = null);
        Block("missing input", x => x.character.input = null);
        Block("missing data", x => x.character.data = null);
        Block("missing player", x => x.character.player = null);
        Block("missing slot array", x => x.character.player.itemSlots = null);
        Block("input disabled", x => x.character.InputAllowed = false);
        Block("unconscious", x => x.character.data.fullyConscious = false);
        Block("climbing", x => x.character.data.isClimbing = true);
        Block("rope climbing", x => x.character.data.isRopeClimbing = true);
        Block("sticky item", x => x.character.data.currentStickyItem = new UnityEngine.Object());
        Block("equip cooldown", x => x.lastEquippedSlotTime = 9.95f);
        Block("forward slot key", x => x.character.input.selectSlotForwardWasPressed = true);
        Block("backward slot key", x => x.character.input.selectSlotBackwardWasPressed = true);
        Block("unselect key", x => x.character.input.unselectSlotWasPressed = true);
        for (int slot = 0; slot < 4; slot++) { int selected = slot; Block("number key " + slot, x => x.character.input.SelectedSlots[selected] = true); }
        Block("active cast", x => x.character.data.currentItem = new Item { progress = 0.5f });
        Block("just-finished cast", x => x.character.data.currentItem = new Item { lastFinishedCast = 9.95f });
        Block("small wheel input", x => x.character.input.scrollInput = 0.5f);
        Block("NaN wheel input", x => x.character.input.scrollInput = float.NaN);
        Block("infinite wheel input", x => x.character.input.scrollInput = float.PositiveInfinity);
        Block("all slots empty", x => x.character.player.itemSlots = new ItemSlot[3]);
        Block("only current slot occupied", x => { x.character.player.itemSlots[1] = null; x.character.player.itemSlots[2] = null; });
        Block("rope after hold window", x => x.character.data.currentItem = new Item { itemComponents = new ItemComponent[] { new RopeSpool() } });
        Block("general scroll handler", x => x.character.data.currentItem = new Item { OnScrolled = _ => { } });
        Block("mouse scroll handler", x => x.character.data.currentItem = new Item { OnScrolledMouseOnly = _ => { } });

        var freshRope = Create();
        freshRope.lastEquippedSlotTime = 9.7f;
        freshRope.character.data.currentItem = new Item { itemComponents = new ItemComponent[] { new RopeSpool() } };
        Frame(freshRope, 1, "freshly equipped rope can be scrolled past");
        var gamepad = Create(); InputHandler.Current = InputScheme.Gamepad;
        gamepad.character.data.currentItem = new Item { OnScrolledMouseOnly = _ => { } };
        Frame(gamepad, 1, "mouse-only handler does not capture gamepad scroll");
        var zeroWindow = Create(); Plugin.Instance.MaxHoldTimeForScrollables.Value = 0f;
        zeroWindow.lastEquippedSlotTime = 9.7f;
        zeroWindow.character.data.currentItem = new Item { OnScrolled = _ => { } };
        Frame(zeroWindow, null, "zero hold window immediately preserves item scroll");
        var partial = Create(); partial.character.player.itemSlots = new ItemSlot[1];
        partial.character.player.backpackSlot = new ItemSlot { prefab = new Item() };
        Frame(partial, 3, "partial slots can still find separate backpack");

        Console.WriteLine($"PASS: {checks} managed regression checks, including upstream issue #1 call-order cases.");
        Console.WriteLine("Unity/Photon native execution and in-game passport behavior require separate manual testing.");
        void Block(string description, Action<CharacterItems> configure)
        {
            var items = Create(); configure(items); Frame(items, null, description);
        }
    }
}
