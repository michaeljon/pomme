using System;

namespace InnoWerks.Simulators
{
    public static class InstructionDecoders
    {
        public static DecodedOperation DecodeUndefined(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);

            return new DecodedOperation
            {
                OpCodeValue = oc,
                Display = "<illegal>",
            };
        }

        /// <summary>
        /// Implied - In the implied addressing mode, the address containing
        /// the operand is implicitly stated in the operation code of the instruction.
        /// </summary>
        public static DecodedOperation DecodeImplicit(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);

            return new DecodedOperation
            {
                Length = 1,
                OpCodeValue = oc,
                Display = "",
            };
        }

        /// <summary>
        /// Implied - In the implied addressing mode, the address containing
        /// the operand is implicitly stated in the operation code of the instruction.
        /// </summary>
        public static DecodedOperation DecodeStack(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);

            return new DecodedOperation
            {
                Length = 1,
                OpCodeValue = oc,
                Display = "",
            };
        }

        /// <summary>
        /// Accum - This form of addressing is represented with a
        /// one byte instruction, implying an operation on the accumulator.
        /// </summary>
        public static DecodedOperation DecodeAccumulator(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);

            return new DecodedOperation
            {
                Length = 1,
                OpCodeValue = oc,
                Display = "",
            };
        }

        /// <summary>
        /// IMM - In immediate addressing, the second byte of the instruction
        /// contains the operand, with no further memory addressing required.
        /// </summary>
        public static DecodedOperation DecodeImmediate(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"#${o1:X2}",
            };
        }

        /// <summary>
        /// ABS - In absolute addressing, the second byte of the instruction
        /// specifies the eight low order bits of the effective address while
        /// the third byte specifies the eight high order bits. Thus the
        /// absolute addressing mode allows access to the entire 64k bytes
        /// of addressable memory.
        /// </summary>
        public static DecodedOperation DecodeAbsolute(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));
            var o2 = bus.Peek((ushort)(programCounter + 2));

            return new DecodedOperation
            {
                Length = 3,
                OpCodeValue = oc,
                Operand1 = o1,
                Operand2 = o2,
                Display = $"${o2:X2}{o1:X2}",
            };
        }

        /// <summary>
        /// ZP - The zero page instructions allow for shorter code and execution
        /// fetch times by fetching only the second byte of the instruction and
        /// assuming a zero high address byte. Careful of use the zero page can
        /// result in significant increase in code efficiency.
        /// </summary>
        public static DecodedOperation DecodeZeroPage(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"${o1:X2}",
            };
        }

        /// <summary>
        /// ABS,X (X indexing) - This form of addressing is used in conjunction
        /// with X and Y index register and is referred to as "Absolute,X".
        /// The effective address is formed by adding the contents
        /// of X to the address contained in the second and third bytes of the
        /// instruction. This mode allows for the index register to contain the
        /// index or count value and the instruction to contain the base address.
        /// This type of indexing allows any location referencing and the index
        /// to modify fields, resulting in reducing coding and execution time.
        /// </summary>
        public static DecodedOperation DecodeAbsoluteXIndexed(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));
            var o2 = bus.Peek((ushort)(programCounter + 2));

            return new DecodedOperation
            {
                Length = 3,
                OpCodeValue = oc,
                Operand1 = o1,
                Operand2 = o2,
                Display = $"${o2:X2}{o1:X2},X",
            };
        }

        /// <summary>
        /// ABS,Y (Y indexing) - This form of addressing is used in conjunction
        /// with X and Y index register and is referred to as
        /// "Absolute,Y". The effective address is formed by adding the contents
        /// of Y to the address contained in the second and third bytes of the
        /// instruction. This mode allows for the index register to contain the
        /// index or count value and the instruction to contain the base address.
        /// This type of indexing allows any location referencing and the index
        /// to modify fields, resulting in reducing coding and execution time.
        /// </summary>
        public static DecodedOperation DecodeAbsoluteYIndexed(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));
            var o2 = bus.Peek((ushort)(programCounter + 2));

            return new DecodedOperation
            {
                Length = 3,
                OpCodeValue = oc,
                Operand1 = o1,
                Operand2 = o2,
                Display = $"${o2:X2}{o1:X2},Y",
            };
        }

        /// <summary>
        /// ZP,X (X indexing) - This form of address is used with the index
        /// register and is referred to as "Zero Page,X".
        /// The effective address is calculated by adding the second byte to the
        /// contents of the index register. Since this is a form of "Zero Page"
        /// addressing, the content of the second byte references a location
        /// in page zero. Additionally, due to the "Zero Page" addressing nature
        /// of this mode, no carry is added to the high order eight bits of
        /// memory and crossing page boundaries does not occur.
        /// </summary>
        public static DecodedOperation DecodeZeroPageXIndexed(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"(${o1:X2},X)",
            };
        }

        /// <summary>
        /// ZP,Y (Y indexing) - This form of address is used with the index
        /// register and is referred to as "Zero Page,Y".
        /// The effective address is calculated by adding the second byte to the
        /// contents of the index register. Since this is a form of "Zero Page"
        /// addressing, the content of the second byte references a location
        /// in page zero. Additionally, due to the "Zero Page" addressing nature
        /// of this mode, no carry is added to the high order eight bits of
        /// memory and crossing page boundaries does not occur.
        /// </summary>
        public static DecodedOperation DecodeZeroPageYIndexed(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"(${o1:X2},Y",
            };
        }

        /// <summary>
        /// <para>Relative - Relative addressing is used only with branch instructions
        /// and establishes a destination for the conditional branch.</para>
        ///
        /// <para>The second byte of the instruction becomes the operand which is an
        /// "Offset" added to the contents of the lower eight bits of the program
        /// counter when the counter is set at the next instruction. The range
        /// of the offset is -128 to +127 bytes from the next instruction.</para>
        /// </summary>
        public static DecodedOperation DecodeRelative(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"${o1:X2}",
            };
        }

        /// <summary>
        /// (IND) - The second byte of the instruction contains a zero page address
        /// serving as the indirect pointer.
        /// </summary>
        public static DecodedOperation DecodeZeroPageIndirect(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"(${o1:X2})",
            };
        }

        /// <summary>
        /// (ABS,X) - The contents of the second and third instruction byte are
        /// added to the X register. The sixteen-bit result is a memory address
        /// containing the effective address (JMP (ABS,X) only).
        /// </summary>
        public static DecodedOperation DecodeAbsoluteIndexedIndirect(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));
            var o2 = bus.Peek((ushort)(programCounter + 2));

            return new DecodedOperation
            {
                Length = 3,
                OpCodeValue = oc,
                Operand1 = o1,
                Operand2 = o2,
                Display = $"(${o2:X2}{o1:X2},X)",
            };
        }

        /// <summary>
        /// (IND,X) - In indexed indirect addressing (referred to as (Indirect,X)),
        /// the second byte of the instruction is added to the contents of the X
        /// register, discarding the carry. The result of this addition points to a
        /// memory location on page zero whose contents are the low order eight bits
        /// of the effective address. The next memory location in page zero contains
        /// the high order eight bits of the effective address. Both memory locations
        /// specifying the high and low order bytes of the effective address
        /// must be in page zero.
        /// </summary>
        public static DecodedOperation DecodeXIndexedIndirect(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"(${o1:X2},X)",
            };
        }

        /// <summary>
        /// (IND),Y - In indirect indexed addressing (referred to as (Indirect),Y), the
        /// second byte of the instruction points to a memory location in page zero. The
        /// contents of this memory location are added to the contents of the Y index
        /// register, the result being the low order eight bits of the effective address.
        /// The carry from this addition is added to the contents of the next page
        /// zero memory location, the result being the high order eight bits
        /// of the effective address.
        /// </summary>
        public static DecodedOperation DecodeIndirectYIndexed(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));

            return new DecodedOperation
            {
                Length = 2,
                OpCodeValue = oc,
                Operand1 = o1,
                Display = $"(${o1:X2}),Y",
            };
        }

        /// <summary>
        /// (ABS) - The second byte of the instruction contains the low order eight
        /// bits of a memory location. The high order eight bits of that memory
        /// location are contained in the third byte of the instruction. The contents
        /// of the fully specified memory location are the low order byte of the
        /// effective address. The next memory location contains the high order
        /// byte of the effective address which is loaded into the sixteen bits
        /// of the program counter (JMP (ABS) only).
        /// </summary>
        public static DecodedOperation DecodeAbsoluteIndirect(ushort programCounter, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            var oc = bus.Peek(programCounter);
            var o1 = bus.Peek((ushort)(programCounter + 1));
            var o2 = bus.Peek((ushort)(programCounter + 2));

            return new DecodedOperation
            {
                Length = 3,
                OpCodeValue = oc,
                Operand1 = o1,
                Operand2 = o2,
                Display = $"(${o2:X2}{o1:X2})",
            };
        }
    }
}
