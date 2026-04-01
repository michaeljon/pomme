using System.IO;
using InnoWerks.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class Harte6502 : HarteBase
    {
        protected override string BasePath => Path.Join(TestRoot, "65x02/6502/v1");

        protected override CpuClass CpuClass => CpuClass.WDC6502;

        [Ignore]
        [TestMethod]
        public void RunAll6502Tests()
        {
            RunBatched(false);
        }

        [TestMethod]
        public void RunSampled6502Tests()
        {
            RunBatched(true);
        }

        [TestMethod]
        public void RunNamed6502Test()
        {
            RunNamedTest("fe d0 c6");
        }

        [TestMethod]
        public void RunIndividual6502Test00()
        {
            RunNamedBatch("00");
        }

        [TestMethod]
        public void RunIndividual6502Test01()
        {
            RunNamedBatch("01");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test02()
        {
            RunNamedBatch("02");
        }

        [TestMethod]
        public void RunIndividual6502Test03()
        {
            RunNamedBatch("03");
        }

        [TestMethod]
        public void RunIndividual6502Test04()
        {
            RunNamedBatch("04");
        }

        [TestMethod]
        public void RunIndividual6502Test05()
        {
            RunNamedBatch("05");
        }

        [TestMethod]
        public void RunIndividual6502Test06()
        {
            RunNamedBatch("06");
        }

        [TestMethod]
        public void RunIndividual6502Test07()
        {
            RunNamedBatch("07");
        }

        [TestMethod]
        public void RunIndividual6502Test08()
        {
            RunNamedBatch("08");
        }

        [TestMethod]
        public void RunIndividual6502Test09()
        {
            RunNamedBatch("09");
        }

        [TestMethod]
        public void RunIndividual6502Test0A()
        {
            RunNamedBatch("0a");
        }

        [TestMethod]
        public void RunIndividual6502Test0B()
        {
            RunNamedBatch("0b");
        }

        [TestMethod]
        public void RunIndividual6502Test0C()
        {
            RunNamedBatch("0c");
        }

        [TestMethod]
        public void RunIndividual6502Test0D()
        {
            RunNamedBatch("0d");
        }

        [TestMethod]
        public void RunIndividual6502Test0E()
        {
            RunNamedBatch("0e");
        }

        [TestMethod]
        public void RunIndividual6502Test0F()
        {
            RunNamedBatch("0f");
        }

        [TestMethod]
        public void RunIndividual6502Test10()
        {
            RunNamedBatch("10");
        }

        [TestMethod]
        public void RunIndividual6502Test11()
        {
            RunNamedBatch("11");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test12()
        {
            RunNamedBatch("12");
        }

        [TestMethod]
        public void RunIndividual6502Test13()
        {
            RunNamedBatch("13");
        }

        [TestMethod]
        public void RunIndividual6502Test14()
        {
            RunNamedBatch("14");
        }

        [TestMethod]
        public void RunIndividual6502Test15()
        {
            RunNamedBatch("15");
        }

        [TestMethod]
        public void RunIndividual6502Test16()
        {
            RunNamedBatch("16");
        }

        [TestMethod]
        public void RunIndividual6502Test17()
        {
            RunNamedBatch("17");
        }

        [TestMethod]
        public void RunIndividual6502Test18()
        {
            RunNamedBatch("18");
        }

        [TestMethod]
        public void RunIndividual6502Test19()
        {
            RunNamedBatch("19");
        }

        [TestMethod]
        public void RunIndividual6502Test1A()
        {
            RunNamedBatch("1a");
        }

        [TestMethod]
        public void RunIndividual6502Test1B()
        {
            RunNamedBatch("1b");
        }

        [TestMethod]
        public void RunIndividual6502Test1C()
        {
            RunNamedBatch("1c");
        }

        [TestMethod]
        public void RunIndividual6502Test1D()
        {
            RunNamedBatch("1d");
        }

        [TestMethod]
        public void RunIndividual6502Test1E()
        {
            RunNamedBatch("1e");
        }

        [TestMethod]
        public void RunIndividual6502Test1F()
        {
            RunNamedBatch("1f");
        }

        [TestMethod]
        public void RunIndividual6502Test20()
        {
            RunNamedBatch("20");
        }

        [TestMethod]
        public void RunIndividual6502Test21()
        {
            RunNamedBatch("21");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test22()
        {
            RunNamedBatch("22");
        }

        [TestMethod]
        public void RunIndividual6502Test23()
        {
            RunNamedBatch("23");
        }

        [TestMethod]
        public void RunIndividual6502Test24()
        {
            RunNamedBatch("24");
        }

        [TestMethod]
        public void RunIndividual6502Test25()
        {
            RunNamedBatch("25");
        }

        [TestMethod]
        public void RunIndividual6502Test26()
        {
            RunNamedBatch("26");
        }

        [TestMethod]
        public void RunIndividual6502Test27()
        {
            RunNamedBatch("27");
        }

        [TestMethod]
        public void RunIndividual6502Test28()
        {
            RunNamedBatch("28");
        }

        [TestMethod]
        public void RunIndividual6502Test29()
        {
            RunNamedBatch("29");
        }

        [TestMethod]
        public void RunIndividual6502Test2A()
        {
            RunNamedBatch("2a");
        }

        [TestMethod]
        public void RunIndividual6502Test2B()
        {
            RunNamedBatch("2b");
        }

        [TestMethod]
        public void RunIndividual6502Test2C()
        {
            RunNamedBatch("2c");
        }

        [TestMethod]
        public void RunIndividual6502Test2D()
        {
            RunNamedBatch("2d");
        }

        [TestMethod]
        public void RunIndividual6502Test2E()
        {
            RunNamedBatch("2e");
        }

        [TestMethod]
        public void RunIndividual6502Test2F()
        {
            RunNamedBatch("2f");
        }

        [TestMethod]
        public void RunIndividual6502Test30()
        {
            RunNamedBatch("30");
        }

        [TestMethod]
        public void RunIndividual6502Test31()
        {
            RunNamedBatch("31");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test32()
        {
            RunNamedBatch("32");
        }

        [TestMethod]
        public void RunIndividual6502Test33()
        {
            RunNamedBatch("33");
        }

        [TestMethod]
        public void RunIndividual6502Test34()
        {
            RunNamedBatch("34");
        }

        [TestMethod]
        public void RunIndividual6502Test35()
        {
            RunNamedBatch("35");
        }

        [TestMethod]
        public void RunIndividual6502Test36()
        {
            RunNamedBatch("36");
        }

        [TestMethod]
        public void RunIndividual6502Test37()
        {
            RunNamedBatch("37");
        }

        [TestMethod]
        public void RunIndividual6502Test38()
        {
            RunNamedBatch("38");
        }

        [TestMethod]
        public void RunIndividual6502Test39()
        {
            RunNamedBatch("39");
        }

        [TestMethod]
        public void RunIndividual6502Test3A()
        {
            RunNamedBatch("3a");
        }

        [TestMethod]
        public void RunIndividual6502Test3B()
        {
            RunNamedBatch("3b");
        }

        [TestMethod]
        public void RunIndividual6502Test3C()
        {
            RunNamedBatch("3c");
        }

        [TestMethod]
        public void RunIndividual6502Test3D()
        {
            RunNamedBatch("3d");
        }

        [TestMethod]
        public void RunIndividual6502Test3E()
        {
            RunNamedBatch("3e");
        }

        [TestMethod]
        public void RunIndividual6502Test3F()
        {
            RunNamedBatch("3f");
        }

        [TestMethod]
        public void RunIndividual6502Test40()
        {
            RunNamedBatch("40");
        }

        [TestMethod]
        public void RunIndividual6502Test41()
        {
            RunNamedBatch("41");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test42()
        {
            RunNamedBatch("42");
        }

        [TestMethod]
        public void RunIndividual6502Test43()
        {
            RunNamedBatch("43");
        }

        [TestMethod]
        public void RunIndividual6502Test44()
        {
            RunNamedBatch("44");
        }

        [TestMethod]
        public void RunIndividual6502Test45()
        {
            RunNamedBatch("45");
        }

        [TestMethod]
        public void RunIndividual6502Test46()
        {
            RunNamedBatch("46");
        }

        [TestMethod]
        public void RunIndividual6502Test47()
        {
            RunNamedBatch("47");
        }

        [TestMethod]
        public void RunIndividual6502Test48()
        {
            RunNamedBatch("48");
        }

        [TestMethod]
        public void RunIndividual6502Test49()
        {
            RunNamedBatch("49");
        }

        [TestMethod]
        public void RunIndividual6502Test4A()
        {
            RunNamedBatch("4a");
        }

        [TestMethod]
        public void RunIndividual6502Test4B()
        {
            RunNamedBatch("4b");
        }

        [TestMethod]
        public void RunIndividual6502Test4C()
        {
            RunNamedBatch("4c");
        }

        [TestMethod]
        public void RunIndividual6502Test4D()
        {
            RunNamedBatch("4d");
        }

        [TestMethod]
        public void RunIndividual6502Test4E()
        {
            RunNamedBatch("4e");
        }

        [TestMethod]
        public void RunIndividual6502Test4F()
        {
            RunNamedBatch("4f");
        }

        [TestMethod]
        public void RunIndividual6502Test50()
        {
            RunNamedBatch("50");
        }

        [TestMethod]
        public void RunIndividual6502Test51()
        {
            RunNamedBatch("51");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test52()
        {
            RunNamedBatch("52");
        }

        [TestMethod]
        public void RunIndividual6502Test53()
        {
            RunNamedBatch("53");
        }

        [TestMethod]
        public void RunIndividual6502Test54()
        {
            RunNamedBatch("54");
        }

        [TestMethod]
        public void RunIndividual6502Test55()
        {
            RunNamedBatch("55");
        }

        [TestMethod]
        public void RunIndividual6502Test56()
        {
            RunNamedBatch("56");
        }

        [TestMethod]
        public void RunIndividual6502Test57()
        {
            RunNamedBatch("57");
        }

        [TestMethod]
        public void RunIndividual6502Test58()
        {
            RunNamedBatch("58");
        }

        [TestMethod]
        public void RunIndividual6502Test59()
        {
            RunNamedBatch("59");
        }

        [TestMethod]
        public void RunIndividual6502Test5A()
        {
            RunNamedBatch("5a");
        }

        [TestMethod]
        public void RunIndividual6502Test5B()
        {
            RunNamedBatch("5b");
        }

        [TestMethod]
        public void RunIndividual6502Test5C()
        {
            RunNamedBatch("5c");
        }

        [TestMethod]
        public void RunIndividual6502Test5D()
        {
            RunNamedBatch("5d");
        }

        [TestMethod]
        public void RunIndividual6502Test5E()
        {
            RunNamedBatch("5e");
        }

        [TestMethod]
        public void RunIndividual6502Test5F()
        {
            RunNamedBatch("5f");
        }

        [TestMethod]
        public void RunIndividual6502Test60()
        {
            RunNamedBatch("60");
        }

        [TestMethod]
        public void RunIndividual6502Test61()
        {
            RunNamedBatch("61");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test62()
        {
            RunNamedBatch("62");
        }

        [TestMethod]
        public void RunIndividual6502Test63()
        {
            RunNamedBatch("63");
        }

        [TestMethod]
        public void RunIndividual6502Test64()
        {
            RunNamedBatch("64");
        }

        [TestMethod]
        public void RunIndividual6502Test65()
        {
            RunNamedBatch("65");
        }

        [TestMethod]
        public void RunIndividual6502Test66()
        {
            RunNamedBatch("66");
        }

        [TestMethod]
        public void RunIndividual6502Test67()
        {
            RunNamedBatch("67");
        }

        [TestMethod]
        public void RunIndividual6502Test68()
        {
            RunNamedBatch("68");
        }

        [TestMethod]
        public void RunIndividual6502Test69()
        {
            RunNamedBatch("69");
        }

        [TestMethod]
        public void RunIndividual6502Test6A()
        {
            RunNamedBatch("6a");
        }

        [TestMethod]
        public void RunIndividual6502Test6B()
        {
            RunNamedBatch("6b");
        }

        [TestMethod]
        public void RunIndividual6502Test6C()
        {
            RunNamedBatch("6c");
        }

        [TestMethod]
        public void RunIndividual6502Test6D()
        {
            RunNamedBatch("6d");
        }

        [TestMethod]
        public void RunIndividual6502Test6E()
        {
            RunNamedBatch("6e");
        }

        [TestMethod]
        public void RunIndividual6502Test6F()
        {
            RunNamedBatch("6f");
        }

        [TestMethod]
        public void RunIndividual6502Test70()
        {
            RunNamedBatch("70");
        }

        [TestMethod]
        public void RunIndividual6502Test71()
        {
            RunNamedBatch("71");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test72()
        {
            RunNamedBatch("72");
        }

        [TestMethod]
        public void RunIndividual6502Test73()
        {
            RunNamedBatch("73");
        }

        [TestMethod]
        public void RunIndividual6502Test74()
        {
            RunNamedBatch("74");
        }

        [TestMethod]
        public void RunIndividual6502Test75()
        {
            RunNamedBatch("75");
        }

        [TestMethod]
        public void RunIndividual6502Test76()
        {
            RunNamedBatch("76");
        }

        [TestMethod]
        public void RunIndividual6502Test77()
        {
            RunNamedBatch("77");
        }

        [TestMethod]
        public void RunIndividual6502Test78()
        {
            RunNamedBatch("78");
        }

        [TestMethod]
        public void RunIndividual6502Test79()
        {
            RunNamedBatch("79");
        }

        [TestMethod]
        public void RunIndividual6502Test7A()
        {
            RunNamedBatch("7a");
        }

        [TestMethod]
        public void RunIndividual6502Test7B()
        {
            RunNamedBatch("7b");
        }

        [TestMethod]
        public void RunIndividual6502Test7C()
        {
            RunNamedBatch("7c");
        }

        [TestMethod]
        public void RunIndividual6502Test7D()
        {
            RunNamedBatch("7d");
        }

        [TestMethod]
        public void RunIndividual6502Test7E()
        {
            RunNamedBatch("7e");
        }

        [TestMethod]
        public void RunIndividual6502Test7F()
        {
            RunNamedBatch("7f");
        }

        [TestMethod]
        public void RunIndividual6502Test80()
        {
            RunNamedBatch("80");
        }

        [TestMethod]
        public void RunIndividual6502Test81()
        {
            RunNamedBatch("81");
        }

        [TestMethod]
        public void RunIndividual6502Test82()
        {
            RunNamedBatch("82");
        }

        [TestMethod]
        public void RunIndividual6502Test83()
        {
            RunNamedBatch("83");
        }

        [TestMethod]
        public void RunIndividual6502Test84()
        {
            RunNamedBatch("84");
        }

        [TestMethod]
        public void RunIndividual6502Test85()
        {
            RunNamedBatch("85");
        }

        [TestMethod]
        public void RunIndividual6502Test86()
        {
            RunNamedBatch("86");
        }

        [TestMethod]
        public void RunIndividual6502Test87()
        {
            RunNamedBatch("87");
        }

        [TestMethod]
        public void RunIndividual6502Test88()
        {
            RunNamedBatch("88");
        }

        [TestMethod]
        public void RunIndividual6502Test89()
        {
            RunNamedBatch("89");
        }

        [TestMethod]
        public void RunIndividual6502Test8A()
        {
            RunNamedBatch("8a");
        }

        [TestMethod]
        public void RunIndividual6502Test8B()
        {
            RunNamedBatch("8b");
        }

        [TestMethod]
        public void RunIndividual6502Test8C()
        {
            RunNamedBatch("8c");
        }

        [TestMethod]
        public void RunIndividual6502Test8D()
        {
            RunNamedBatch("8d");
        }

        [TestMethod]
        public void RunIndividual6502Test8E()
        {
            RunNamedBatch("8e");
        }

        [TestMethod]
        public void RunIndividual6502Test8F()
        {
            RunNamedBatch("8f");
        }

        [TestMethod]
        public void RunIndividual6502Test90()
        {
            RunNamedBatch("90");
        }

        [TestMethod]
        public void RunIndividual6502Test91()
        {
            RunNamedBatch("91");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502Test92()
        {
            RunNamedBatch("92");
        }

        [TestMethod]
        public void RunIndividual6502Test93()
        {
            RunNamedBatch("93");
        }

        [TestMethod]
        public void RunIndividual6502Test94()
        {
            RunNamedBatch("94");
        }

        [TestMethod]
        public void RunIndividual6502Test95()
        {
            RunNamedBatch("95");
        }

        [TestMethod]
        public void RunIndividual6502Test96()
        {
            RunNamedBatch("96");
        }

        [TestMethod]
        public void RunIndividual6502Test97()
        {
            RunNamedBatch("97");
        }

        [TestMethod]
        public void RunIndividual6502Test98()
        {
            RunNamedBatch("98");
        }

        [TestMethod]
        public void RunIndividual6502Test99()
        {
            RunNamedBatch("99");
        }

        [TestMethod]
        public void RunIndividual6502Test9A()
        {
            RunNamedBatch("9a");
        }

        [TestMethod]
        public void RunIndividual6502Test9B()
        {
            RunNamedBatch("9b");
        }

        [TestMethod]
        public void RunIndividual6502Test9C()
        {
            RunNamedBatch("9c");
        }

        [TestMethod]
        public void RunIndividual6502Test9D()
        {
            RunNamedBatch("9d");
        }

        [TestMethod]
        public void RunIndividual6502Test9E()
        {
            RunNamedBatch("9e");
        }

        [TestMethod]
        public void RunIndividual6502Test9F()
        {
            RunNamedBatch("9f");
        }

        [TestMethod]
        public void RunIndividual6502TestA0()
        {
            RunNamedBatch("a0");
        }

        [TestMethod]
        public void RunIndividual6502TestA1()
        {
            RunNamedBatch("a1");
        }

        [TestMethod]
        public void RunIndividual6502TestA2()
        {
            RunNamedBatch("a2");
        }

        [TestMethod]
        public void RunIndividual6502TestA3()
        {
            RunNamedBatch("a3");
        }

        [TestMethod]
        public void RunIndividual6502TestA4()
        {
            RunNamedBatch("a4");
        }

        [TestMethod]
        public void RunIndividual6502TestA5()
        {
            RunNamedBatch("a5");
        }

        [TestMethod]
        public void RunIndividual6502TestA6()
        {
            RunNamedBatch("a6");
        }

        [TestMethod]
        public void RunIndividual6502TestA7()
        {
            RunNamedBatch("a7");
        }

        [TestMethod]
        public void RunIndividual6502TestA8()
        {
            RunNamedBatch("a8");
        }

        [TestMethod]
        public void RunIndividual6502TestA9()
        {
            RunNamedBatch("a9");
        }

        [TestMethod]
        public void RunIndividual6502TestAA()
        {
            RunNamedBatch("aa");
        }

        [TestMethod]
        public void RunIndividual6502TestAB()
        {
            RunNamedBatch("ab");
        }

        [TestMethod]
        public void RunIndividual6502TestAC()
        {
            RunNamedBatch("ac");
        }

        [TestMethod]
        public void RunIndividual6502TestAD()
        {
            RunNamedBatch("ad");
        }

        [TestMethod]
        public void RunIndividual6502TestAE()
        {
            RunNamedBatch("ae");
        }

        [TestMethod]
        public void RunIndividual6502TestAF()
        {
            RunNamedBatch("af");
        }

        [TestMethod]
        public void RunIndividual6502TestB0()
        {
            RunNamedBatch("b0");
        }

        [TestMethod]
        public void RunIndividual6502TestB1()
        {
            RunNamedBatch("b1");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502TestB2()
        {
            RunNamedBatch("b2");
        }

        [TestMethod]
        public void RunIndividual6502TestB3()
        {
            RunNamedBatch("b3");
        }

        [TestMethod]
        public void RunIndividual6502TestB4()
        {
            RunNamedBatch("b4");
        }

        [TestMethod]
        public void RunIndividual6502TestB5()
        {
            RunNamedBatch("b5");
        }

        [TestMethod]
        public void RunIndividual6502TestB6()
        {
            RunNamedBatch("b6");
        }

        [TestMethod]
        public void RunIndividual6502TestB7()
        {
            RunNamedBatch("b7");
        }

        [TestMethod]
        public void RunIndividual6502TestB8()
        {
            RunNamedBatch("b8");
        }

        [TestMethod]
        public void RunIndividual6502TestB9()
        {
            RunNamedBatch("b9");
        }

        [TestMethod]
        public void RunIndividual6502TestBA()
        {
            RunNamedBatch("ba");
        }

        [TestMethod]
        public void RunIndividual6502TestBB()
        {
            RunNamedBatch("bb");
        }

        [TestMethod]
        public void RunIndividual6502TestBC()
        {
            RunNamedBatch("bc");
        }

        [TestMethod]
        public void RunIndividual6502TestBD()
        {
            RunNamedBatch("bd");
        }

        [TestMethod]
        public void RunIndividual6502TestBE()
        {
            RunNamedBatch("be");
        }

        [TestMethod]
        public void RunIndividual6502TestBF()
        {
            RunNamedBatch("bf");
        }

        [TestMethod]
        public void RunIndividual6502TestC0()
        {
            RunNamedBatch("c0");
        }

        [TestMethod]
        public void RunIndividual6502TestC1()
        {
            RunNamedBatch("c1");
        }

        [TestMethod]
        public void RunIndividual6502TestC2()
        {
            RunNamedBatch("c2");
        }

        [TestMethod]
        public void RunIndividual6502TestC3()
        {
            RunNamedBatch("c3");
        }

        [TestMethod]
        public void RunIndividual6502TestC4()
        {
            RunNamedBatch("c4");
        }

        [TestMethod]
        public void RunIndividual6502TestC5()
        {
            RunNamedBatch("c5");
        }

        [TestMethod]
        public void RunIndividual6502TestC6()
        {
            RunNamedBatch("c6");
        }

        [TestMethod]
        public void RunIndividual6502TestC7()
        {
            RunNamedBatch("c7");
        }

        [TestMethod]
        public void RunIndividual6502TestC8()
        {
            RunNamedBatch("c8");
        }

        [TestMethod]
        public void RunIndividual6502TestC9()
        {
            RunNamedBatch("c9");
        }

        [TestMethod]
        public void RunIndividual6502TestCA()
        {
            RunNamedBatch("ca");
        }

        [TestMethod]
        public void RunIndividual6502TestCB()
        {
            RunNamedBatch("cb");
        }

        [TestMethod]
        public void RunIndividual6502TestCC()
        {
            RunNamedBatch("cc");
        }

        [TestMethod]
        public void RunIndividual6502TestCD()
        {
            RunNamedBatch("cd");
        }

        [TestMethod]
        public void RunIndividual6502TestCE()
        {
            RunNamedBatch("ce");
        }

        [TestMethod]
        public void RunIndividual6502TestCF()
        {
            RunNamedBatch("cf");
        }

        [TestMethod]
        public void RunIndividual6502TestD0()
        {
            RunNamedBatch("d0");
        }

        [TestMethod]
        public void RunIndividual6502TestD1()
        {
            RunNamedBatch("d1");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502TestD2()
        {
            RunNamedBatch("d2");
        }

        [TestMethod]
        public void RunIndividual6502TestD3()
        {
            RunNamedBatch("d3");
        }

        [TestMethod]
        public void RunIndividual6502TestD4()
        {
            RunNamedBatch("d4");
        }

        [TestMethod]
        public void RunIndividual6502TestD5()
        {
            RunNamedBatch("d5");
        }

        [TestMethod]
        public void RunIndividual6502TestD6()
        {
            RunNamedBatch("d6");
        }

        [TestMethod]
        public void RunIndividual6502TestD7()
        {
            RunNamedBatch("d7");
        }

        [TestMethod]
        public void RunIndividual6502TestD8()
        {
            RunNamedBatch("d8");
        }

        [TestMethod]
        public void RunIndividual6502TestD9()
        {
            RunNamedBatch("d9");
        }

        [TestMethod]
        public void RunIndividual6502TestDA()
        {
            RunNamedBatch("da");
        }

        [TestMethod]
        public void RunIndividual6502TestDB()
        {
            RunNamedBatch("db");
        }

        [TestMethod]
        public void RunIndividual6502TestDC()
        {
            RunNamedBatch("dc");
        }

        [TestMethod]
        public void RunIndividual6502TestDD()
        {
            RunNamedBatch("dd");
        }

        [TestMethod]
        public void RunIndividual6502TestDE()
        {
            RunNamedBatch("de");
        }

        [TestMethod]
        public void RunIndividual6502TestDF()
        {
            RunNamedBatch("df");
        }

        [TestMethod]
        public void RunIndividual6502TestE0()
        {
            RunNamedBatch("e0");
        }

        [TestMethod]
        public void RunIndividual6502TestE1()
        {
            RunNamedBatch("e1");
        }

        [TestMethod]
        public void RunIndividual6502TestE2()
        {
            RunNamedBatch("e2");
        }

        [TestMethod]
        public void RunIndividual6502TestE3()
        {
            RunNamedBatch("e3");
        }

        [TestMethod]
        public void RunIndividual6502TestE4()
        {
            RunNamedBatch("e4");
        }

        [TestMethod]
        public void RunIndividual6502TestE5()
        {
            RunNamedBatch("e5");
        }

        [TestMethod]
        public void RunIndividual6502TestE6()
        {
            RunNamedBatch("e6");
        }

        [TestMethod]
        public void RunIndividual6502TestE7()
        {
            RunNamedBatch("e7");
        }

        [TestMethod]
        public void RunIndividual6502TestE8()
        {
            RunNamedBatch("e8");
        }

        [TestMethod]
        public void RunIndividual6502TestE9()
        {
            RunNamedBatch("e9");
        }

        [TestMethod]
        public void RunIndividual6502TestEA()
        {
            RunNamedBatch("ea");
        }

        [TestMethod]
        public void RunIndividual6502TestEB()
        {
            RunNamedBatch("eb");
        }

        [TestMethod]
        public void RunIndividual6502TestEC()
        {
            RunNamedBatch("ec");
        }

        [TestMethod]
        public void RunIndividual6502TestED()
        {
            RunNamedBatch("ed");
        }

        [TestMethod]
        public void RunIndividual6502TestEE()
        {
            RunNamedBatch("ee");
        }

        [TestMethod]
        public void RunIndividual6502TestEF()
        {
            RunNamedBatch("ef");
        }

        [TestMethod]
        public void RunIndividual6502TestF0()
        {
            RunNamedBatch("f0");
        }

        [TestMethod]
        public void RunIndividual6502TestF1()
        {
            RunNamedBatch("f1");
        }

        [TestMethod]
        [ExpectedException(typeof(IllegalOpCodeException))]
        public void RunIndividual6502TestF2()
        {
            RunNamedBatch("f2");
        }

        [TestMethod]
        public void RunIndividual6502TestF3()
        {
            RunNamedBatch("f3");
        }

        [TestMethod]
        public void RunIndividual6502TestF4()
        {
            RunNamedBatch("f4");
        }

        [TestMethod]
        public void RunIndividual6502TestF5()
        {
            RunNamedBatch("f5");
        }

        [TestMethod]
        public void RunIndividual6502TestF6()
        {
            RunNamedBatch("f6");
        }

        [TestMethod]
        public void RunIndividual6502TestF7()
        {
            RunNamedBatch("f7");
        }

        [TestMethod]
        public void RunIndividual6502TestF8()
        {
            RunNamedBatch("f8");
        }

        [TestMethod]
        public void RunIndividual6502TestF9()
        {
            RunNamedBatch("f9");
        }

        [TestMethod]
        public void RunIndividual6502TestFA()
        {
            RunNamedBatch("fa");
        }

        [TestMethod]
        public void RunIndividual6502TestFB()
        {
            RunNamedBatch("fb");
        }

        [TestMethod]
        public void RunIndividual6502TestFC()
        {
            RunNamedBatch("fc");
        }

        [TestMethod]
        public void RunIndividual6502TestFD()
        {
            RunNamedBatch("fd");
        }

        [TestMethod]
        public void RunIndividual6502TestFE()
        {
            RunNamedBatch("fe");
        }

        [TestMethod]
        public void RunIndividual6502TestFF()
        {
            RunNamedBatch("ff");
        }
    }
}
