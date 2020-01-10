using System.Collections.Generic;

namespace ARMCustomTool.Tests
{
    public class FakeFileSystem : IFileSystem
    {
        private readonly Dictionary<string, string> files = new Dictionary<string, string>();
        public void Add(string path, string data) => files.Add(path, data);
        public string ReadFile(string path) => files[path];
        public bool Exists(string path) => files.ContainsKey(path);
    }
}