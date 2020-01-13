using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using ARMCustomTool;

namespace ARMLinker.PowerShell
{
    [Cmdlet("Convert", "TemplateLinks")]
    public class ARMLinker : PSCmdlet, IReporter
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string InputPath { get; set; }

        [Parameter(Position = 1, Mandatory = false)]
        public string OutputPath { get; set; }

        protected override void ProcessRecord()
        {
            var inputPath = ResolvePath(InputPath);

            var linker = new ArmJsonLinker(inputPath, new PhysicalFileSystem(), this);
            var output = linker.LinkContent(File.ReadAllText(inputPath));

            if (String.IsNullOrWhiteSpace(OutputPath))
            { 
                WriteObject(output);
            }
            else
            {
                File.WriteAllText(ResolvePath(OutputPath), output);
            }
        }

        private string ResolvePath(string absoluteOrRelativePath)
        {
            string resolvedPath;
            if (Path.IsPathRooted(absoluteOrRelativePath))
            {
                resolvedPath = Path.GetFullPath(absoluteOrRelativePath);
            }
            else
            {
                resolvedPath =
                    Path.GetFullPath(Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, absoluteOrRelativePath));
            }

            return resolvedPath;
        }

        public void ReportError(string message)
        {
            WriteError(new ErrorRecord(new Exception(message), "0", ErrorCategory.InvalidOperation, null));
        }
    }
}
