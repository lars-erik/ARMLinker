using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ARMCustomTool.Tests
{
    [TestFixture]
    public class Template_Link_Properties
    {
        [Test]
        public void Are_Replaced_With_Content_From_File_At_Uri()
        {
            const string data = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""parameters"": {},
                ""resources"": [
                {
                    ""templateLink"": {
                        ""uri"": ""./aresource.json""
                    }
                }]
            }
            ";

            const string linkedData = @"{
                ""type"": ""Microsoft.Web/connections"",
                ""apiVersion"": ""2016-06-01"",
                ""proper"": ""properties were probably here""
            }";

            var fileSystem = new FakeFileSystem();
            fileSystem.Add("c:\\users\\user\\source\\repos\\project\\aresource.json", linkedData);

            string output = "";

            IntPtr[] dataPtr = new IntPtr[]
            {
                Marshal.StringToHGlobalAnsi(output)
            };
            uint outputLength = 0;

            var linker = new ARMLinker(fileSystem);
            linker.Generate(
                "c:\\users\\user\\source\\repos\\project\\azuredeploy.template.json",
                data,
                "",
                dataPtr,
                out outputLength,
                null
            );

            var gotBack = Marshal.PtrToStringAnsi(dataPtr[0]);
            gotBack = gotBack.Substring(0, (int)outputLength);

            Console.WriteLine(gotBack);
        }

        [Test]
        public void Missing_Url()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void Invalid_Url()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void Invalid_Contents()
        {
            Assert.Inconclusive();
        }

    }

    public class FakeFileSystem : IFileSystem
    {
        Dictionary<string, string> files = new Dictionary<string, string>();

        public void Add(string path, string data)
        {
            files.Add(path, data);
        }


        public string ReadFile(string path)
        {
            return files[path];
        }
    }
}
