using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ARMCustomTool.Tests
{
    [TestFixture]
    public class Template_Link_Properties : LinkerTestBase
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

        private static string DeepTemplateWithRelativeResource(string relativePath) =>
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""parameters"": {},
                ""resources"": [
                {
                  ""someproperty"": {
                    ""templateLink"": {
                      ""uri"": """ + relativePath + @"""
                    }
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
            FileSystem.Add(
                "c:\\users\\user\\source\\repos\\project\\aresource.json",
                @"{
                    ""type"": ""Microsoft.Web/connections"",
                    ""apiVersion"": ""2016-06-01"",
                    ""proper"": ""properties were probably here""
                }"
            );

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
        public void At_Deeper_Levels_Are_Replaced_With_Content_From_File_At_Relative_Uri()
        {
            const string expectedJson = @"
            {
              ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
              ""parameters"": {},
              ""resources"": [
                {
                  ""someproperty"": {
                    ""type"": ""Microsoft.Web/connections"",
                    ""apiVersion"": ""2016-06-01"",
                    ""proper"": ""properties were probably here""
                  }
                }
              ]
            }
            ";

            var relativePath = @"./aresource.json";
            var input = DeepTemplateWithRelativeResource(relativePath);
            AddAResource();

            AssertOutput(expectedJson, input);
        }

        [Test]
        [TestCase(@"")]
        [TestCase(@"-")]
        [TestCase(@"[]")] // ?? Should we swap object/templateLink with array?
        public void Reports_Error_For_Invalid_Contents(string content)
        {
            var relativePath = @"./aresource.json";
            var input = SimpleTemplateWithRelativeResource(relativePath);

            FileSystem.Add(
                "c:\\users\\user\\source\\repos\\project\\aresource.json",
                content
            );

            GenerateOutput(input);

            Assert.AreEqual(
                @"Content of linked file should be a JSON object. ./aresource.json",
                Reporter.Errors[0].error,
                Reporter.Errors[0].error
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
            Linker = new ArmJsonLinker(InputFilePath, new PhysicalFileSystem(), Reporter);

            var relativePath = @"./../aresource.json";
            var input = SimpleTemplateWithRelativeResource(relativePath);
            AddAResource();

            GenerateOutput(input);

            Assert.AreEqual(
                @"File not found at ./../aresource.json, resolved to absolute c:\users\user\source\repos\aresource.json",
                Reporter.Errors[0].error
            );
        }

        [Test]
        public void Reports_Error_For_Invalid_Url()
        {
            Linker = new ArmJsonLinker(InputFilePath, new PhysicalFileSystem(), Reporter);

            var relativePath = @"file://something.wrong/here";
            var input = SimpleTemplateWithRelativeResource(relativePath);
            AddAResource();

            GenerateOutput(input);

            Assert.AreEqual(
                @"The given path's format is not supported. file://something.wrong/here",
                Reporter.Errors[0].error,
                Reporter.Errors[0].error
            );
        }
    }
}
