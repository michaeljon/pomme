using System;
using InnoWerks.Processors;

//
// things to note: http://www.6502.org/tutorials/65c02opcodes.html
//                 https://xotmatrix.github.io/6502/6502-single-cycle-execution.html
//

#pragma warning disable RCS1163, IDE0060, CA1707, CA1822

namespace InnoWerks.Simulators
{
    public class Cpu6502 : Cpu6502Core
    {
        public override CpuClass CpuClass => CpuClass.WDC6502;

        public Cpu6502(IBus bus,
                       Action<ICpu, ushort> preExecutionCallback,
                       Action<ICpu> postExecutionCallback)
            : base(bus, preExecutionCallback, postExecutionCallback)
        {
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            bus.SetCpu(this);
        }

        protected override InstructionSet InstructionSet => CpuInstructions.OpCode6502;

        protected override void Dispatch(OpCodeDefinition opCodeDefinition, bool writeInstructions = false)
        {
            ArgumentNullException.ThrowIfNull(opCodeDefinition);

            switch (opCodeDefinition.OpCode)
            {
                // This is the case where we're running into an unknown or undocumented
                // opcode. What we need to do is handle the bus access and cycle counting
                // correctly for the addressing mode.
                case OpCode.Unknown:
                    switch (opCodeDefinition.AddressingMode)
                    {
                        case AddressingMode.Immediate:
                            bus.Read((ushort)(Registers.ProgramCounter + 1));
                            bus.Read((ushort)(Registers.ProgramCounter + 2));

                            Registers.ProgramCounter += 2;

                            break;

                        case AddressingMode.Implicit:
                            bus.Read((ushort)(Registers.ProgramCounter + 1));

                            Registers.ProgramCounter++;

                            break;
                    }

                    break;

                // A. 1. SINGLE-BYTE INSTRUCTIONS
                // These single-byte instructions require two cycles to execute. During the second
                // cycle the address of the next instruction in program sequence will be placed on
                // the address bus. However, the OP CODE which appears on the data bus during the
                // second cycle will be ignored. This same instruction will be fetched on the following
                // cycle, at which time it will be decoded and executed. The ASL, LSR, ROL and ROR
                // instructions apply to the accumulator mode of address.

                case OpCode.ASL_A:
                case OpCode.CLC:
                case OpCode.CLD:
                case OpCode.CLI:
                case OpCode.CLV:
                case OpCode.DEA:
                case OpCode.DEX:
                case OpCode.DEY:
                case OpCode.INA:
                case OpCode.INX:
                case OpCode.INY:
                case OpCode.LSR_A:
                case OpCode.NOP:
                case OpCode.ROL_A:
                case OpCode.ROR_A:
                case OpCode.SEC:
                case OpCode.SED:
                case OpCode.SEI:
                case OpCode.TAX:
                case OpCode.TAY:
                case OpCode.TSX:
                case OpCode.TXA:
                case OpCode.TXS:
                case OpCode.TYA:
                    // A. 1.1 Implied Addressing (2 Cycles)
                    {
                        // T1
                        /* var discarded = */
                        bus.Read((ushort)(Registers.ProgramCounter + 1));
                        opCodeDefinition.Execute(this, 0, 0);

                        Registers.ProgramCounter++;
                    }
                    break;

                // A. 2. INTERNAL EXECUTION ON MEMORY DATA
                // The instructions listed above will execute by performing operations inside the microprocessor
                // using data fetched from the effective address. This total operation requires three steps. The
                // first step (one cycle) is the OP CODE fetch. The second (zero to four cycles) Is the calculation
                // of an effective address. The final step is the fetching of the data from the effective address.
                // Execution of the instruction takes place during the fetching and decoding of the next instruction.
                case OpCode.ADC:
                case OpCode.AND:
                case OpCode.BIT:
                case OpCode.CMP:
                case OpCode.CPX:
                case OpCode.CPY:
                case OpCode.EOR:
                case OpCode.LDA:
                case OpCode.LDX:
                case OpCode.LDY:
                case OpCode.ORA:
                case OpCode.SBC:
                    switch (opCodeDefinition.AddressingMode)
                    {
                        // A. 1.1 Implied Addressing (2 Cycles)
                        case AddressingMode.Accumulator:
                            {
                                // T1
                                /* var discarded = */
                                bus.Read((ushort)(Registers.ProgramCounter + 1));
                                opCodeDefinition.Execute(this, 0, 0);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.1. Immediate Addressing (2 Cycles)
                        case AddressingMode.Immediate:
                            {
                                // T1
                                var data = bus.Read((ushort)(Registers.ProgramCounter + 1));

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.2. Zero Page Addressing (3 Cycles)
                        case AddressingMode.ZeroPage:
                            {
                                // T1
                                var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var data = bus.Read(adl);

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.3. Absolute Addressing (4 Cycles)
                        case AddressingMode.Absolute:
                            {
                                // T1
                                var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var adh = bus.Read((ushort)(Registers.ProgramCounter + 2));

                                // T3
                                var ad = (ushort)((adh << 8) | adl);

                                var data = bus.Read(ad);

                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        // A. 2.4. Indirect, X Addressing (6 Cycles)
                        case AddressingMode.XIndexedIndirect:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var adl = bus.Read((ushort)((bal + Registers.X) & 0xff));
                                // T4
                                var adh = bus.Read((ushort)((bal + Registers.X + 1) & 0xff));
                                // T5
                                var ad = (ushort)((adh << 8) | adl);

                                var data = bus.Read(ad);

                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.5. Absolute, X or Absolute, Y Addressing (4 or 5 Cycles)
                        case AddressingMode.AbsoluteXIndexed:
                        case AddressingMode.AbsoluteYIndexed:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var bah = bus.Read((ushort)(Registers.ProgramCounter + 2));
                                // T3
                                var index = opCodeDefinition.AddressingMode == AddressingMode.AbsoluteXIndexed ?
                                    Registers.X :
                                    Registers.Y;

                                var adl = bal + index;              // Fetch Data (No Page Crossing)
                                var adh = bah;                      // Carry is 0 or 1 as Required from Previous Add Operation
                                var ad = (ushort)((adh << 8) + (adl & 0xff));

                                var data = bus.Read(ad);

                                // T4
                                var adWithIndex = (ushort)((adh << 8) + bal + index);
                                var adWithoutIndex = (ushort)((adh << 8) + bal);

                                if ((adWithIndex & 0xff00) != (adWithoutIndex & 0xff00))
                                {
                                    data = bus.Read((ushort)((bah << 8) + bal + index));
                                }

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        // A. 2.6. Zero Page, X or Zero Page, Y Addressing Modes (4 Cycles)
                        case AddressingMode.ZeroPageXIndexed:
                        case AddressingMode.ZeroPageYIndexed:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var index = opCodeDefinition.AddressingMode == AddressingMode.ZeroPageXIndexed ?
                                    Registers.X :
                                    Registers.Y;
                                var data = bus.Read((ushort)((bal + index) & 0xff));

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.7. Indirect, Y Addressing Mode (5 or 6 Cycles)
                        case AddressingMode.IndirectYIndexed:
                            {
                                // T1
                                var ial = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var bal = bus.Read((ushort)(ial));
                                // T3
                                var bah = bus.Read((ushort)((ial + 1) & 0xff));

                                // T4
                                var adl = bal + Registers.Y;            // Fetch Data (No Page Crossing)
                                var adh = bah;                          // Carry is 0 or 1 as Required from Previous Add Operation
                                var ad = (ushort)((adh << 8) + (adl & 0xff));

                                var data = bus.Read(ad);

                                // T5
                                var adWithIndex = (ushort)((adh << 8) + bal + Registers.Y);
                                var adWithoutIndex = (ushort)((adh << 8) + bal);

                                if ((adWithIndex & 0xff00) != (adWithoutIndex & 0xff00))
                                {
                                    data = bus.Read((ushort)((bah << 8) + bal + Registers.Y));
                                }

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // new 65c 02 adressing mode
                        case AddressingMode.ZeroPageIndirect:
                            {
                                // T1
                                var ial = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var bal = bus.Read((ushort)(ial));
                                // T3
                                var bah = bus.Read((ushort)((ial + 1) & 0xff));

                                // T4
                                var ad = (ushort)((bah << 8) | bal);

                                var data = bus.Read(ad);

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        default:
                            throw new UnhandledAddressingModeException(Registers.ProgramCounter, opCodeDefinition.OpCodeValue, opCodeDefinition.OpCode, opCodeDefinition.AddressingMode);
                    }
                    break;

                // A. 3. STORE OPERATIONS
                // The specific steps taken in the Store Operations are very similar to those taken in the
                // previous group (internal execution on memory data). However, in the Store Operation, the
                // fetch of data is replaced by a WRITE (R/W = 0) cycle. No overlapping occurs and no
                // shortening of the instruction time occurs on indexing operations.
                case OpCode.STA:
                case OpCode.STX:
                case OpCode.STY:
                case OpCode.STZ:
                    byte val = opCodeDefinition.OpCode switch
                    {
                        OpCode.STA => Registers.A,
                        OpCode.STX => Registers.X,
                        OpCode.STY => Registers.Y,
                        OpCode.STZ => 0,

                        _ => throw new IllegalOpCodeException("OpCode doesn't map to A, X, or Y")
                    };

                    switch (opCodeDefinition.AddressingMode)
                    {
                        // A. 3.1. Zero Page Addressing (3 Cycles)
                        case AddressingMode.ZeroPage:
                            {
                                // T1
                                var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                opCodeDefinition.Execute(this, adl, val);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 3.2. Absolute Addressing (4 Cycles)
                        case AddressingMode.Absolute:
                            {
                                // T1
                                var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var adh = bus.Read((ushort)(Registers.ProgramCounter + 2));
                                // T3
                                var ad = (ushort)((adh << 8) | adl);
                                opCodeDefinition.Execute(this, ad, val);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        // A. 3.3. Indirect, X Addressing (6 Cycles)
                        case AddressingMode.XIndexedIndirect:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var adl = bus.Read((ushort)((bal + Registers.X) & 0xff));
                                // T4
                                var adh = bus.Read((ushort)((bal + Registers.X + 1) & 0xff));
                                // T5
                                var ad = (ushort)((adh << 8) | adl);
                                opCodeDefinition.Execute(this, ad, val);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 3.4. Absolute, X or Absolute, Y Addressing (5 Cycles)
                        case AddressingMode.AbsoluteXIndexed:
                        case AddressingMode.AbsoluteYIndexed:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var bah = bus.Read((ushort)(Registers.ProgramCounter + 2));
                                // T3
                                var index = opCodeDefinition.AddressingMode == AddressingMode.AbsoluteXIndexed ?
                                    Registers.X :
                                    Registers.Y;

                                var adl = bal + index;

                                /* var discarded = */
                                bus.Read((ushort)((bah << 8) + (adl & 0xff)));

                                // T4
                                opCodeDefinition.Execute(this, (ushort)((bah << 8) + adl), val);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        // A. 3.5. Zero Page, X or Zero Page, Y Addressing Modes (4 Cycles)
                        case AddressingMode.ZeroPageXIndexed:
                        case AddressingMode.ZeroPageYIndexed:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var index = opCodeDefinition.AddressingMode == AddressingMode.ZeroPageXIndexed ?
                                    Registers.X :
                                    Registers.Y;
                                var adl = (ushort)((bal + index) & 0xff);

                                // T4
                                opCodeDefinition.Execute(this, adl, val);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 3.6. Indirect, Y Addressing Mode (6 Cycles)
                        case AddressingMode.IndirectYIndexed:
                            {
                                // T1
                                var ial = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var bal = bus.Read((ushort)(ial));
                                // T3
                                var bah = bus.Read((ushort)((ial + 1) & 0xff));
                                // T4
                                var adl = bal + Registers.Y;

                                /* var discarded = */
                                bus.Read((ushort)((bah << 8) + (adl & 0xff)));

                                // T5
                                opCodeDefinition.Execute(this, (ushort)((bah << 8) + adl), val);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        case AddressingMode.ZeroPageIndirect:
                            {
                                // T1
                                var ial = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var bal = bus.Read((ushort)(ial));
                                // T3
                                var bah = bus.Read((ushort)((ial + 1) & 0xff));

                                // T4
                                var ad = (ushort)((bah << 8) | bal);

                                opCodeDefinition.Execute(this, ad, 0);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        default:
                            throw new UnhandledAddressingModeException(Registers.ProgramCounter, opCodeDefinition.OpCodeValue, opCodeDefinition.OpCode, opCodeDefinition.AddressingMode);
                    }
                    break;

                // A. 4. READ -- MODIFY -- WRITE OPERATIONS
                // The Read -- Modify -- Write operations involve the loading of operands from the
                // operand address, modification of the operand and the resulting modified data being
                // stored in the original location.
                case OpCode.ASL:
                case OpCode.LSR:
                case OpCode.DEC:
                case OpCode.INC:
                case OpCode.ROL:
                case OpCode.ROR:
                    switch (opCodeDefinition.AddressingMode)
                    {
                        // A. 4.1. Zero Page Addressing (5 Cycles)
                        case AddressingMode.ZeroPage:
                            {
                                // T1
                                var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var data = bus.Read(adl);
                                // T3
                                bus.Write(adl, data);
                                // T4
                                opCodeDefinition.Execute(this, adl, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 4.2. Absolute Addressing (6 Cycles)
                        case AddressingMode.Absolute:
                            {
                                // T1
                                var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var adh = bus.Read((ushort)(Registers.ProgramCounter + 2));
                                // T3
                                var ad = (ushort)((adh << 8) | adl);
                                var data = bus.Read(ad);
                                // T4
                                bus.Write(ad, data);
                                // T3
                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        // A. 4.3. Zero Page, X Addressing (6 Cycles)
                        case AddressingMode.ZeroPageXIndexed:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var ad = (ushort)((bal + Registers.X) & 0xff);
                                var data = bus.Read(ad);
                                // T4
                                bus.Write(ad, data);
                                // T5
                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 4.4. Absolute, X Addressing (7 Cycles)
                        case AddressingMode.AbsoluteXIndexed:
                            {
                                // T1
                                var bal = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var bah = bus.Read((ushort)(Registers.ProgramCounter + 2));

                                // T3
                                bus.Read((ushort)((bah << 8) | ((bal + Registers.X) & 0xff)));

                                // T4
                                var ad = (ushort)((bah << 8) + bal + Registers.X);
                                var data = bus.Read(ad);

                                // T5
                                bus.Write(ad, data);

                                // T6
                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        default:
                            throw new UnhandledAddressingModeException(Registers.ProgramCounter, opCodeDefinition.OpCodeValue, opCodeDefinition.OpCode, opCodeDefinition.AddressingMode);
                    }
                    break;

                // A. 5.4. Break Operation -- (Hardware Interrupt)-BRK (7 Cycles)
                case OpCode.BRK:
                    {
                        // T1
                        /* var discarded = */
                        bus.Read((ushort)(Registers.ProgramCounter + 1));
                        // T2
                        StackPush((byte)(((Registers.ProgramCounter + 2) & 0xff00) >> 8));
                        // T3
                        StackPush((byte)((Registers.ProgramCounter + 2) & 0x00ff));
                        // T4
                        StackPush((byte)(Registers.ProcessorStatus | (byte)ProcessorStatusBit.BreakCommand));
                        // T5
                        var adl = bus.Read(IrqVectorL);
                        // T6
                        var adh = bus.Read(IrqVectorH);

                        opCodeDefinition.Execute(this, RegisterMath.MakeShort(adh, adl), 0);
                    }
                    break;

                // A. 5.6. Jump Operation -- JMP
                case OpCode.JMP:
                    switch (opCodeDefinition.AddressingMode)
                    {
                        // A. 5.6.1 Absolute Addressing Mode (3 Cycles)
                        case AddressingMode.Absolute:
                            {
                                // T1
                                var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var adh = bus.Read((ushort)(Registers.ProgramCounter + 2));
                                var ad = (ushort)((adh << 8) | adl);

                                opCodeDefinition.Execute(this, ad, 0);
                            }
                            break;

                        // A. 5.6.2 Indirect Addressing Mode (5 Cycles)
                        case AddressingMode.AbsoluteIndirect:
                            {
                                // T1
                                var ial = bus.Read((ushort)(Registers.ProgramCounter + 1));
                                // T2
                                var iah = bus.Read((ushort)(Registers.ProgramCounter + 2));
                                // T3
                                var ad = RegisterMath.MakeShort(iah, ial);
                                var adl = bus.Read(ad);

                                // T4
                                var aa = RegisterMath.MakeShort(iah, (byte)((ial + 1) & 0xff));
                                var adh = bus.Read(aa);

                                opCodeDefinition.Execute(this, RegisterMath.MakeShort(adh, adl), 0);
                            }
                            break;

                        default:
                            throw new UnhandledAddressingModeException(Registers.ProgramCounter, opCodeDefinition.OpCodeValue, opCodeDefinition.OpCode, opCodeDefinition.AddressingMode);
                    }
                    break;

                // A. 5.3. Jump to Subroutine -- JSR (6 Cycles)
                case OpCode.JSR:
                    {
                        // T1
                        var adl = bus.Read((ushort)(Registers.ProgramCounter + 1));
                        // T2
                        /* var discarded = */
                        bus.Read((ushort)(StackBase + Registers.StackPointer));
                        // T3
                        StackPush((byte)(((Registers.ProgramCounter + 2) & 0xff00) >> 8));
                        // T4
                        StackPush((byte)((Registers.ProgramCounter + 2) & 0x00ff));
                        // T5
                        var adh = bus.Read((ushort)(Registers.ProgramCounter + 2));

                        opCodeDefinition.Execute(this, RegisterMath.MakeShort(adh, adl), 0);
                    }
                    break;

                // A. 5.1. Push Operations -- PHP, PHA (3 Cycles)
                case OpCode.PHA:
                case OpCode.PHP:
                case OpCode.PHX:
                case OpCode.PHY:
                    {
                        // T1
                        /* var discarded = */
                        bus.Read((ushort)(Registers.ProgramCounter + 1));
                        // T2
                        opCodeDefinition.Execute(this, 0, 0);

                        Registers.ProgramCounter++;
                    }
                    break;

                // A. 5.2. Pull Operations -- PLP, PLA (4 Cycles)
                case OpCode.PLA:
                case OpCode.PLP:
                case OpCode.PLX:
                case OpCode.PLY:
                    {
                        // T1
                        /* var discarded = */
                        bus.Read((ushort)(Registers.ProgramCounter + 1));
                        // T2
                        /* var discarded = */
                        bus.Read((ushort)(StackBase + Registers.StackPointer));
                        // T3
                        opCodeDefinition.Execute(this, 0, 0);

                        Registers.ProgramCounter++;
                    }
                    break;

                // A. 5.5. Return from Interrupt -- RTI (6 Cycles)
                case OpCode.RTI:
                    {
                        // T1
                        /* var discarded = */
                        bus.Read((ushort)(Registers.ProgramCounter + 1));
                        // T2
                        bus.Read((ushort)(StackBase + Registers.StackPointer));
                        // T3 - T5
                        opCodeDefinition.Execute(this, 0, 0);
                    }
                    break;

                // A. 5.7. Return from Subroutine -- RTS (6 Cycles)
                case OpCode.RTS:
                    {
                        // T1
                        bus.Read((ushort)(Registers.ProgramCounter + 1));
                        // T2
                        bus.Read((ushort)(StackBase + Registers.StackPointer));
                        // T3 - T5
                        opCodeDefinition.Execute(this, 0, 0);
                    }
                    break;

                // A. 5.8. Branch Operation -- BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS (2, 3, or 4 Cycles)
                case OpCode.BRA:
                case OpCode.BCC:
                case OpCode.BCS:
                case OpCode.BEQ:
                case OpCode.BMI:
                case OpCode.BNE:
                case OpCode.BPL:
                case OpCode.BVC:
                case OpCode.BVS:
                    {
                        // T1
                        var offset = bus.Read((ushort)(Registers.ProgramCounter + 1));

                        // T2 - T3
                        var addr = (ushort)(Registers.ProgramCounter + 2 + ((sbyte)offset < 0 ? (sbyte)offset : offset));
                        opCodeDefinition.Execute(this, addr, offset);
                    }
                    break;

                default:
                    // this is unexpected...
                    throw new IllegalOpCodeException(Registers.ProgramCounter, opCodeDefinition.OpCodeValue);
            }
        }

        /// <summary>
        /// <para>ADC - Add with Carry 6502</para>
        /// <code>
        /// Flags affected: nv----zc
        ///
        /// A ← A + M + c
        ///
        /// n ← Most significant bit of result
        /// v ← Signed overflow of result
        /// z ← Set if the result is zero
        /// c ← Carry from ALU (bit 8/16 of result)
        /// </code>
        /// </summary>
        public override void ADC(ushort addr, byte value)
        {
            int adjustment = Registers.Carry ? 0x01 : 0x00;

            if (Registers.Decimal == true)
            {
                int w = (Registers.A & 0x0f) + (value & 0x0f) + adjustment;
                if (w > 0x09)
                {
                    w += 0x06;
                }
                if (w <= 0x0f)
                {
                    w = (w & 0x0f) + (Registers.A & 0xf0) + (value & 0xf0);
                }
                else
                {
                    w = (w & 0x0f) + (Registers.A & 0xf0) + (value & 0xf0) + 0x10;
                }

                Registers.Zero = RegisterMath.IsZero((Registers.A + value + adjustment) & 0xff);
                Registers.Negative = RegisterMath.IsHighBitSet(w);
                Registers.Overflow = ((Registers.A ^ w) & 0x80) != 0 && ((Registers.A ^ value) & 0x80) == 0;

                if ((w & 0x1f0) > 0x90)
                {
                    w += 0x60;
                }

                Registers.Carry = (w & 0xff0) > 0xf0;
                Registers.A = RegisterMath.TruncateToByte(w);
            }
            else
            {
                int w = Registers.A + value + adjustment;

                Registers.Carry = w > 0xff;
                Registers.Overflow = ((Registers.A & 0x80) == (value & 0x80)) && ((Registers.A & 0x80) != (w & 0x80));
                Registers.A = RegisterMath.TruncateToByte(w);
                Registers.SetNZ(Registers.A);
            }
        }

        /// <summary>
        /// <para>SBC - Subtract with Borrow from Accumulator 6502</para>
        /// <code>
        /// Flags affected: nv----zc
        ///
        /// A ← A + (~M) + c
        ///
        /// n ← Most significant bit of result
        /// v ← Signed overflow of result
        /// z ← Set if the Accumulator is zero
        /// c ← Carry from ALU(bit 8/16 of result) (set if borrow not required)
        /// </code>
        /// </summary>
        public override void SBC(ushort addr, byte value)
        {
            int adjustment = Registers.Carry ? 0x00 : 0x01;
            int result = Registers.A - value - adjustment;

            bool borrowNeeded = false;
            if (result < 0)
            {
                borrowNeeded = true;
            }

            if (Registers.Decimal == true)
            {
                int val = (Registers.A & 0x0f) - (value & 0x0f) - adjustment;
                if ((val & 0x10) != 0)
                {
                    val = ((val - 0x06) & 0x0f) | ((Registers.A & 0xf0) - (value & 0xf0) - 0x10);
                }
                else
                {
                    val = (val & 0x0f) | ((Registers.A & 0xf0) - (value & 0xf0));
                }
                if ((val & 0x100) != 0)
                {
                    val -= 0x60;
                }

                // Registers.Carry = result < 0x100;
                Registers.Carry = !borrowNeeded;
                Registers.SetNZ(result);
                Registers.Overflow = ((Registers.A ^ result) & 0x80) != 0 && ((Registers.A ^ value) & 0x80) != 0;
                Registers.A = RegisterMath.TruncateToByte(val);
            }
            else
            {
                // Registers.Carry = result < 0x100;
                Registers.Carry = !borrowNeeded;
                Registers.Overflow = ((Registers.A & 0x80) != (value & 0x80)) && ((Registers.A & 0x80) != (result & 0x80));
                Registers.A = RegisterMath.TruncateToByte(result);
                Registers.SetNZ(Registers.A);
            }
        }
    }
}
