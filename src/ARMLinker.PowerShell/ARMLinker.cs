using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ARMLinker.PowerShell
{
    [Cmdlet("Convert", "TemplateLinks")]
    public class ARMLinker : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string InputPath { get; set; }

        protected override void ProcessRecord()
        {
            var fullPath = Path.GetFullPath(InputPath);
            var linker = new ArmJsonLinker()

            
            base.ProcessRecord();
        }
    }
}
