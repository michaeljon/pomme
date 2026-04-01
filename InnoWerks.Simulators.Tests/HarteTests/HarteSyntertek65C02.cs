using System.IO;
using InnoWerks.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class HarteSynertek65C02 : HarteBase
    {
        protected override string BasePath => Path.Join(TestRoot, "65x02/synertek65c02/v1");

        protected override CpuClass CpuClass => CpuClass.Synertek65C02;

        [Ignore]
        [TestMethod]
        public void RunAllSynertek65C02Tests()
        {
            RunBatched(false);
        }

        [TestMethod]
        public void RunSampledSynertek65C02Tests()
        {
            RunBatched(true);
        }

        [TestMethod]
        public void RunNamedSynertek65C02Test()
        {
            RunNamedTest("cb 4a 20");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test00()
        {
            RunNamedBatch("00");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test01()
        {
            RunNamedBatch("01");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test02()
        {
            RunNamedBatch("02");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test03()
        {
            RunNamedBatch("03");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test04()
        {
            RunNamedBatch("04");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test05()
        {
            RunNamedBatch("05");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test06()
        {
            RunNamedBatch("06");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test07()
        {
            RunNamedBatch("07");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test08()
        {
            RunNamedBatch("08");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test09()
        {
            RunNamedBatch("09");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test0A()
        {
            RunNamedBatch("0a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test0B()
        {
            RunNamedBatch("0b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test0C()
        {
            RunNamedBatch("0c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test0D()
        {
            RunNamedBatch("0d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test0E()
        {
            RunNamedBatch("0e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test0F()
        {
            RunNamedBatch("0f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test10()
        {
            RunNamedBatch("10");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test11()
        {
            RunNamedBatch("11");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test12()
        {
            RunNamedBatch("12");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test13()
        {
            RunNamedBatch("13");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test14()
        {
            RunNamedBatch("14");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test15()
        {
            RunNamedBatch("15");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test16()
        {
            RunNamedBatch("16");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test17()
        {
            RunNamedBatch("17");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test18()
        {
            RunNamedBatch("18");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test19()
        {
            RunNamedBatch("19");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test1A()
        {
            RunNamedBatch("1a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test1B()
        {
            RunNamedBatch("1b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test1C()
        {
            RunNamedBatch("1c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test1D()
        {
            RunNamedBatch("1d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test1E()
        {
            RunNamedBatch("1e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test1F()
        {
            RunNamedBatch("1f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test20()
        {
            RunNamedBatch("20");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test21()
        {
            RunNamedBatch("21");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test22()
        {
            RunNamedBatch("22");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test23()
        {
            RunNamedBatch("23");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test24()
        {
            RunNamedBatch("24");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test25()
        {
            RunNamedBatch("25");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test26()
        {
            RunNamedBatch("26");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test27()
        {
            RunNamedBatch("27");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test28()
        {
            RunNamedBatch("28");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test29()
        {
            RunNamedBatch("29");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test2A()
        {
            RunNamedBatch("2a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test2B()
        {
            RunNamedBatch("2b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test2C()
        {
            RunNamedBatch("2c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test2D()
        {
            RunNamedBatch("2d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test2E()
        {
            RunNamedBatch("2e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test2F()
        {
            RunNamedBatch("2f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test30()
        {
            RunNamedBatch("30");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test31()
        {
            RunNamedBatch("31");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test32()
        {
            RunNamedBatch("32");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test33()
        {
            RunNamedBatch("33");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test34()
        {
            RunNamedBatch("34");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test35()
        {
            RunNamedBatch("35");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test36()
        {
            RunNamedBatch("36");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test37()
        {
            RunNamedBatch("37");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test38()
        {
            RunNamedBatch("38");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test39()
        {
            RunNamedBatch("39");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test3A()
        {
            RunNamedBatch("3a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test3B()
        {
            RunNamedBatch("3b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test3C()
        {
            RunNamedBatch("3c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test3D()
        {
            RunNamedBatch("3d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test3E()
        {
            RunNamedBatch("3e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test3F()
        {
            RunNamedBatch("3f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test40()
        {
            RunNamedBatch("40");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test41()
        {
            RunNamedBatch("41");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test42()
        {
            RunNamedBatch("42");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test43()
        {
            RunNamedBatch("43");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test44()
        {
            RunNamedBatch("44");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test45()
        {
            RunNamedBatch("45");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test46()
        {
            RunNamedBatch("46");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test47()
        {
            RunNamedBatch("47");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test48()
        {
            RunNamedBatch("48");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test49()
        {
            RunNamedBatch("49");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test4A()
        {
            RunNamedBatch("4a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test4B()
        {
            RunNamedBatch("4b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test4C()
        {
            RunNamedBatch("4c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test4D()
        {
            RunNamedBatch("4d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test4E()
        {
            RunNamedBatch("4e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test4F()
        {
            RunNamedBatch("4f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test50()
        {
            RunNamedBatch("50");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test51()
        {
            RunNamedBatch("51");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test52()
        {
            RunNamedBatch("52");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test53()
        {
            RunNamedBatch("53");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test54()
        {
            RunNamedBatch("54");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test55()
        {
            RunNamedBatch("55");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test56()
        {
            RunNamedBatch("56");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test57()
        {
            RunNamedBatch("57");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test58()
        {
            RunNamedBatch("58");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test59()
        {
            RunNamedBatch("59");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test5A()
        {
            RunNamedBatch("5a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test5B()
        {
            RunNamedBatch("5b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test5C()
        {
            RunNamedBatch("5c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test5D()
        {
            RunNamedBatch("5d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test5E()
        {
            RunNamedBatch("5e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test5F()
        {
            RunNamedBatch("5f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test60()
        {
            RunNamedBatch("60");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test61()
        {
            RunNamedBatch("61");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test62()
        {
            RunNamedBatch("62");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test63()
        {
            RunNamedBatch("63");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test64()
        {
            RunNamedBatch("64");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test65()
        {
            RunNamedBatch("65");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test66()
        {
            RunNamedBatch("66");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test67()
        {
            RunNamedBatch("67");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test68()
        {
            RunNamedBatch("68");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test69()
        {
            RunNamedBatch("69");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test6A()
        {
            RunNamedBatch("6a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test6B()
        {
            RunNamedBatch("6b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test6C()
        {
            RunNamedBatch("6c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test6D()
        {
            RunNamedBatch("6d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test6E()
        {
            RunNamedBatch("6e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test6F()
        {
            RunNamedBatch("6f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test70()
        {
            RunNamedBatch("70");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test71()
        {
            RunNamedBatch("71");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test72()
        {
            RunNamedBatch("72");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test73()
        {
            RunNamedBatch("73");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test74()
        {
            RunNamedBatch("74");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test75()
        {
            RunNamedBatch("75");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test76()
        {
            RunNamedBatch("76");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test77()
        {
            RunNamedBatch("77");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test78()
        {
            RunNamedBatch("78");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test79()
        {
            RunNamedBatch("79");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test7A()
        {
            RunNamedBatch("7a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test7B()
        {
            RunNamedBatch("7b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test7C()
        {
            RunNamedBatch("7c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test7D()
        {
            RunNamedBatch("7d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test7E()
        {
            RunNamedBatch("7e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test7F()
        {
            RunNamedBatch("7f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test80()
        {
            RunNamedBatch("80");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test81()
        {
            RunNamedBatch("81");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test82()
        {
            RunNamedBatch("82");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test83()
        {
            RunNamedBatch("83");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test84()
        {
            RunNamedBatch("84");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test85()
        {
            RunNamedBatch("85");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test86()
        {
            RunNamedBatch("86");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test87()
        {
            RunNamedBatch("87");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test88()
        {
            RunNamedBatch("88");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test89()
        {
            RunNamedBatch("89");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test8A()
        {
            RunNamedBatch("8a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test8B()
        {
            RunNamedBatch("8b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test8C()
        {
            RunNamedBatch("8c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test8D()
        {
            RunNamedBatch("8d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test8E()
        {
            RunNamedBatch("8e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test8F()
        {
            RunNamedBatch("8f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test90()
        {
            RunNamedBatch("90");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test91()
        {
            RunNamedBatch("91");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test92()
        {
            RunNamedBatch("92");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test93()
        {
            RunNamedBatch("93");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test94()
        {
            RunNamedBatch("94");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test95()
        {
            RunNamedBatch("95");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test96()
        {
            RunNamedBatch("96");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test97()
        {
            RunNamedBatch("97");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test98()
        {
            RunNamedBatch("98");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test99()
        {
            RunNamedBatch("99");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test9A()
        {
            RunNamedBatch("9a");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test9B()
        {
            RunNamedBatch("9b");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test9C()
        {
            RunNamedBatch("9c");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test9D()
        {
            RunNamedBatch("9d");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test9E()
        {
            RunNamedBatch("9e");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02Test9F()
        {
            RunNamedBatch("9f");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA0()
        {
            RunNamedBatch("a0");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA1()
        {
            RunNamedBatch("a1");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA2()
        {
            RunNamedBatch("a2");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA3()
        {
            RunNamedBatch("a3");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA4()
        {
            RunNamedBatch("a4");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA5()
        {
            RunNamedBatch("a5");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA6()
        {
            RunNamedBatch("a6");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA7()
        {
            RunNamedBatch("a7");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA8()
        {
            RunNamedBatch("a8");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestA9()
        {
            RunNamedBatch("a9");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestAA()
        {
            RunNamedBatch("aa");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestAB()
        {
            RunNamedBatch("ab");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestAC()
        {
            RunNamedBatch("ac");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestAD()
        {
            RunNamedBatch("ad");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestAE()
        {
            RunNamedBatch("ae");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestAF()
        {
            RunNamedBatch("af");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB0()
        {
            RunNamedBatch("b0");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB1()
        {
            RunNamedBatch("b1");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB2()
        {
            RunNamedBatch("b2");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB3()
        {
            RunNamedBatch("b3");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB4()
        {
            RunNamedBatch("b4");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB5()
        {
            RunNamedBatch("b5");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB6()
        {
            RunNamedBatch("b6");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB7()
        {
            RunNamedBatch("b7");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB8()
        {
            RunNamedBatch("b8");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestB9()
        {
            RunNamedBatch("b9");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestBA()
        {
            RunNamedBatch("ba");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestBB()
        {
            RunNamedBatch("bb");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestBC()
        {
            RunNamedBatch("bc");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestBD()
        {
            RunNamedBatch("bd");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestBE()
        {
            RunNamedBatch("be");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestBF()
        {
            RunNamedBatch("bf");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC0()
        {
            RunNamedBatch("c0");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC1()
        {
            RunNamedBatch("c1");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC2()
        {
            RunNamedBatch("c2");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC3()
        {
            RunNamedBatch("c3");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC4()
        {
            RunNamedBatch("c4");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC5()
        {
            RunNamedBatch("c5");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC6()
        {
            RunNamedBatch("c6");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC7()
        {
            RunNamedBatch("c7");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC8()
        {
            RunNamedBatch("c8");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestC9()
        {
            RunNamedBatch("c9");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestCA()
        {
            RunNamedBatch("ca");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestCB()
        {
            RunNamedBatch("cb");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestCC()
        {
            RunNamedBatch("cc");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestCD()
        {
            RunNamedBatch("cd");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestCE()
        {
            RunNamedBatch("ce");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestCF()
        {
            RunNamedBatch("cf");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD0()
        {
            RunNamedBatch("d0");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD1()
        {
            RunNamedBatch("d1");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD2()
        {
            RunNamedBatch("d2");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD3()
        {
            RunNamedBatch("d3");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD4()
        {
            RunNamedBatch("d4");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD5()
        {
            RunNamedBatch("d5");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD6()
        {
            RunNamedBatch("d6");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD7()
        {
            RunNamedBatch("d7");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD8()
        {
            RunNamedBatch("d8");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestD9()
        {
            RunNamedBatch("d9");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestDA()
        {
            RunNamedBatch("da");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestDB()
        {
            RunNamedBatch("db");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestDC()
        {
            RunNamedBatch("dc");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestDD()
        {
            RunNamedBatch("dd");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestDE()
        {
            RunNamedBatch("de");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestDF()
        {
            RunNamedBatch("df");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE0()
        {
            RunNamedBatch("e0");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE1()
        {
            RunNamedBatch("e1");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE2()
        {
            RunNamedBatch("e2");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE3()
        {
            RunNamedBatch("e3");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE4()
        {
            RunNamedBatch("e4");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE5()
        {
            RunNamedBatch("e5");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE6()
        {
            RunNamedBatch("e6");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE7()
        {
            RunNamedBatch("e7");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE8()
        {
            RunNamedBatch("e8");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestE9()
        {
            RunNamedBatch("e9");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestEA()
        {
            RunNamedBatch("ea");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestEB()
        {
            RunNamedBatch("eb");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestEC()
        {
            RunNamedBatch("ec");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestED()
        {
            RunNamedBatch("ed");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestEE()
        {
            RunNamedBatch("ee");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestEF()
        {
            RunNamedBatch("ef");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF0()
        {
            RunNamedBatch("f0");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF1()
        {
            RunNamedBatch("f1");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF2()
        {
            RunNamedBatch("f2");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF3()
        {
            RunNamedBatch("f3");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF4()
        {
            RunNamedBatch("f4");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF5()
        {
            RunNamedBatch("f5");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF6()
        {
            RunNamedBatch("f6");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF7()
        {
            RunNamedBatch("f7");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF8()
        {
            RunNamedBatch("f8");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestF9()
        {
            RunNamedBatch("f9");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestFA()
        {
            RunNamedBatch("fa");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestFB()
        {
            RunNamedBatch("fb");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestFC()
        {
            RunNamedBatch("fc");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestFD()
        {
            RunNamedBatch("fd");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestFE()
        {
            RunNamedBatch("fe");
        }

        [TestMethod]
        public void RunIndividualSynertek65C02TestFF()
        {
            RunNamedBatch("ff");
        }
    }
}
