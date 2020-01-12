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
        private ArmJsonLinker linker;
        private FakeProgress reporter;
        private string inputFilePath = @"c:\users\user\source\repos\project\azuredeploy.template.json";

        [SetUp]
        public void Setup()
        {
            fileSystem = new FakeFileSystem();
            reporter = new FakeProgress();
            linker = new ArmJsonLinker(inputFilePath, fileSystem, reporter);
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

            AssertOutput(expectedJson, input);
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

            GenerateOutput(input);

            Assert.AreEqual(
                @"Content of linked file should be a JSON object. ./aresource.json",
                reporter.Errors[0].error,
                reporter.Errors[0].error
            );
        }

        [Test]
        public void Ignores_Missing_Uri_Property()
        {
            var input = MissingUriTemplate();
            AddAResource();

            AssertOutput(input, input);
        }

        [Test]
        public void Reports_Error_For_Missing_File()
        {
            linker = new ArmJsonLinker(inputFilePath, new PhysicalFileSystem(), reporter);

            var relativePath = @"./../aresource.json";
            var input = SimpleTemplateWithRelativeResource(relativePath);
            AddAResource();

            GenerateOutput(input);

            Assert.AreEqual(
                @"File not found at ./../aresource.json, resolved to absolute c:\users\user\source\repos\aresource.json",
                reporter.Errors[0].error
            );
        }

        [Test]
        public void Reports_Error_For_Invalid_Url()
        {
            linker = new ArmJsonLinker(inputFilePath, new PhysicalFileSystem(), reporter);

            var relativePath = @"file://something.wrong/here";
            var input = SimpleTemplateWithRelativeResource(relativePath);
            AddAResource();

            GenerateOutput(input);

            Assert.AreEqual(
                @"The given path's format is not supported. file://something.wrong/here",
                reporter.Errors[0].error,
                reporter.Errors[0].error
            );
        }


        private void AssertOutput(string expectedJson, string input)
        {
            var actual = GenerateOutput(input);
            Console.WriteLine(actual);
            var expected = JObject.Parse(expectedJson).ToString(Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        private string GenerateOutput(string input)
        {
            var actual = linker.LinkContent(input);
            return actual;
        }
    }
}
