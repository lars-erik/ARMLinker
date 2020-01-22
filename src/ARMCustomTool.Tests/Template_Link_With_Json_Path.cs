using NUnit.Framework;

namespace ARMCustomTool.Tests
{
    [TestFixture]
    public class Template_Link_With_Json_Path : LinkerTestBase
    {
        private static string SimpleTemplateWithRelativeResource(string relativePath, string jsonPath) =>
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""parameters"": {},
                ""resources"": [
                {
                    ""type"": ""Microsoft.Logic/workflows"",
                    ""apiVersion"": ""2019-05-01"",
                    ""name"": ""[parameters('logicAppName')]"",
                    ""location"": ""[parameters('location')]"",
                    ""properties"": {
                        ""definition"": {
                            ""templateLink"": {
                                ""uri"": """ + relativePath + @""",
                                ""jsonPath"": """ + jsonPath + @"""
                            }
                        },
                        ""parameters"": {
                        }
                    }
                }]
            }
            ";

        private void AddAResource() =>
            FileSystem.Add(
                "c:\\users\\user\\source\\repos\\project\\aresource.json",
                @"{
                    ""definition"": {
                        ""$schema"": ""https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#"",
                        ""contentVersion"": ""1.0.0.0"",
                    },
                    ""parameters"": {
                        ""we don't want these"": ""at all""
                    }
                }"
            );

        [Test]
        public void Links_Content_From_Path_In_Graph()
        {
            const string expectedJson = @"
                {
                    ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                    ""parameters"": {},
                    ""resources"": [
                    {
                        ""type"": ""Microsoft.Logic/workflows"",
                        ""apiVersion"": ""2019-05-01"",
                        ""name"": ""[parameters('logicAppName')]"",
                        ""location"": ""[parameters('location')]"",
                        ""properties"": {
                            ""definition"": {
                                ""$schema"": ""https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#"",
                                ""contentVersion"": ""1.0.0.0"",
                            },
                            ""parameters"": {
                            }
                        }
                    }]
                }
            ";

            var templateContent = SimpleTemplateWithRelativeResource("./aresource.json", "definition");
            AddAResource();
            AssertOutput(expectedJson, templateContent);
        }

        [Test]
        public void Reports_Error_For_Invalid_Json_Path()
        {
            var templateContent = SimpleTemplateWithRelativeResource("./aresource.json", "bad.definition");
            AddAResource();

            GenerateOutput(templateContent);

            Assert.AreEqual(
                @"Invalid json path bad.definition at definition. Tool currently only supports dot separated paths to objects. ./aresource.json",
                Reporter.Errors[0].error,
                Reporter.Errors[0].error
            );

        }
    }
}
