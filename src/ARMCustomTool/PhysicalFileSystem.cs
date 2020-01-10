using System;
using System.IO;

namespace ARMCustomTool
{
    public class PhysicalFileSystem : IFileSystem
    {
        public string ReadFile(string path)
        {
            return File.ReadAllText(path);
        }
    }
}