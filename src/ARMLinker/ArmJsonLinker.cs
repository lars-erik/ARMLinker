using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ARMCustomTool
{
    public class ArmJsonLinker
    {
        private readonly IReporter reporter;
        private readonly IFileSystem fileSystem;
        private readonly string inputPath;

        public ArmJsonLinker(string inputPath, IFileSystem fileSystem, IReporter reporter)
        {
            this.inputPath = inputPath;
            this.fileSystem = fileSystem;
            this.reporter = reporter;
        }

        public string LinkContent(string inputJson)
        {
            var graph = JsonConvert.DeserializeObject<JToken>(inputJson);
            LinkFiles(graph);
            var outputJson = graph.ToString(Formatting.Indented);
            return outputJson;
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
                    reporter.ReportError(e.Message + " " + path);
                    return inputJson;
                }

                if (!fileSystem.Exists(absolutePath))
                {
                    reporter.ReportError($"File not found at {path}, resolved to absolute {absolutePath}");
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
                    reporter.ReportError(e.Message + " " + path);
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
            reporter.ReportError("Content of linked file should be a JSON object. " + path);
        }
    }
}