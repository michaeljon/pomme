using System;

namespace InnoWerks.Computers.Apple
{
    public enum MemoryPageType
    {
        ReadOnly,

        Ram,

        Rom,

        CardRom,

        LanguageCard
    }

#pragma warning disable CA1819 // Properties should not return arrays

    public class MemoryPage
    {
        public const int PageSize = 256;

        public byte[] Block { get; set; }

        public MemoryPageType MemoryPageType { get; init; }

        public string Description { get; init; }

        public byte PageNumber { get; init; }

        public int Slot { get; set; }

        public MemoryPage(MemoryPageType memoryPageType, string description, byte pageNumber, byte fill = 0x00)
        {
            Block = new byte[PageSize];
            Array.Fill(Block, fill);

            MemoryPageType = memoryPageType;
            Description = description;
            PageNumber = pageNumber;
        }

        public override string ToString()
        {
            return $"{MemoryPageType} {Description} at ${PageNumber:X2} ({PageNumber})";
        }

        public void ZeroOut()
        {
            Array.Fill(Block, (byte)0x00);
        }
    }
}
