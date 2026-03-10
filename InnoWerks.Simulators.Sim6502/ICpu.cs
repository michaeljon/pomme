using System;
using InnoWerks.Processors;

//
// things to note: http://www.6502.org/tutorials/65c02opcodes.html
//                 https://xotmatrix.github.io/6502/6502-single-cycle-execution.html
//

#pragma warning disable RCS1163, IDE0060, CA1707, CA1822, CA1716

namespace InnoWerks.Simulators
{
    public interface ICpu
    {
        /// <summary>
        /// Describes the type of CPU: 6502, WDC 65C02, Synertek 65SC02, etc
        /// </summary>
        CpuClass CpuClass { get; }

        /// <summary>
        /// Provides access to the CPU's register set. This is used primarily
        /// as a development feature.
        /// </summary>
        Registers Registers { get; }

        /// <summary>
        /// Resets the CPU state to it's cold boot state
        /// </summary>
        void Reset();

        /// <summary>
        /// Runs a single cycle-accurate CPU instruction and returns the number of
        /// cycles "consumed" during that instruction.
        /// </summary>
        /// <param name="returnPriorToBreak"></param>
        /// <returns>true if the CPU encounters a BRK instruction</returns>
        int Step(bool returnPriorToBreak = false);

        /// <summary>
        /// Provides a non-access counting view at the next instruction.
        /// This is used by the CPU itself to check for intercept handler
        /// execution, and for debuggers and emulators to capture and
        /// display the next call.
        /// </summary>
        /// <returns></returns>
        CpuTraceEntry PeekInstruction();

        /// <summary>
        /// Registers a function that is called when PC == address. This
        /// is used for injecting device driver code that's not implemented
        /// in ROM (think SmartPort and disk controllers). It's up to the
        /// implementation here to set the registers as necessary. The CPU
        /// will continue the in-flight Step upon return from handler.
        /// </summary>
        /// <param name="address">Address that results in call</param>
        /// <param name="handler">Handler function for the intercept</param>
        void AddIntercept(ushort address, Action<ICpu, IBus> handler);

        void ClearIntercept(ushort address);

        void ClearIntercepts();

        /// <summary>
        /// Pushes a byte onto the stack at the current value of SP
        /// and adjusts SP accordingly. This is used as a development
        /// feature for running the core 6502 simulator.
        /// </summary>
        /// <param name="b"></param>
        void StackPush(byte b);

        /// <summary>
        /// Pops a byte from the stack at the current value of SP
        /// and adjusts SP accordingly. This is used as a development
        /// feature for running the core 6502 simulator.
        /// </summary>
        /// <returns></returns>
        byte StackPop();
    }
}
