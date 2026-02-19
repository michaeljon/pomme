using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using InnoWerks.Processors;

#pragma warning disable CA1822

namespace InnoWerks.Computers.Apple
{
    public class Memory128k
    {
        private readonly MachineState machineState;

        public byte GetPage(ushort address) => (byte)((address & 0xFF00) >> 8);

        public byte GetOffset(ushort address) => (byte)(address & 0x00FF);

        // 48k $00-$C0
        private readonly MemoryPage[] mainMemory;

        // 48k $00-$C0
        private readonly MemoryPage[] auxMemory;

        // active r/w memory, 64k (256 pages $00-$FF)
        private readonly MemoryPage[] activeRead = [];

        private readonly MemoryPage[] activeWrite = [];

        // language card low ram
        private readonly MemoryPage[] languageCardBank2; // $D000-$DFFF

        // language card high ram
        private readonly MemoryPage[] languageCardRam;   // $D000-$FFFF

        // language card low ram
        private readonly MemoryPage[] auxLanguageCardBank2; // $D000-$DFFF

        // language card high ram
        private readonly MemoryPage[] auxLanguageCardRam;   // $D000-$FFFF

        // switch-selectable
        private readonly MemoryPage[] intCxRom;          // $C000-$CFFF

        // swappable lo rom banks
        private readonly MemoryPage[] intDxRom;          // $D000–$DFFF

        // single hi rom bank
        private readonly MemoryPage[] intEFRom;          // $E000–$FFFF

        // device rom, c100-c700, numbered from 0 for convenience
        private readonly MemoryPage[] loSlotRom = new MemoryPage[8];

        // device rom, c800, numbered from 0 for convenience
        private readonly MemoryPage[][] hiSlotRom = new MemoryPage[8][];

        public Memory128k(MachineState machineState)
        {
            this.machineState = machineState;

            //
            // setup enough spac to hold our working memory pointers, let's
            // call it, say, 64k worth of pages
            //

            activeRead = new MemoryPage[64 * 1024 / MemoryPage.PageSize];
            activeWrite = new MemoryPage[64 * 1024 / MemoryPage.PageSize];

            //
            // main and aux ram $0000-$C000
            //

            mainMemory = new MemoryPage[48 * 1024 / MemoryPage.PageSize];
            auxMemory = new MemoryPage[48 * 1024 / MemoryPage.PageSize];
            for (var p = 0x00; p < 0xC0; p++)
            {
                mainMemory[p] = new MemoryPage(MemoryPageType.Ram, "main", (byte)p);
                auxMemory[p] = new MemoryPage(MemoryPageType.Ram, "aux", (byte)p);

                activeRead[p] = mainMemory[p];
                activeWrite[p] = mainMemory[p];
            }

            //
            // language card RAM
            //

            languageCardRam = new MemoryPage[12 * 1024 / MemoryPage.PageSize];
            auxLanguageCardRam = new MemoryPage[12 * 1024 / MemoryPage.PageSize];
            for (var p = 0; p < 12 * 1024 / MemoryPage.PageSize; p++)
            {
                languageCardRam[p] = new MemoryPage(MemoryPageType.LanguageCard, "languageCardRam", (byte)(0xD0 + p));
                auxLanguageCardRam[p] = new MemoryPage(MemoryPageType.LanguageCard, "auxLanguageCardRam", (byte)(0xD0 + p));
            }

            languageCardBank2 = new MemoryPage[4 * 1024 / MemoryPage.PageSize];
            auxLanguageCardBank2 = new MemoryPage[4 * 1024 / MemoryPage.PageSize];
            for (var p = 0; p < 4 * 1024 / MemoryPage.PageSize; p++)
            {
                languageCardBank2[p] = new MemoryPage(MemoryPageType.LanguageCard, "languageCardBank2", (byte)(0xD0 + p));
                auxLanguageCardBank2[p] = new MemoryPage(MemoryPageType.LanguageCard, "auxLanguageCardBank2", (byte)(0xD0 + p));
            }

            //
            // ROM space
            //

            // 4k switch selectable $C000-$CFFF
            intCxRom = new MemoryPage[4 * 1024 / MemoryPage.PageSize];
            for (var p = 0; p < 4 * 1024 / MemoryPage.PageSize; p++)
            {
                intCxRom[p] = new MemoryPage(MemoryPageType.Rom, "intCxRom", (byte)(0xC0 + p));
            }

            // 4k ROM bank 1 $D000-$DFFF
            intDxRom = new MemoryPage[4 * 1024 / MemoryPage.PageSize];
            for (var p = 0; p < 4 * 1024 / MemoryPage.PageSize; p++)
            {
                intDxRom[p] = new MemoryPage(MemoryPageType.Rom, "intDxRom", (byte)(0xD0 + p));
            }

            // 8k ROM $E000-$FFFF
            intEFRom = new MemoryPage[8 * 1024 / MemoryPage.PageSize];
            for (var p = 0; p < 8 * 1024 / MemoryPage.PageSize; p++)
            {
                intEFRom[p] = new MemoryPage(MemoryPageType.Rom, "intEFRom", (byte)(0xE0 + p));
            }

            //
            // slot ROM
            //

            // cx slot rom, one page per slot, $C100-$C7FF
            for (var slot = 0; slot < 8; slot++)
            {
                loSlotRom[slot] = MemoryPage.Zeros(MemoryPageType.CardRom, (byte)(0xC0 + slot));
                // loSlotRom[slot] = null;
            }

            // c8 slot rom, one page per slot, $C800-$CFFF
            for (var slot = 0; slot < 8; slot++)
            {
                hiSlotRom[slot] = new MemoryPage[2048 / MemoryPage.PageSize];

                for (var page = 0; page < 2048 / MemoryPage.PageSize; page++)
                {
                    hiSlotRom[slot][page] = MemoryPage.Zeros(MemoryPageType.CardRom, (byte)(0xC8 + page));
                }
            }

            Remap();
        }

        public void Reset()
        {
            // main and aux memory
            for (var p = 0; p < mainMemory.Length; p++)
            {
                mainMemory[p].ZeroOut();
                auxMemory[p].ZeroOut();
            }

            // language cards
            for (var p = 0; p < languageCardRam.Length; p++)
            {
                languageCardRam[p].ZeroOut();
                auxLanguageCardRam[p].ZeroOut();
            }
            for (var p = 0; p < languageCardBank2.Length; p++)
            {
                languageCardBank2[p].ZeroOut();
                languageCardBank2[p].ZeroOut();
            }

            Remap();
        }

        /// <summary>
        /// Overall memory map
        ///<para>
        /// BSR / ROM                $E0 - $FF   mainMemory / auxMemory / intEFRom
        /// Bank 2                   $D0 - $DF   lcRam
        /// Bank 1                   $D0 - $DF   lcRam / intDxRom
        /// INT ROM                  $C0 - $CF   intCxRom
        /// Hi RAM                   $60 - $BF   mainMemory / auxMemory
        /// Hi-res Page 2            $40 - $5F
        /// Hi-res Page 1            $20 - $3F
        /// RAM                      $0C - $1F
        /// Text Page 2              $08 - $0B
        /// Text Page 1              $04 - $07
        /// BASIC workspace          $02 - $03
        /// zero page and stack      $00 - $01
        /// </para>
        /// </summary>
        public void Remap()
        {
            RemapRead();
            RemapWrite();

            // DumpActiveMemory();
        }

        private void RemapRead()
        {
            //
            // reset the entire memory map
            //
            InjectRam(
                activeRead,
                machineState.State[SoftSwitch.AuxRead] == true ? auxMemory : mainMemory);

            //
            // copy over the rom blocks, we might override below
            //
            InjectRom(intDxRom);
            InjectRom(intEFRom);

            //
            // handle language card
            //

            if (machineState.State[SoftSwitch.LcReadEnabled] == true)
            {
                if (machineState.State[SoftSwitch.ZpAux] == false)
                {
                    InjectRam(activeRead, languageCardRam);
                    if (machineState.State[SoftSwitch.LcBank2] == true)
                    {
                        InjectRam(activeRead, languageCardBank2);
                    }
                }
                else
                {
                    InjectRam(activeRead, auxLanguageCardRam);
                    if (machineState.State[SoftSwitch.LcBank2] == true)
                    {
                        InjectRam(activeRead, auxLanguageCardBank2);
                    }
                }
            }

            //
            // display pages TXT page 1 and HIRES page 1
            //
            if (machineState.State[SoftSwitch.Store80] == true)
            {
                InjectRam(
                    activeRead,
                    machineState.State[SoftSwitch.Page2] == true ? auxMemory : mainMemory,
                    0x04, 0x08
                );

                if (machineState.State[SoftSwitch.HiRes] == true)
                {
                    InjectRam(
                        activeRead,
                        machineState.State[SoftSwitch.Page2] == true ? auxMemory : mainMemory,
                        0x20, 0x40
                    );
                }
            }

            //
            // zero page and stack      $00 - $01
            //
            InjectRam(
                activeRead,
                machineState.State[SoftSwitch.ZpAux] == true ? auxMemory : mainMemory,
                0x00, 0x02
            );

            //                                           $C100-$C2FF
            //      INTCXROM           SLOTC3ROM         $C400-$CFFF    $C300-$C3FF
            //  InternalRomEnabled  Slot3RomEnabled
            //        false              false              slot         internal
            //        false              true               slot           slot
            //        true               false            internal       internal
            //        true               true             internal       internal
            //

            //
            // ROM                      $C0 - $C7
            //
            if (machineState.State[SoftSwitch.IntCxRomEnabled] == true)
            {
                InjectRom(intCxRom);
            }
            else
            {
                // walk each slot and hook up its rom
                for (var slot = 0; slot < 8; slot++)
                {
                    activeRead[0xC0 + slot] = loSlotRom[slot];
                }

                if (machineState.CurrentSlot == 0)
                {
                    for (var loop = 0xC8; loop < 0xD0; loop++)
                    {
                        activeRead[loop] = MemoryPage.Zeros(MemoryPageType.Undefined, (byte)loop);
                    }
                }
                else
                {
                    if (hiSlotRom[machineState.CurrentSlot] != null)
                    {
                        InjectRom(hiSlotRom[machineState.CurrentSlot]);
                    }
                }

                if (machineState.State[SoftSwitch.SlotC3RomEnabled] == false)
                {
                    // point c3 at internal rom
                    activeRead[0xC3] = intCxRom[0x03];
                }

                if (machineState.State[SoftSwitch.IntC8RomEnabled] == true)
                {
                    //
                    // point c8 at internal rom
                    //
                    for (var loop = 0xC8; loop < 0xD0; loop++)
                    {
                        activeRead[loop] = intCxRom[loop - 0xC0];
                    }
                }
            }

            // this is only here to help accesses higher in stack
            // hard-fail since this page is never readable as ram or rom
            activeRead[0xC0] = null;
        }

        private void RemapWrite()
        {
            //
            // reset the entire memory map
            //
            InjectRam(
                activeWrite,
                machineState.State[SoftSwitch.AuxWrite] == true ? auxMemory : mainMemory);

            //
            // mark the lo rom blocks as read-only
            //
            for (var loop = 0xC0; loop < 0xD0; loop++)
            {
                activeWrite[loop] = null;
            }

            //
            // handle language card and/or high ROM
            //
            if (machineState.State[SoftSwitch.LcWriteEnabled] == true)
            {
                if (machineState.State[SoftSwitch.ZpAux] == false)
                {
                    InjectRam(activeWrite, languageCardRam);
                    if (machineState.State[SoftSwitch.LcBank2] == true)
                    {
                        InjectRam(activeWrite, languageCardBank2);
                    }
                }
                else
                {
                    InjectRam(activeWrite, auxLanguageCardRam);
                    if (machineState.State[SoftSwitch.LcBank2] == true)
                    {
                        InjectRam(activeWrite, auxLanguageCardBank2);
                    }
                }
            }
            else
            {
                for (var loop = 0xD0; loop < 0x100; loop++)
                {
                    activeWrite[loop] = null;
                }
            }

            //
            // display pages TXT page 1 and HIRES page 1
            //
            if (machineState.State[SoftSwitch.Store80] == true)
            {
                InjectRam(
                    activeWrite,
                    machineState.State[SoftSwitch.Page2] == true ? auxMemory : mainMemory,
                    0x04, 0x08
                );

                if (machineState.State[SoftSwitch.HiRes] == true)
                {
                    InjectRam(
                        activeWrite,
                        machineState.State[SoftSwitch.Page2] == true ? auxMemory : mainMemory,
                        0x20, 0x40
                    );
                }
            }

            //
            // zero page and stack      $00 - $01
            //
            InjectRam(
                activeWrite,
                machineState.State[SoftSwitch.ZpAux] == true ? auxMemory : mainMemory,
                0x00, 0x02
            );
        }

        /// <summary>
        /// Writes all pages from memoryPages into the proper location
        /// within activeRead, and sets activeWrite to return 0x00
        /// </summary>
        /// <param name="memoryPages">Source ROM pages</param>
        private void InjectRom(MemoryPage[] memoryPages)
        {
            foreach (var memoryPage in memoryPages)
            {
                activeRead[memoryPage.PageNumber] = memoryPage;
                activeWrite[memoryPage.PageNumber] = MemoryPage.FFs(memoryPage.MemoryPageType, memoryPage.PageNumber);
            }
        }

        /// <summary>
        /// Writes all pages from memoryPages into activeMemory (read or write)
        /// </summary>
        /// <param name="activeMemory">Target page read or write</param>
        /// <param name="memoryPages">Source RAM pages</param>
        private void InjectRam(MemoryPage[] activeMemory, MemoryPage[] memoryPages)
        {
            foreach (var memoryPage in memoryPages)
            {
                activeMemory[memoryPage.PageNumber] = memoryPage;
            }
        }

        /// <summary>
        /// Writes selected pages from memoryPages into activeMemory
        /// </summary>
        /// <param name="activeMemory">Target page read or write</param>
        /// <param name="memoryPages">Source RAM pages</param>
        /// <param name="from">Inclusive starting page</param>
        /// <param name="to">Exclusive ending page</param>
        private void InjectRam(MemoryPage[] activeMemory, MemoryPage[] memoryPages, int from, int to)
        {
            for (var p = from; p < to; p++)
            {
                activeMemory[p] = memoryPages[p];
            }
        }

        public void LoadProgramToRom(byte[] objectCode)
        {
            ArgumentNullException.ThrowIfNull(objectCode);

            if (objectCode.Length == 32 * 1024)
            {
                Load32kRom(objectCode);
            }
            else if (objectCode.Length == 16 * 1024)
            {
                Load16kRom(objectCode);
            }
            else
            {
                throw new NotImplementedException("IIe ROM must be 16k or 32k");
            }
        }

        private void Load32kRom(byte[] objectCode)
        {
            if (objectCode.Length != 32 * 1024)
            {
                throw new NotImplementedException("IIe ROM must be 32k");
            }

            for (var page = 0; page < 4 * 1024 / MemoryPage.PageSize; page++)
            {
                // load the first 4k from the 16k block at the end into cx rom
                Array.Copy(objectCode, (16 * 1024) + (page * 0x100), intCxRom[page].Block, 0, 0x100);
            }

            for (var page = 0; page < 4 * 1024 / MemoryPage.PageSize; page++)
            {
                // load the first 4k from the 16k block at the end into lo rom
                Array.Copy(objectCode, (20 * 1024) + (page * 0x100), intDxRom[page].Block, 0, 0x100);
            }

            for (var page = 0; page < 8 * 1024 / MemoryPage.PageSize; page++)
            {
                // load the remaining 8k from the 16k block into hi rom
                Array.Copy(objectCode, (24 * 1024) + (page * 0x100), intEFRom[page].Block, 0, 0x100);
            }
        }

        private void Load16kRom(byte[] objectCode)
        {
            if (objectCode.Length != 16 * 1024)
            {
                throw new NotImplementedException("IIe ROM must be 16k");
            }

            for (var page = 0; page < 4 * 1024 / MemoryPage.PageSize; page++)
            {
                // load the first 4k from the 16k block at the end into cx rom
                Array.Copy(objectCode, (page * 0x100), intCxRom[page].Block, 0, 0x100);
            }

            for (var page = 0; page < 4 * 1024 / MemoryPage.PageSize; page++)
            {
                // load the first 4k from the 16k block at the end into lo rom
                Array.Copy(objectCode, (4 * 1024) + (page * 0x100), intDxRom[page].Block, 0, 0x100);
            }

            for (var page = 0; page < 8 * 1024 / MemoryPage.PageSize; page++)
            {
                // load the remaining 8k from the 16k block into hi rom
                Array.Copy(objectCode, (8 * 1024) + (page * 0x100), intEFRom[page].Block, 0, 0x100);
            }
        }

        public void LoadProgramToRam(byte[] objectCode, ushort origin)
        {
            ArgumentNullException.ThrowIfNull(objectCode);

            var pageNumber = GetPage(origin);
            var pages = objectCode.Length / MemoryPage.PageSize;
            var remainder = objectCode.Length - (pages * MemoryPage.PageSize);

            for (var page = 0; page < pages; page++)
            {
                Array.Copy(objectCode, page * 0x100, mainMemory[pageNumber + page].Block, 0, 256);
            }

            if (remainder > 0)
            {
                Array.Copy(objectCode, pages * 0x100, mainMemory[pageNumber + pages].Block, 0, remainder);
            }
        }

        public void LoadSlotCxRom(int slot, byte[] objectCode)
        {
            // slots load themselves starting at 1, so 0xC6 would map to
            // a Disk II in slot 6
            var memoryPage = new MemoryPage(MemoryPageType.CardRom, $"slot{slot}-cx", (byte)(0xC0 + slot));
            Array.Copy(objectCode, 0, memoryPage.Block, 0, 256);

            loSlotRom[slot] = memoryPage;
        }

        public void LoadSlotC8Rom(int slot, byte[] objectCode)
        {
            hiSlotRom[slot] = new MemoryPage[2048 / MemoryPage.PageSize];

            for (var page = 0; page < 2048 / MemoryPage.PageSize; page++)
            {
                var memoryPage = new MemoryPage(MemoryPageType.CardRom, $"slot{slot}-c8", (byte)(0xC8 + page));
                Array.Copy(objectCode, 0, memoryPage.Block, 0, 256);

                hiSlotRom[slot][page] = memoryPage;
            }
        }

        public MemoryPage ResolveRead(ushort address)
        {
            var page = GetPage(address);

            return activeRead[page];
        }

        public MemoryPage ResolveWrite(ushort address)
        {
            var page = GetPage(address);

            return activeWrite[page];
        }

        /// <summary>
        /// Main READ entry point into activeMemory
        /// </summary>
        /// <param name="address">Virtual 64k address mapped onto backing store</param>
        /// <returns>Value at address</returns>
        public byte Read(ushort address)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            if (activeRead[page] != null)
            {
                return activeRead[page].Block[offset];
            }

            return 0xFF;
        }

        /// <summary>
        /// Main READ entry point into activeMemory. Allows for bulk
        /// read to make consumers faster.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageCount"></param>
        /// <returns></returns>
        public byte[] Read(byte page, int pageCount)
        {
            var bytes = new byte[pageCount * MemoryPage.PageSize];
            for (var p = 0; p < pageCount; p++)
            {
                Array.Copy(activeRead[page + p].Block, 0, bytes, p * MemoryPage.PageSize, MemoryPage.PageSize);
            }
            return bytes;
        }

        /// <summary>
        /// Main WRITE entry point into activeMemory
        /// </summary>
        /// <param name="address">Virtual 64k address mapped onto backing store</param>
        /// <param name="value">Value to write</param>
        public void Write(ushort address, byte value)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            activeWrite[page]?.Block[offset] = value;
        }

        /// <summary>
        /// Allows for bus-tied devices to directly access the
        /// main 64k of RAM. Used primarily by the video system.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public byte GetMain(ushort address)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            return mainMemory[page].Block[offset];
        }

        /// <summary>
        /// Bulk direct-access READ into mainMemory (bypassing mapping)
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageCount"></param>
        /// <returns></returns>
        public byte[] GetMain(byte page, int pageCount)
        {
            var bytes = new byte[pageCount * MemoryPage.PageSize];
            for (var p = 0; p < pageCount; p++)
            {
                Array.Copy(mainMemory[page + p].Block, 0, bytes, p * MemoryPage.PageSize, MemoryPage.PageSize);
            }
            return bytes;
        }

        public void SetMain(ushort address, byte value)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            mainMemory[page].Block[offset] = value;
        }

        public void ZeroMain(ushort address)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            for (var b = 0; b < MemoryPage.PageSize; b++)
            {
                mainMemory[page].Block[offset] = 0x00;
            }
        }

        /// <summary>
        /// Allows for bus-tied devices to directly access the
        /// aux 64k of RAM. Used primarily by the video system.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public byte GetAux(ushort address)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            return auxMemory[page].Block[offset];
        }

        /// <summary>
        /// Bulk direct-access READ into auxMemory (bypassing mapping)
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageCount"></param>
        /// <returns></returns>
        public byte[] GetAux(byte page, int pageCount)
        {
            var bytes = new byte[pageCount * MemoryPage.PageSize];
            for (var p = 0; p < pageCount; p++)
            {
                Array.Copy(auxMemory[page + p].Block, 0, bytes, p * MemoryPage.PageSize, MemoryPage.PageSize);
            }
            return bytes;
        }

        public void SetAux(ushort address, byte value)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            auxMemory[page].Block[offset] = value;
        }

        public void ZeroAux(ushort address)
        {
            var page = GetPage(address);
            var offset = GetOffset(address);

            for (var b = 0; b < MemoryPage.PageSize; b++)
            {
                auxMemory[page].Block[offset] = 0x00;
            }
        }

        internal void DumpPage(MemoryPage memoryPage)
        {
            Debug.WriteLine("MemoryPage {0}", memoryPage);

            DumpPage(memoryPage.Block, memoryPage.PageNumber);
        }

        internal void DumpPage(byte[] page, int pageNumber)
        {
            Debug.Write("       ");
            for (var b = 0; b < 32; b++)
            {
                if (b > 0x00 && b % 0x08 == 0)
                {
                    Debug.Write("  ");
                }

                Debug.Write($"{b:X2} ");
            }

            Debug.WriteLine("");

            Debug.Write("       ");
            for (var b = 0; b < 32; b++)
            {
                if (b > 0x00 && b % 0x08 == 0)
                {
                    Debug.Write("  ");
                }

                Debug.Write($"== ");
            }

            Debug.WriteLine("");

            for (var l = 0; l < 0x100; l += 32)
            {
                Debug.Write($"{l:X4}:  ");

                for (var b = 0; b < 32; b++)
                {
                    if (b > 0x00 && b % 0x08 == 0)
                    {
                        Debug.Write("  ");
                    }

                    Debug.Write($"{page[(ushort)(l + b)]:X2} ");
                }

                Debug.WriteLine("");
            }

            Debug.WriteLine("");
        }

        internal void DumpActiveMemory(int startPage = 0x00, int endPage = 0x100)
        {
            for (int page = startPage; page < endPage; page++)
            {
                if (startPage <= page && page <= endPage)
                {
                    MemoryPage r = activeRead[page];
                    MemoryPage w = activeWrite[page];

                    Debug.WriteLine($"[${page:X2}] -- R: {r}    W: {w}");
                }
            }
        }

        internal void DumpNamedMemory(MemoryPage[] memoryPages)
        {
            for (var p = 0; p < memoryPages.Length; p++)
            {
                Debug.WriteLine($"[{p}]: {memoryPages[p]}");
            }
        }
    }
}
