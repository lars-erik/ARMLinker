using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace ARMCustomTool
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ARMLinker.PackageGuidString)]
    [ComVisible(true)]
    [ProvideObject(typeof(ARMLinker))]
    [CodeGeneratorRegistration(typeof(ARMLinker), "ARM Relative Path Linker", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(ARMLinker), "ARM Relative Path Linker", "{151D2E53-A2C4-4D7D-83FE-D05416EBD58E}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(ARMLinker), "ARM Relative Path Linker", "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(ARMLinker), "ARM Relative Path Linker", "{BA615A9A-A410-4E40-82E7-461B6830A0BB}", GeneratesDesignTimeSource = true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // ReSharper disable once InconsistentNaming
    public sealed class ARMLinker : AsyncPackage, IVsSingleFileGenerator, IReporter
    {
        private readonly IFileSystem fileSystem;
        private IVsGeneratorProgress progress;
        public const string PackageGuidString = "a7c0fed9-4150-4ab6-92c9-19576e038fbb";
        private bool failed = false;
        private EnvDTE.DTE dte;
        private IVsSolution solution;

        public ARMLinker()
            : this(new PhysicalFileSystem())
        {
        }

        public ARMLinker(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;

        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await DebugProjectTypes();
            await Command1.InitializeAsync(this);
        }

        private async Task DebugProjectTypes()
        {
            //await Task.CompletedTask;
            //return;

            try
            {
                // TODO: Does this class ever need to be tested, so stub this?
                // TODO: Possibly move to other package instance thing all together?
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                dte = (EnvDTE.DTE) Package.GetGlobalService(typeof(EnvDTE.DTE));
                var proj1 = dte.Solution.Projects.Item(1);
                var proj2 = dte.Solution.Projects.Item(2);

                var proj1Name = proj1.UniqueName;
                var proj2Name = proj2.UniqueName;
                solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
                IVsHierarchy hier1;
                IVsHierarchy hier2;
                solution.GetProjectOfUniqueName(proj1Name, out hier1);
                solution.GetProjectOfUniqueName(proj2Name, out hier2);

                var items = new[]
                {
                    proj1.ProjectItems.Item(1),
                    proj1.ProjectItems.Item(2),
                    proj1.ProjectItems.Item(3),
                    proj1.ProjectItems.Item(4),
                };

                var item = items[0];

                Func<Properties, IDictionary<string, object>> getProps = ps => ps.Cast<Property>().ToDictionary(x => x.Name, x =>
                {
                    try
                    {
                        return x.Value;
                    }
                    catch (Exception e)
                    {
                        return "Error: " + e.Message;
                    }
                });

                var projProps = getProps(proj1.Properties);
                var proj2Props = getProps(proj2.Properties);

                var props = getProps(item.Properties);

                var item2 = dte.Solution.Projects.Item(2).ProjectItems.Item(1);
                var props2 = getProps(item2.Properties);

                var genProp = item.Properties.Cast<Property>().FirstOrDefault(x => x.Name == "Generator");
                var toolProp = item.Properties.Cast<Property>().FirstOrDefault(x => x.Name == "CustomTool");

                if (genProp != null)
                {
                    genProp.Value = "ARMLinker";
                }

                if (toolProp != null)
                {
                    toolProp.Value = "ARMLinker";
                }

                return;

                var interfaces = SupportedInterfaces(hier1);

                IVsAggregatableProjectCorrected ap1 = hier1 as IVsAggregatableProjectCorrected;
                IVsAggregatableProjectCorrected ap2 = hier2 as IVsAggregatableProjectCorrected;
                ap1.GetAggregateProjectTypeGuids(out var guids1);
                ap2.GetAggregateProjectTypeGuids(out var guids2);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private List<Type> SupportedInterfaces(IVsHierarchy hier1)
        {
            var someComObjectType = typeof(IVsHierarchy);
            var someComObjectType2 = typeof(IVsAggregatableProjectCorrected);
            var interopAssembly = someComObjectType.Assembly;
            var interopAssembly2 = someComObjectType2.Assembly;

            Func<Type, bool> implementsInterface = iface =>
            {
                try
                {
                    Marshal.GetComInterfaceForObject(hier1, iface);
                    return true;
                }
                catch
                {
                    return false;
                }
            };

            Func<Type, bool> isInterface = t =>
            {
                try
                {
                    return t.IsInterface;
                }
                catch
                {
                    return false;
                }
            };

            var supportedInterfaces = interopAssembly
                .GetTypes()
                .Where(isInterface)
                .Where(implementsInterface)
                .Union(interopAssembly2
                    .GetTypes()
                    .Where(isInterface)
                    .Where(implementsInterface))
                .ToList();

            return supportedInterfaces;
        }


        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".linked.json";
            return pbstrDefaultExtension.Length;
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace,
            IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            failed = false;
            progress = pGenerateProgress;
            try
            {
                var inputJson = bstrInputFileContents;
                var linker = new ArmJsonLinker(wszInputFilePath, fileSystem, this);
                var outputJson = linker.LinkContent(inputJson);

                pcbOutput = CopyToOutput(outputJson, rgbOutputFileContents);
            }
            catch
            {
                ReportError("Totally failed to transform this, sorry. :(");
                pcbOutput = 0;
            }

            return failed ? VSConstants.S_FALSE : VSConstants.S_OK;
        }

        public void ReportError(string message)
        {
            progress.GeneratorError(0, 0, message, 0, 0);
            failed = true;
        }

        private static uint CopyToOutput(string content, IntPtr[] rgbOutputFileContents)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var length = bytes.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
            Marshal.Copy(bytes, 0, rgbOutputFileContents[0], length);
            return (uint)length;
        }
    }
}
