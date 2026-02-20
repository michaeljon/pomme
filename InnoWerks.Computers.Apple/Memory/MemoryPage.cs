using System;
using System.Net.NetworkInformation;

namespace InnoWerks.Computers.Apple
{
    public enum MemoryPageType
    {
        Undefined,

        Ram,

        Rom,

        CardRom,

        LanguageCard
    }

    public class MemoryPage
    {
        public const int PageSize = 256;

#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] Block { get; init; }
#pragma warning restore CA1819 // Properties should not return arrays

        public MemoryPageType MemoryPageType { get; init; }

        public string Description { get; init; }

        public byte PageNumber { get; init; }

        private static readonly byte[] zeros = new byte[PageSize];
        private static readonly byte[] ffs = new byte[PageSize];

        private static readonly MemoryPage zeroValuePage = new(MemoryPageType.Undefined, "0x00", 0x00);
        private static readonly MemoryPage ffValuePage = new(MemoryPageType.Undefined, "0xff", 0x00);

        static MemoryPage()
        {
            for (var i = 0; i < PageSize; i++)
            {
                zeros[i] = 0;
                ffs[i] = 0;
            }

            Array.Copy(ffs, zeroValuePage.Block, PageSize);
            Array.Copy(ffs, ffValuePage.Block, PageSize);
        }

        public MemoryPage(MemoryPageType memoryPageType, string description, byte pageNumber)
        {
            Block = new byte[PageSize];

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
            Array.Copy(zeros, Block, PageSize);
        }

        public static MemoryPage Zeros(MemoryPageType memoryPageType, byte pageNumber)
        {
            return zeroValuePage;
        }

        public static MemoryPage FFs(MemoryPageType memoryPageType, byte pageNumber)
        {
            return ffValuePage;
        }
    }
}
