using System;
using System.IO;

namespace ARMCustomTool
{
    public class PhysicalFileSystem : IFileSystem
    {
        public string ReadFile(string path) => File.ReadAllText(path);

        public bool Exists(string path) => File.Exists(path);
    }
}