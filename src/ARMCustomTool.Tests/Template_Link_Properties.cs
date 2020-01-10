using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ARMCustomTool.Tests
{
    [TestFixture]
    public class Template_Link_Properties
    {
        private static string SimpleTemplateWithRelativeResource(string relativePath) =>
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""parameters"": {},
                ""resources"": [
                {
                    ""templateLink"": {
                        ""uri"": """ + relativePath + @"""
                    }
                }]
            }
            ";

        private static string MissingUriTemplate() =>
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""parameters"": {},
                ""resources"": [
                {
                    ""templateLink"": {
                    }
                }]
            }
            ";

        private void AddAResource() =>
            fileSystem.Add(
                "c:\\users\\user\\source\\repos\\project\\aresource.json",
                @"{
                    ""type"": ""Microsoft.Web/connections"",
                    ""apiVersion"": ""2016-06-01"",
                    ""proper"": ""properties were probably here""
                }"
            );

        private FakeFileSystem fileSystem;
        private ARMLinker linker;
        private FakeProgress progress;
        private string inputFilePath = @"c:\users\user\source\repos\project\azuredeploy.template.json";

        [SetUp]
        public void Setup()
        {
            fileSystem = new FakeFileSystem();
            progress = new FakeProgress();
            linker = new ARMLinker(fileSystem);
        }

        [Test]
        public void Are_Replaced_With_Content_From_File_At_Relative_Uri()
        {
            const string expectedJson = @"
                {
                  ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                  ""parameters"": {},
                  ""resources"": [
                    {
                      ""type"": ""Microsoft.Web/connections"",
                      ""apiVersion"": ""2016-06-01"",
                      ""proper"": ""properties were probably here""
                    }
                  ]
                }
            ";

            var relativePath = @"./aresource.json";
            var input = SimpleTemplateWithRelativeResource(relativePath);
            AddAResource();

            AssertOutput(expectedJson, inputFilePath, input);
        }

        [Test]
        [TestCase(@"")]
        [TestCase(@"-")]
        [TestCase(@"[]")] // ?? Should we swap object/templateLink with array?
        public void Invalid_Contents(string content)
        {
            var relativePath = @"./aresource.json";
            var input = SimpleTemplateWithRelativeResource(relativePath);

            fileSystem.Add(
                "c:\\users\\user\\source\\repos\\project\\aresource.json",
                content
            );

            GenerateOutput(inputFilePath, input);

            Assert.AreEqual(
                @"Content of linked file should be a JSON object. ./aresource.json",
                progress.Errors[0].error,
                progress.Errors[0].error
            );
        }

        [Test]
        public void Ignores_Missing_Uri_Property()
        {
            var input = MissingUriTemplate();
            AddAResource();

            AssertOutput(input, inputFilePath, input);
        }

        [Test]
        public void Reports_Error_For_Missing_File()
        {
            linker = new ARMLinker(new PhysicalFileSystem());

            var relativePath = @"./../aresource.json";
            var input = SimpleTemplateWithRelativeResource(relativePath);
            AddAResource();

            GenerateOutput(inputFilePath, input);

            Assert.AreEqual(
                @"File not found at ./../aresource.json, resolved to absolute c:\users\user\source\repos\aresource.json",
                progress.Errors[0].error
            );
        }

        [Test]
        public void Reports_Error_For_Invalid_Url()
        {
            linker = new ARMLinker(new PhysicalFileSystem());

            var relativePath = @"file://something.wrong/here";
            var input = SimpleTemplateWithRelativeResource(relativePath);
            AddAResource();

            GenerateOutput(inputFilePath, input);

            Assert.AreEqual(
                @"The given path's format is not supported. file://something.wrong/here",
                progress.Errors[0].error,
                progress.Errors[0].error
            );
        }


        private void AssertOutput(string expectedJson, string inputFilePath, string input)
        {
            var actual = GenerateOutput(inputFilePath, input);
            Console.WriteLine(actual);
            var expected = JObject.Parse(expectedJson).ToString(Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        private string GenerateOutput(string inputFilePath, string input)
        {
            var dataPtr = new[] {Marshal.StringToHGlobalAnsi("")};

            linker.Generate(
                inputFilePath,
                input,
                "",
                dataPtr,
                out var outputLength,
                progress
            );

            var actual = Marshal.PtrToStringAnsi(dataPtr[0]);
            actual = actual?.Substring(0, (int) outputLength);
            return actual;
        }
    }
}
