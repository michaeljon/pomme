using System.Collections.Generic;

namespace InnoWerks.Computers.Apple
{
    public static class SoftSwitchAddress
    {
        //
        // see http://apple2.guidero.us/doku.php/mg_notes/general/io_page
        //

        public const ushort KBD = 0xC000;   // Last Key Pressed (+ 128 if strobe not cleared)
        public const ushort KBDSTRB = 0xC010;  //  1=key pressed 0=keys free    (clears strobe)

        // MEMORY MANAGEMENT SOFT SWITCHES

        public const ushort RDLCBNK2 = 0xC011;  // 1=bank2 available    0=bank1 available
        public const ushort RDLCRAM = 0xC012;  // 1=BSR active for read 0=$D000-$FFFF active

        public const ushort RDMAINRAM = 0xC002;  // Read enable main memory from $0200-$BFFF
        public const ushort RDCARDRAM = 0xC003;  //  Read enable aux memory from $0200-$BFFF
        public const ushort RDRAMRD = 0xC013;  //  0=main $0200-$BFFF active reads  1=aux active

        public const ushort WRMAINRAM = 0xC004;  // Write enable main memory from $0200-$BFFF
        public const ushort WRCARDRAM = 0xC005;  // Write enable aux memory from $0200-$BFFF
        public const ushort RDRAMWRT = 0xC014;  //  0=main $0200-$BFFF active writes 1=aux writes

        public const ushort SETSLOTCXROM = 0xC006;  // Enable slot ROM from $C100-$CFFF
        public const ushort SETINTCXROM = 0xC007;  // Enable main ROM from $C100-$CFFF
        public const ushort RDCXROM = 0xC015;  // 1=main $C100-$CFFF ROM active 0=slot active

        public const ushort CLRALSTKZP = 0xC008;  // Enable main memory from $0000-$01FF & avl BSR
        public const ushort SETALTSTKZP = 0xC009;  //  Enable aux memory from $0000-$01FF & avl BSR
        public const ushort RDALTSTKZP = 0xC016;  //  1=aux $0000-$1FF+auxBSR    0=main available

        public const ushort SETINTC3ROM = 0xC00A;  // Enable main ROM from $C300-$C3FF
        public const ushort SETSLOTC3ROM = 0xC00B;  // Enable slot ROM from $C300-$C3FF
        public const ushort RDC3ROM = 0xC017;  // 1=slot $C3 ROM active 0=main $C3 ROM active

        // VIDEO SOFT SWITCHES

        public const ushort CLRALTCHAR = 0xC00E;  // Turn off alternate characters
        public const ushort SETALTCHAR = 0xC00F;  // Turn on alternate characters
        public const ushort RDALTCHR = 0xC01E;  // 1=alt character set on   0=alt char set off

        public const ushort CLR80COL = 0xC00C;  // Turn off 80 column display
        public const ushort SET80COL = 0xC00D;  //  Turn on 80 column display
        public const ushort RD80COL = 0xC01F;  //  1=80 col display on 0=80 col display off

        public const ushort CLR80STORE = 0xC000;  // Allow page2 to switch video page1 page2
        public const ushort SET80STORE = 0xC001;  // Allow page2 to switch main & aux video memory
        public const ushort RD80STORE = 0xC018;  //  1=page2 switches main/aux   0=page2 video

        public const ushort TXTPAGE1 = 0xC054;  // Select panel display (or main video memory)
        public const ushort TXTPAGE2 = 0xC055;  //  Select page2 display (or aux video memory)
        public const ushort RDPAGE2 = 0xC01C;  //  1=video page2 selected or aux

        public const ushort TXTCLR = 0xC050;  //  Select graphics mode
        public const ushort TXTSET = 0xC051;  //  Select text mode
        public const ushort RDTEXT = 0xC01A;  //  1=text mode is active 0=graphics mode active

        public const ushort MIXCLR = 0xC052;  // Use full screen for graphics
        public const ushort MIXSET = 0xC053;  //  Use graphics with 4 lines of text
        public const ushort RDMIXED = 0xC01B;  //  1=mixed graphics & text    0=full screen

        public const ushort LORES = 0xC056;  // Select low resolution graphics
        public const ushort HIRES = 0xC057;  //  Select high resolution graphics
        public const ushort RDHIRES = 0xC01D;  //  1=high resolution graphics   0=low resolution

        public const ushort IOUDISON = 0xC07E;
        public const ushort IOUDISOFF = 0xC07F;
        public const ushort RDIOUDIS = 0xC07E;

        public const ushort DHIRESON = 0xC05E;
        public const ushort DHIRESOFF = 0xC05F;
        public const ushort RDDHIRES = 0xC07F;

        public const ushort RDVBLBAR = 0xC019;  // 1=vertical retrace on 0=vertical retrace off

        // Annunciator pairs
        public const ushort CLRAN0 = 0xC058;
        public const ushort SETAN0 = 0xC059;
        public const ushort CLRAN1 = 0xC05A;
        public const ushort SETAN1 = 0xC05B;
        public const ushort CLRAN2 = 0xC05C;
        public const ushort SETAN2 = 0xC05D;
        public const ushort CLRAN3 = 0xC05E;
        public const ushort SETAN3 = 0xC05F;

        // Other hardware
        public const ushort SPKR = 0xC030;
        public const ushort TAPEOUT = 0xC020;
        public const ushort TAPEIN = 0xC060;
        public const ushort STROBE = 0xC040;

        public const ushort BUTTON0 = 0xC061;
        public const ushort OPENAPPLE = 0xC061;
        public const ushort BUTTON1 = 0xC062;
        public const ushort SOLIDAPPLE = 0xC062;
        public const ushort BUTTON2 = 0xC063;
        public const ushort SHIFT = 0xC063;

        public const ushort PADDLE0 = 0xC064;
        public const ushort PADDLE1 = 0xC065;
        public const ushort PADDLE2 = 0xC066;
        public const ushort PADDLE3 = 0xC067;
        public const ushort PTRIG = 0xC070;

        public static string LookupAddress(ushort address)
        {
            return address switch
            {
                0xC080 or 0xC084 => $"READBSR2 {address & 0x000F:b4}",
                0xC081 or 0xC085 => $"WRITEBSR2 {address & 0x000F:b4}",
                0xC082 or 0xC086 => $"OFFSBR2 {address & 0x000F:b4}",
                0xC083 or 0xC087 => $"RDWRBSR2 {address & 0x000F:b4}",
                0xC088 or 0xC08C => $"READBSR1 {address & 0x000F:b4}",
                0xC089 or 0xC08D => $"WRITEBSR1 {address & 0x000F:b4}",
                0xC08A or 0xC08E => $"OFFSBR1 {address & 0x000F:b4}",
                0xC08B or 0xC08F => $"RDWRBSR1 {address & 0x000F:b4}",

                _ => Lookup.TryGetValue(address, out string value) ? value : "*** UNASSIGNED ***",
            };
        }

        public static readonly Dictionary<ushort, string> Lookup = new()
        {
            { 0xC000, "KBD / CLR80STORE" },
            { KBDSTRB, "KBDSTRB" },

            { RDLCBNK2, "RDLCBNK2" },
            { RDLCRAM, "RDLCRAM" },

            { RDMAINRAM, "RDMAINRAM" },
            { RDCARDRAM, "RDCARDRAM" },
            { RDRAMRD, "RDRAMRD" },

            { WRMAINRAM, "WRMAINRAM" },
            { WRCARDRAM, "WRCARDRAM" },
            { RDRAMWRT, "RDRAMWRT" },

            { SETSLOTCXROM, "SETSLOTCXROM" },
            { SETINTCXROM, "SETINTCXROM" },
            { RDCXROM, "RDCXROM" },

            { CLRALSTKZP, "CLRALSTKZP" },
            { SETALTSTKZP, "SETALTSTKZP" },
            { RDALTSTKZP, "RDALTSTKZP" },

            { SETINTC3ROM, "SETINTC3ROM" },
            { SETSLOTC3ROM, "SETSLOTC3ROM" },
            { RDC3ROM, "RDC3ROM" },

            { CLRALTCHAR, "CLRALTCHAR" },
            { SETALTCHAR, "SETALTCHAR" },
            { RDALTCHR, "RDALTCHR" },

            { CLR80COL, "CLR80COL" },
            { SET80COL, "SET80COL" },
            { RD80COL, "RD80COL" },

            // CLR80STORE see above
            { SET80STORE, "SET80STORE" },
            { RD80STORE, "RD80STORE" },

            { TXTPAGE1, "TXTPAGE1" },
            { TXTPAGE2, "TXTPAGE2" },
            { RDPAGE2, "RDPAGE2" },

            { TXTCLR, "TXTCLR" },
            { TXTSET, "TXTSET" },
            { RDTEXT, "RDTEXT" },

            { MIXCLR, "MIXCLR" },
            { MIXSET, "MIXSET" },
            { RDMIXED, "RDMIXED" },

            { LORES, "LORES" },
            { HIRES, "HIRES" },
            { RDHIRES, "RDHIRES" },

            { IOUDISON, "IOUDISON / RDIOUDIS" },
            { IOUDISOFF, "IOUDISOFF / RDDHIRES" },

            { DHIRESON, "DHIRESON / CLRAN3" },
            { DHIRESOFF, "DHIRESOFF / SETAN3" },
            // RDDHIRES see above

            { RDVBLBAR, "RDVBLBAR" },

            { CLRAN0, "CLRAN0" },
            { SETAN0, "SETAN0" },
            { CLRAN1, "CLRAN1" },
            { SETAN1, "SETAN1" },
            { CLRAN2, "CLRAN2" },
            { SETAN2, "SETAN2" },
            // CLRAN3 see above
            // SETAN3 see above

            { SPKR, "SPKR" },
            { TAPEOUT, "TAPEOUT" },
            { TAPEIN, "TAPEIN" },
            { STROBE, "STROBE" },

            { 0xC061, "BUTTON0 / OPENAPPLE" },
            { 0xC062, "BUTTON1 / SOLIDAPPLE" },
            { 0xC063, "BUTTON2 / SHIFT" },

            { PADDLE0, "PADDLE0" },
            { PADDLE1, "PADDLE1" },
            { PADDLE2, "PADDLE2" },
            { PADDLE3, "PADDLE3" },
            { PTRIG, "PTRIG" },
        };
    }
}
