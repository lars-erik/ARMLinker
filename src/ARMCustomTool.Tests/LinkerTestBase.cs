using System;
using ARMCustomTool.Tests.Fakes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ARMCustomTool.Tests
{
    public class LinkerTestBase
    {
        protected FakeFileSystem FileSystem;
        protected ArmJsonLinker Linker;
        protected FakeProgress Reporter;
        protected string InputFilePath = @"c:\users\user\source\repos\project\azuredeploy.template.json";

        [SetUp]
        public void Setup()
        {
            FileSystem = new FakeFileSystem();
            Reporter = new FakeProgress();
            Linker = new ArmJsonLinker(InputFilePath, FileSystem, Reporter);
        }

        protected void AssertOutput(string expectedJson, string input)
        {
            var actual = GenerateOutput(input);
            Console.WriteLine((string) actual);
            var expected = JObject.Parse(expectedJson).ToString(Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        protected string GenerateOutput(string input)
        {
            var actual = Linker.LinkContent(input);
            return actual;
        }
    }
}