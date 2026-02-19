using System;
using System.Collections.Generic;
using System.Linq;

namespace InnoWerks.Computers.Apple
{
    public enum ExpansionRomType
    {
        ExpRomNull = 0,
        ExpRomInternal,
        ExpRomPeripheral
    };


    public class MachineState
    {
        private readonly Random rng = new();

        private readonly Queue<byte> keyboardQueue = new();

        public Dictionary<SoftSwitch, bool> State { get; } = [];

        public MachineState()
        {
            foreach (SoftSwitch sw in Enum.GetValues<SoftSwitch>().OrderBy(v => v.ToString()))
            {
                State[sw] = false;
            }
        }

        // used to hold the current slot device, if present
        public int CurrentSlot
        {
            get;
            set;
        }

        public ExpansionRomType ExpansionRomType { get; set; }

        /// <summary>
        /// Used to hold the most recent keyboard entry
        /// </summary>
        public byte KeyLatch { get; set; }

        /// <summary>
        /// Used to indicate if there's a key available
        /// </summary>
        public bool KeyStrobe { get; set; }

        public void ResetKeyboard()
        {
            KeyStrobe = false;
            KeyLatch = 0x00;

            keyboardQueue.Clear();
        }

        public void TryLoadNextKey()
        {
            if (keyboardQueue.Count > 0)
            {
                KeyLatch = keyboardQueue.Dequeue();
                KeyStrobe = true;
            }
        }

        public byte ReadKeyboardData()
        {
            // Bit 7 is the Strobe (1 = new key, 0 = old key)
            byte strobeBit = (byte)(KeyStrobe ? 0x80 : 0x00);

            // Return the strobe bit merged with the lower 7
            // bits of the ASCII character
            return (byte)(strobeBit | (KeyLatch & 0x7F));
        }

        // Called by the Emulator CPU when accessing $C010
        public void ClearKeyboardStrobe()
        {
            KeyStrobe = false;

            // The instant the CPU clears the strobe, check if another
            // key is waiting in the queue
            TryLoadNextKey();
        }

        public void EnqueueKey(byte ascii)
        {
            if (keyboardQueue.Count > 0)
            {
                keyboardQueue.Enqueue(ascii);
            }
            else
            {
                KeyLatch = ascii;
                KeyStrobe = true;
            }
        }

        public byte PeekKeyboard()
        {
            return KeyStrobe ?
                KeyLatch |= 0x80 :
                KeyLatch;
        }

#pragma warning disable CA5394 // Do not use insecure randomness
        public byte FloatingValue => (byte)(rng.Next() & 0xFF);
#pragma warning restore CA5394 // Do not use insecure randomness

        public byte HandleReadStateToggle(Memory128k memoryBlocks, SoftSwitch softSwitch, bool toState, bool floating = false)
        {
            ArgumentNullException.ThrowIfNull(memoryBlocks);

#pragma warning disable CA5394 // Do not use insecure randomness
            byte returnValue = floating == true ? (byte)(rng.Next() & 0xFF) : (byte)0x00;
#pragma warning restore CA5394 // Do not use insecure randomness

            if (State[softSwitch] == toState)
            {
                return returnValue;
            }

            State[softSwitch] = toState;
            memoryBlocks.Remap();

            return returnValue;
        }

        public void HandleWriteStateToggle(Memory128k memoryBlocks, SoftSwitch softSwitch, bool toState)
        {
            ArgumentNullException.ThrowIfNull(memoryBlocks);

            if (State[softSwitch] == toState)
            {
                return;
            }

            State[softSwitch] = toState;
            memoryBlocks.Remap();
        }
    }
}
