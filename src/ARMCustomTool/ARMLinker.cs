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
    [CodeGeneratorRegistration(typeof(ARMLinker), "ARM Custom Tool", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    // ReSharper disable once InconsistentNaming
    public sealed class ARMLinker : IVsSingleFileGenerator
    {
        private readonly IFileSystem fileSystem;
        private string inputPath;
        public const string PackageGuidString = "a7c0fed9-4150-4ab6-92c9-19576e038fbb";

#if ISSAMPLE

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        #endregion

#endif
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
            pbstrDefaultExtension = ".generated.txt";
            return pbstrDefaultExtension.Length;
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace,
            IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            inputPath = wszInputFilePath;
            try
            {
                var inputJson = JsonConvert.DeserializeObject<JToken>(bstrInputFileContents);
                // TODO: Recurse
                LinkFiles(inputJson);

                var content = inputJson.ToString(Formatting.Indented);
                pcbOutput = CopyToOutput(content, rgbOutputFileContents);
            }
            catch (Exception e)
            {
#if DEBUG
                throw e;
#endif
                pcbOutput = 0;
            }

            return VSConstants.S_OK;
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
                var path = inputJson["templateLink"].Value<string>("uri");
                var basePath = Path.GetDirectoryName(inputPath);
                var absolutePath = Path.GetFullPath(Path.Combine(basePath, path));
                // TODO: Handle error
                var contents = JsonConvert.DeserializeObject<JObject>(fileSystem.ReadFile(absolutePath));
                inputJson.Remove("templateLink");
                contents.Properties().ToList().ForEach(x => inputJson.Add(x.Name, x.Value));
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
