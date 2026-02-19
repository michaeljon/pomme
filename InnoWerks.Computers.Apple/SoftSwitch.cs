namespace InnoWerks.Computers.Apple
{
    public enum SoftSwitch
    {
        // Keyboard & system
        Speaker,
        TapeOut,
        TapeIn,
        GameStrobe,

        Annunciator0,
        Annunciator1,
        Annunciator2,
        Annunciator3,

        Button0,
        Button1,
        Button2,

        // for the IIe only, these are shared
        OpenApple = Button0,
        SolidApple = Button1,
        ShiftKey = Button2,

        Paddle0,
        Paddle1,
        Paddle2,
        Paddle3,

        // Video
        TextMode,
        MixedMode,
        Page2,
        HiRes,
        DoubleHiRes,
        IOUDisabled,

        // 80 column / aux
        Store80,
        AuxRead,
        AuxWrite,
        ZpAux,
        EightyColumnMode,
        AltCharSet,

        NotVerticalBlank,

        // ROM & slot control
        IntCxRomEnabled,
        SlotC3RomEnabled,
        IntC8RomEnabled,

        LcBank2,
        LcReadEnabled,
        LcWriteEnabled,
    }
}
