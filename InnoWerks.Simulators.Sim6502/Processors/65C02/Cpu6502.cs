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

        private readonly OpCodeDefinition[] instructionSet;

        public Cpu6502(IBus bus,
                       Action<ICpu, ushort> preExecutionCallback,
                       Action<ICpu> postExecutionCallback)
            : base(bus, preExecutionCallback, postExecutionCallback)
        {
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            instructionSet = CpuInstructions.GetInstructionSet(CpuClass);

            bus.SetCpu(this);
        }

        protected override OpCodeDefinition[] InstructionSet => instructionSet;

        protected override void Dispatch(OpCodeDefinition opCodeDefinition)
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
                            bus.Read(Registers.ProgramCounter + 1);
                            bus.Read(Registers.ProgramCounter + 2);

                            Registers.ProgramCounter += 2;

                            break;

                        case AddressingMode.Implicit:
                            bus.Read(Registers.ProgramCounter + 1);

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
                        bus.Read(Registers.ProgramCounter + 1);
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
                case OpCode.LAX:
                case OpCode.ANC:
                case OpCode.ALR:
                case OpCode.ARR:
                case OpCode.AXS:
                case OpCode.USBC:
                case OpCode.ANE:
                case OpCode.LXA:
                case OpCode.LAS:
                case OpCode.DOP:
                    switch (opCodeDefinition.AddressingMode)
                    {
                        // A. 1.1 Implied Addressing (2 Cycles)
                        case AddressingMode.Accumulator:
                            {
                                // T1
                                /* var discarded = */
                                bus.Read(Registers.ProgramCounter + 1);
                                opCodeDefinition.Execute(this, 0, 0);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.1. Immediate Addressing (2 Cycles)
                        case AddressingMode.Immediate:
                            {
                                // T1
                                var data = bus.Read(Registers.ProgramCounter + 1);

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.2. Zero Page Addressing (3 Cycles)
                        case AddressingMode.ZeroPage:
                            {
                                // T1
                                var adl = bus.Read(Registers.ProgramCounter + 1);
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
                                var adl = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var adh = bus.Read(Registers.ProgramCounter + 2);

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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var adl = bus.Read((bal + Registers.X) & 0xff);
                                // T4
                                var adh = bus.Read((bal + Registers.X + 1) & 0xff);
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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bah = bus.Read(Registers.ProgramCounter + 2);
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
                                    data = bus.Read((bah << 8) + bal + index);
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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var index = opCodeDefinition.AddressingMode == AddressingMode.ZeroPageXIndexed ?
                                    Registers.X :
                                    Registers.Y;
                                var data = bus.Read((bal + index) & 0xff);

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 2.7. Indirect, Y Addressing Mode (5 or 6 Cycles)
                        case AddressingMode.IndirectYIndexed:
                            {
                                // T1
                                var ial = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bal = bus.Read(ial);
                                // T3
                                var bah = bus.Read((ial + 1) & 0xff);

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
                                    data = bus.Read((bah << 8) + bal + Registers.Y);
                                }

                                opCodeDefinition.Execute(this, 0, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // new 65c 02 adressing mode
                        case AddressingMode.ZeroPageIndirect:
                            {
                                // T1
                                var ial = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bal = bus.Read(ial);
                                // T3
                                var bah = bus.Read((ial + 1) & 0xff);

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
                case OpCode.SAX:
                    byte val = opCodeDefinition.OpCode switch
                    {
                        OpCode.STA => Registers.A,
                        OpCode.STX => Registers.X,
                        OpCode.STY => Registers.Y,
                        OpCode.STZ => 0,
                        OpCode.SAX => (byte)(Registers.A & Registers.X),

                        _ => throw new IllegalOpCodeException("OpCode doesn't map to A, X, or Y")
                    };

                    switch (opCodeDefinition.AddressingMode)
                    {
                        // A. 3.1. Zero Page Addressing (3 Cycles)
                        case AddressingMode.ZeroPage:
                            {
                                // T1
                                var adl = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                opCodeDefinition.Execute(this, adl, val);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 3.2. Absolute Addressing (4 Cycles)
                        case AddressingMode.Absolute:
                            {
                                // T1
                                var adl = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var adh = bus.Read(Registers.ProgramCounter + 2);
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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                /* var discarded = */
                                bus.Read(bal);
                                // T3
                                var adl = bus.Read((bal + Registers.X) & 0xff);
                                // T4
                                var adh = bus.Read((bal + Registers.X + 1) & 0xff);
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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bah = bus.Read(Registers.ProgramCounter + 2);
                                // T3
                                var index = opCodeDefinition.AddressingMode == AddressingMode.AbsoluteXIndexed ?
                                    Registers.X :
                                    Registers.Y;

                                var adl = bal + index;

                                /* var discarded = */
                                bus.Read((bah << 8) + (adl & 0xff));

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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
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
                                var ial = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bal = bus.Read(ial);
                                // T3
                                var bah = bus.Read((ial + 1) & 0xff);
                                // T4
                                var adl = bal + Registers.Y;

                                /* var discarded = */
                                bus.Read((bah << 8) + (adl & 0xff));

                                // T5
                                opCodeDefinition.Execute(this, (ushort)((bah << 8) + adl), val);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        case AddressingMode.ZeroPageIndirect:
                            {
                                // T1
                                var ial = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bal = bus.Read(ial);
                                // T3
                                var bah = bus.Read((ial + 1) & 0xff);

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
                // undocumented RMW combo instructions
                case OpCode.SLO:
                case OpCode.RLA:
                case OpCode.SRE:
                case OpCode.RRA:
                case OpCode.DCP:
                case OpCode.ISC:
                    switch (opCodeDefinition.AddressingMode)
                    {
                        // A. 4.1. Zero Page Addressing (5 Cycles)
                        case AddressingMode.ZeroPage:
                            {
                                // T1
                                var adl = bus.Read(Registers.ProgramCounter + 1);
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
                                var adl = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var adh = bus.Read(Registers.ProgramCounter + 2);
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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
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
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bah = bus.Read(Registers.ProgramCounter + 2);

                                // T3
                                bus.Read((bah << 8) | ((bal + Registers.X) & 0xff));

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

                        // A. 4.5. Absolute, Y Addressing (7 Cycles) - undocumented RMW only
                        case AddressingMode.AbsoluteYIndexed:
                            {
                                // T1
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bah = bus.Read(Registers.ProgramCounter + 2);

                                // T3
                                bus.Read((bah << 8) | ((bal + Registers.Y) & 0xff));

                                // T4
                                var ad = (ushort)((bah << 8) + bal + Registers.Y);
                                var data = bus.Read(ad);

                                // T5
                                bus.Write(ad, data);

                                // T6
                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        // A. 4.6. Indirect, X Addressing (8 Cycles) - undocumented RMW only
                        case AddressingMode.XIndexedIndirect:
                            {
                                // T1
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                bus.Read(bal);
                                // T3
                                var adl = bus.Read((bal + Registers.X) & 0xff);
                                // T4
                                var adh = bus.Read((bal + Registers.X + 1) & 0xff);
                                // T5
                                var ad = (ushort)((adh << 8) | adl);
                                var data = bus.Read(ad);
                                // T6
                                bus.Write(ad, data);
                                // T7
                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        // A. 4.7. Indirect, Y Addressing (8 Cycles) - undocumented RMW only
                        case AddressingMode.IndirectYIndexed:
                            {
                                // T1
                                var ial = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bal = bus.Read(ial);
                                // T3
                                var bah = bus.Read((ial + 1) & 0xff);
                                // T4
                                bus.Read((bah << 8) | ((bal + Registers.Y) & 0xff));
                                // T5
                                var ad = (ushort)((bah << 8) + bal + Registers.Y);
                                var data = bus.Read(ad);
                                // T6
                                bus.Write(ad, data);
                                // T7
                                opCodeDefinition.Execute(this, ad, data);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        default:
                            throw new UnhandledAddressingModeException(Registers.ProgramCounter, opCodeDefinition.OpCodeValue, opCodeDefinition.OpCode, opCodeDefinition.AddressingMode);
                    }
                    break;

                // Undocumented "& H+1" store group -- SHA, SHX, SHY, TAS
                // These follow the store pattern but have a page-crossing quirk:
                // when the indexed address crosses a page boundary, the high byte of the
                // target address is replaced by the stored value (val & (H+1)).
                case OpCode.SHA:
                case OpCode.SHX:
                case OpCode.SHY:
                case OpCode.TAS:
                    switch (opCodeDefinition.AddressingMode)
                    {
                        case AddressingMode.AbsoluteXIndexed:
                        case AddressingMode.AbsoluteYIndexed:
                            {
                                // T1
                                var bal = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bah = bus.Read(Registers.ProgramCounter + 2);
                                // T3
                                var index = opCodeDefinition.AddressingMode == AddressingMode.AbsoluteXIndexed ?
                                    Registers.X :
                                    Registers.Y;

                                // set SP before computing stored value for TAS
                                if (opCodeDefinition.OpCode == OpCode.TAS)
                                {
                                    Registers.StackPointer = (byte)(Registers.A & Registers.X);
                                }

                                byte storedVal = opCodeDefinition.OpCode switch
                                {
                                    OpCode.SHA => (byte)(Registers.A & Registers.X & (bah + 1)),
                                    OpCode.SHX => (byte)(Registers.X & (bah + 1)),
                                    OpCode.SHY => (byte)(Registers.Y & (bah + 1)),
                                    OpCode.TAS => (byte)(Registers.A & Registers.X & (bah + 1)),
                                    _ => 0
                                };

                                var adl = bal + index;

                                // page crossing fixup: high byte becomes the stored value
                                var adh = (adl > 0xff) ? storedVal : bah;

                                bus.Read((bah << 8) | (adl & 0xff));

                                // T4
                                var ad = (ushort)((adh << 8) | (adl & 0xff));
                                bus.Write(ad, storedVal);

                                Registers.ProgramCounter += 3;
                            }
                            break;

                        // SHA (ind),Y
                        case AddressingMode.IndirectYIndexed:
                            {
                                // T1
                                var ial = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var bal = bus.Read(ial);
                                // T3
                                var bah = bus.Read((ial + 1) & 0xff);
                                // T4
                                var adl = bal + Registers.Y;

                                byte storedVal = (byte)(Registers.A & Registers.X & (bah + 1));

                                // page crossing fixup
                                var adh = (adl > 0xff) ? storedVal : bah;

                                bus.Read((bah << 8) | (adl & 0xff));

                                // T5
                                var ad = (ushort)((adh << 8) | (adl & 0xff));
                                bus.Write(ad, storedVal);

                                Registers.ProgramCounter += 2;
                            }
                            break;

                        default:
                            throw new UnhandledAddressingModeException(Registers.ProgramCounter, opCodeDefinition.OpCodeValue, opCodeDefinition.OpCode, opCodeDefinition.AddressingMode);
                    }
                    break;

                // KIL - Halt the processor (undocumented, 11 cycles)
                // The CPU gets stuck attempting what looks like a BRK/interrupt sequence,
                // repeatedly reading the interrupt vector. After this instruction the
                // processor is stopped and requires a hardware reset.
                case OpCode.KIL:
                    {
                        // T1
                        bus.Read(Registers.ProgramCounter + 1);
                        // T2
                        bus.Read(0xffff);
                        // T3
                        bus.Read(0xfffe);
                        // T4
                        bus.Read(0xfffe);
                        // T5 - T10
                        bus.Read(0xffff);
                        bus.Read(0xffff);
                        bus.Read(0xffff);
                        bus.Read(0xffff);
                        bus.Read(0xffff);
                        bus.Read(0xffff);

                        Registers.ProgramCounter++;

                        opCodeDefinition.Execute(this, 0, 0);
                    }
                    break;

                // A. 5.4. Break Operation -- (Hardware Interrupt)-BRK (7 Cycles)
                case OpCode.BRK:
                    {
                        // T1
                        /* var discarded = */
                        bus.Read(Registers.ProgramCounter + 1);
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
                                var adl = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var adh = bus.Read(Registers.ProgramCounter + 2);
                                var ad = (ushort)((adh << 8) | adl);

                                opCodeDefinition.Execute(this, ad, 0);
                            }
                            break;

                        // A. 5.6.2 Indirect Addressing Mode (5 Cycles)
                        case AddressingMode.AbsoluteIndirect:
                            {
                                // T1
                                var ial = bus.Read(Registers.ProgramCounter + 1);
                                // T2
                                var iah = bus.Read(Registers.ProgramCounter + 2);
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
                        var adl = bus.Read(Registers.ProgramCounter + 1);
                        // T2
                        /* var discarded = */
                        bus.Read(StackBase + Registers.StackPointer);
                        // T3
                        StackPush((byte)(((Registers.ProgramCounter + 2) & 0xff00) >> 8));
                        // T4
                        StackPush((byte)((Registers.ProgramCounter + 2) & 0x00ff));
                        // T5
                        var adh = bus.Read(Registers.ProgramCounter + 2);

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
                        bus.Read(Registers.ProgramCounter + 1);
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
                        bus.Read(Registers.ProgramCounter + 1);
                        // T2
                        /* var discarded = */
                        bus.Read(StackBase + Registers.StackPointer);
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
                        bus.Read(Registers.ProgramCounter + 1);
                        // T2
                        bus.Read(StackBase + Registers.StackPointer);
                        // T3 - T5
                        opCodeDefinition.Execute(this, 0, 0);
                    }
                    break;

                // A. 5.7. Return from Subroutine -- RTS (6 Cycles)
                case OpCode.RTS:
                    {
                        // T1
                        bus.Read(Registers.ProgramCounter + 1);
                        // T2
                        bus.Read(StackBase + Registers.StackPointer);
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
                        var offset = bus.Read(Registers.ProgramCounter + 1);

                        // T2 - T3
                        // TODO: verify this does not break tests
                        var addr = (ushort)(Registers.ProgramCounter + 2 + (sbyte)offset);
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

        #region Undocumented RMW combo instructions

        /// <summary>
        /// SLO - Shift Left then OR with Accumulator (undocumented)
        /// MEM &lt;- ASL(MEM), A &lt;- A ORA MEM
        /// Flags affected: n-----zc
        /// </summary>
        public void SLO(ushort addr, byte value)
        {
            Registers.Carry = (value & 0x80) != 0;
            byte result = (byte)(value << 1);
            bus.Write(addr, result);

            Registers.A |= result;
            Registers.SetNZ(Registers.A);
        }

        /// <summary>
        /// RLA - Rotate Left then AND with Accumulator (undocumented)
        /// MEM &lt;- ROL(MEM), A &lt;- A AND MEM
        /// Flags affected: n-----zc
        /// </summary>
        public void RLA(ushort addr, byte value)
        {
            var adjustment = Registers.Carry ? 0x01 : 0x00;
            Registers.Carry = (value & 0x80) != 0;
            byte result = (byte)((value << 1) | adjustment);
            bus.Write(addr, result);

            Registers.A &= result;
            Registers.SetNZ(Registers.A);
        }

        /// <summary>
        /// SRE - Shift Right then EOR with Accumulator (undocumented)
        /// MEM &lt;- LSR(MEM), A &lt;- A EOR MEM
        /// Flags affected: n-----zc
        /// </summary>
        public void SRE(ushort addr, byte value)
        {
            Registers.Carry = (value & 0x01) != 0;
            byte result = (byte)(value >> 1);
            bus.Write(addr, result);

            Registers.A ^= result;
            Registers.SetNZ(Registers.A);
        }

        /// <summary>
        /// RRA - Rotate Right then ADC with Accumulator (undocumented)
        /// MEM &lt;- ROR(MEM), A &lt;- A ADC MEM
        /// Flags affected: nv----zc
        /// </summary>
        public void RRA(ushort addr, byte value)
        {
            var adjustment = (Registers.Carry ? 0x01 : 0x00) << 7;
            Registers.Carry = (value & 0x01) != 0;
            byte result = (byte)((value >> 1) | adjustment);
            bus.Write(addr, result);

            ADC(addr, result);
        }

        /// <summary>
        /// DCP - Decrement then Compare with Accumulator (undocumented)
        /// MEM &lt;- DEC(MEM), A CMP MEM
        /// Flags affected: n-----zc
        /// </summary>
        public void DCP(ushort addr, byte value)
        {
            byte result = (byte)(value - 1);
            bus.Write(addr, result);

            int cmp = Registers.A - result;
            Registers.Carry = Registers.A >= result;
            Registers.SetNZ(cmp);
        }

        /// <summary>
        /// ISC - Increment then Subtract with Carry (undocumented)
        /// MEM &lt;- INC(MEM), A &lt;- A SBC MEM
        /// Flags affected: nv----zc
        /// </summary>
        public void ISC(ushort addr, byte value)
        {
            byte result = (byte)(value + 1);
            bus.Write(addr, result);

            SBC(addr, result);
        }

        // SHA, SHX, SHY, TAS - these are handled entirely in Dispatch() due to
        // the page-crossing address fixup quirk. The table still needs callable methods.
        public void SHA(ushort addr, byte value) { }
        public void SHX(ushort addr, byte value) { }
        public void SHY(ushort addr, byte value) { }
        public void TAS(ushort addr, byte value) { }

        /// <summary>
        /// ANE/XAA - (A OR CONST) AND X AND imm -> A (undocumented, unstable)
        /// CONST = 0xFF for Harte tests
        /// Flags affected: n-----z-
        /// </summary>
        public void ANE(ushort addr, byte value)
        {
            Registers.A = (byte)((Registers.A | 0xee) & Registers.X & value);
            Registers.SetNZ(Registers.A);
        }

        /// <summary>
        /// LXA/ATX - (A OR CONST) AND imm -> A, X (undocumented, unstable)
        /// CONST = 0xFF for Harte tests
        /// Flags affected: n-----z-
        /// </summary>
        public void LXA(ushort addr, byte value)
        {
            Registers.A = (byte)((Registers.A | 0xee) & value);
            Registers.X = Registers.A;
            Registers.SetNZ(Registers.A);
        }

        /// <summary>
        /// LAS/LAR - M AND SP -> A, X, SP (undocumented)
        /// Flags affected: n-----z-
        /// </summary>
        public void LAS(ushort addr, byte value)
        {
            byte result = (byte)(value & Registers.StackPointer);
            Registers.A = result;
            Registers.X = result;
            Registers.StackPointer = result;
            Registers.SetNZ(result);
        }

        /// <summary>
        /// ANC - AND with Carry (undocumented)
        /// A &lt;- A AND imm, C &lt;- bit 7 of result
        /// Flags affected: n-----zc
        /// </summary>
        public void ANC(ushort addr, byte value)
        {
            Registers.A &= value;
            Registers.SetNZ(Registers.A);
            Registers.Carry = (Registers.A & 0x80) != 0;
        }

        /// <summary>
        /// ALR - AND then Logical Shift Right (undocumented)
        /// A &lt;- (A AND imm) >> 1
        /// Flags affected: n-----zc
        /// </summary>
        public void ALR(ushort addr, byte value)
        {
            Registers.A &= value;
            Registers.Carry = (Registers.A & 0x01) != 0;
            Registers.A >>= 1;
            Registers.SetNZ(Registers.A);
        }

        /// <summary>
        /// ARR - AND then Rotate Right (undocumented)
        /// A &lt;- (A AND imm), then ROR A with special C/V handling
        /// Flags affected: nv----zc
        /// </summary>
        public void ARR(ushort addr, byte value)
        {
            Registers.A &= value;

            if (Registers.Decimal)
            {
                var temp = Registers.A;

                // ROR through carry
                var adjustment = (Registers.Carry ? 0x01 : 0x00) << 7;
                Registers.A = (byte)((Registers.A >> 1) | adjustment);
                Registers.SetNZ(Registers.A);

                // V flag: bit 6 XOR bit 5 of the ROR result
                Registers.Overflow = ((Registers.A ^ (Registers.A << 1)) & 0x40) != 0;

                // BCD fixup for low nibble
                if ((temp & 0x0f) + (temp & 0x01) > 0x05)
                {
                    Registers.A = (byte)((Registers.A & 0xf0) | ((Registers.A + 0x06) & 0x0f));
                }

                // BCD fixup for high nibble sets carry
                if ((temp & 0xf0) + (temp & 0x10) > 0x50)
                {
                    Registers.Carry = true;
                    Registers.A = (byte)(Registers.A + 0x60);
                }
                else
                {
                    Registers.Carry = false;
                }
            }
            else
            {
                // ROR through carry
                var adjustment = (Registers.Carry ? 0x01 : 0x00) << 7;
                Registers.A = (byte)((Registers.A >> 1) | adjustment);
                Registers.SetNZ(Registers.A);

                // C <- bit 6 of result
                Registers.Carry = (Registers.A & 0x40) != 0;
                // V <- bit 6 XOR bit 5 of result
                Registers.Overflow = ((Registers.A & 0x40) ^ ((Registers.A & 0x20) << 1)) != 0;
            }
        }

        /// <summary>
        /// AXS/SBX - (A AND X) - imm -> X (undocumented)
        /// X &lt;- (A AND X) - imm, always binary mode
        /// Flags affected: n-----zc
        /// </summary>
        public void AXS(ushort addr, byte value)
        {
            int result = (Registers.A & Registers.X) - value;
            Registers.Carry = result >= 0;
            Registers.X = (byte)(result & 0xff);
            Registers.SetNZ(Registers.X);
        }

        /// <summary>
        /// USBC - Undocumented SBC, identical to SBC immediate (undocumented)
        /// A &lt;- A - imm - !C
        /// Flags affected: nv----zc
        /// </summary>
        public void USBC(ushort addr, byte value)
        {
            SBC(addr, value);
        }

        /// <summary>
        /// SAX - Store A AND X into memory (undocumented)
        /// MEM &lt;- A AND X
        /// Flags affected: none
        /// </summary>
        public void SAX(ushort addr, byte value)
        {
            bus.Write(addr, (byte)(Registers.A & Registers.X));
        }

        /// <summary>
        /// LAX - Load Accumulator and X from memory (undocumented)
        /// A, X &lt;- MEM
        /// Flags affected: n-----z-
        /// </summary>
        public void LAX(ushort addr, byte value)
        {
            Registers.A = value;
            Registers.X = value;
            Registers.SetNZ(value);
        }

        #endregion

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
