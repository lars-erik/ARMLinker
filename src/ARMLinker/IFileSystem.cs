namespace ARMCustomTool
{
    public interface IFileSystem
    {
        string ReadFile(string path);
        bool Exists(string path);
    }
}