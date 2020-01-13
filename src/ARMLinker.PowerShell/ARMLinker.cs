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
    public class ARMLinker : Cmdlet, IReporter
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string InputPath { get; set; }

        [Parameter(Position = 1, Mandatory = false)]
        public string OutputPath { get; set; }

        protected override void ProcessRecord()
        {
            var fullPath = Path.GetFullPath(InputPath);
            var linker = new ArmJsonLinker(fullPath, new PhysicalFileSystem(), this);
            var output = linker.LinkContent(File.ReadAllText(fullPath));

            if (String.IsNullOrWhiteSpace(OutputPath))
            { 
                WriteObject(output);
            }
            else
            {
                File.WriteAllText(Path.GetFullPath(OutputPath), output);
            }
        }

        public void ReportError(string message)
        {
            WriteError(new ErrorRecord(new Exception(message), "0", ErrorCategory.InvalidOperation, null));
        }
    }
}
