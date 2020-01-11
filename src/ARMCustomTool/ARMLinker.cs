using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ARMCustomTool
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ARMLinker.PackageGuidString)]
    [ComVisible(true)]
    [ProvideObject(typeof(ARMLinker))]
    [CodeGeneratorRegistration(typeof(ARMLinker), "ARM Relative Path Linker", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(ARMLinker), "ARM Relative Path Linker", "{6D6714E1-EC76-42BE-9A63-5F6C9FB05D24}", GeneratesDesignTimeSource = true)]
    // ReSharper disable once InconsistentNaming
    public sealed class ARMLinker : IVsSingleFileGenerator
    {
        private readonly IFileSystem fileSystem;
        private string inputPath;
        private IVsGeneratorProgress progress;
        public const string PackageGuidString = "a7c0fed9-4150-4ab6-92c9-19576e038fbb";
        private bool failed = false;

        public ARMLinker()
            : this(new PhysicalFileSystem())
        {
        }

        public ARMLinker(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }


        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".linked.json";
            return pbstrDefaultExtension.Length;
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace,
            IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            failed = false;
            progress = pGenerateProgress;
            inputPath = wszInputFilePath;
            try
            {
                var inputJson = JsonConvert.DeserializeObject<JToken>(bstrInputFileContents);
                // TODO: Recurse
                LinkFiles(inputJson);

                var content = inputJson.ToString(Formatting.Indented);
                pcbOutput = CopyToOutput(content, rgbOutputFileContents);
            }
            catch
            {
                ReportError("Totally failed to transform this, sorry. :(");
                pcbOutput = 0;
            }

            return failed ? VSConstants.S_FALSE : VSConstants.S_OK;
        }

        private JToken LinkFiles(JToken inputJson)
        {
            switch (inputJson)
            {
                case JObject x: return LinkFiles(x);
                case JArray x: return LinkFiles(x);
                case JProperty x: return LinkFiles(x.Value);
            }

            return inputJson;
        }

        private JToken LinkFiles(JObject inputJson)
        {
            if (inputJson.ContainsKey("templateLink"))
            {
                string absolutePath;
                var path = inputJson["templateLink"].Value<string>("uri");
                try
                {
                    var basePath = Path.GetDirectoryName(inputPath);
                    absolutePath = Path.GetFullPath(Path.Combine(basePath, path));
                }
                catch(Exception e)
                {
                    progress.GeneratorError(0, 0, e.Message + " " + path, 0, 0);
                    return inputJson;
                }

                if (!fileSystem.Exists(absolutePath))
                {
                    ReportError($"File not found at {path}, resolved to absolute {absolutePath}");
                    return inputJson;
                }
                
                try
                {
                    var contents = JsonConvert.DeserializeObject<JObject>(fileSystem.ReadFile(absolutePath));
                    inputJson.Remove("templateLink");
                    contents.Properties().ToList().ForEach(x => inputJson.Add(x.Name, x.Value));
                }
                catch (NullReferenceException)
                {
                    ReportInvalidJson(path);
                }
                catch (JsonReaderException)
                {
                    ReportInvalidJson(path);
                }
                catch (InvalidCastException)
                {
                    ReportInvalidJson(path);
                }
                catch (Exception e)
                {
                    ReportError(e.Message + " " + path);
                }
            }
            else
            {
                inputJson.Properties().ToList().ForEach(x => LinkFiles(x));
            }

            return inputJson;
        }

        private JToken LinkFiles(JArray inputJson)
        {
            inputJson.ToList().ForEach(x => LinkFiles(x));
            return inputJson;
        }

        private void ReportInvalidJson(string path)
        {
            ReportError("Content of linked file should be a JSON object. " + path);
        }

        private void ReportError(string message)
        {
            progress.GeneratorError(0, 0, message, 0, 0);
            failed = true;
        }

        private static uint CopyToOutput(string content, IntPtr[] rgbOutputFileContents)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var length = bytes.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
            Marshal.Copy(bytes, 0, rgbOutputFileContents[0], length);
            return (uint) length;
        }
    }
}
