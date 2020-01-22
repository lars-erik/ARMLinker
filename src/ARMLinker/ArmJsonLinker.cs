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
                try
                {
                    TryMergeLink(inputJson);
                }
                catch
                {
                    return inputJson;
                }
            }
            else
            {
                inputJson.Properties().ToList().ForEach(x => LinkFiles(x));
            }

            return inputJson;
        }

        private void TryMergeLink(JObject inputObject)
        {
            var linkData = inputObject["templateLink"];
            var linkPath = linkData.Value<string>("uri");
            var jsonPath = linkData.Value<string>("jsonPath");

            var absolutePath = AbsolutePath(linkPath);
            VerifyFileExists(absolutePath, linkPath);

            try
            {
                var linkedObject = GetLinkedContent(absolutePath, jsonPath);
                ReplaceLink(inputObject, linkedObject);
            }
            catch (NullReferenceException)
            {
                ReportInvalidJson(linkPath);
                throw;
            }
            catch (JsonReaderException)
            {
                ReportInvalidJson(linkPath);
                throw;
            }
            catch (InvalidCastException)
            {
                ReportInvalidJson(linkPath);
                throw;
            }
            catch (Exception e)
            {
                reporter.ReportError(e.Message + " " + linkPath);
                throw;
            }
        }

        private JObject GetLinkedContent(string absolutePath, string jsonPath)
        {
            var contents = JsonConvert.DeserializeObject<JObject>(fileSystem.ReadFile(absolutePath));
            if (jsonPath != null)
            {
                foreach (var part in jsonPath.Split('.'))
                {
                    try
                    {
                        contents = (JObject) contents[part];
                    }
                    catch
                    {
                        throw new Exception($"Invalid json path {jsonPath} at {part}. Tool currently only supports dot separated paths to objects.");
                    }
                }
            }

            return contents;
        }

        private static void ReplaceLink(JObject inputJson, JObject contents)
        {
            inputJson.Remove("templateLink");
            contents.Properties().ToList().ForEach(x => inputJson.Add(x.Name, x.Value));
        }

        private void VerifyFileExists(string absolutePath, string linkPath)
        {
            if (!fileSystem.Exists(absolutePath))
            {
                reporter.ReportError($"File not found at {linkPath}, resolved to absolute {absolutePath}");
                throw new Exception("File not found");
            }
        }

        private string AbsolutePath(string linkPath)
        {
            try
            {
                var basePath = Path.GetDirectoryName(inputPath);
                var absolutePath = Path.GetFullPath(Path.Combine(basePath ?? "", linkPath));
                return absolutePath;
            }
            catch (Exception e)
            {
                reporter.ReportError(e.Message + " " + linkPath);
                throw;
            }
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