using System;
using System.Runtime.InteropServices;
using IntelliSenseExtender.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IntelliSenseExtender
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(OptionsPage),
        "IntelliSense Extender", "General", 0, 0, true)]
    [ProvideProfile(typeof(OptionsPage),
        "IntelliSense Extender", "General", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class IntelliSenseExtenderPackage : AsyncPackage
    {
        /// <summary>
        /// IntelliSenseExtenderPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "497C930A-E6CD-4B49-BE5A-35F8DC7E79CA";
        public static Guid PackageGuid = new Guid(PackageGuidString);

        private static readonly object lockObject = new object();
        private static OptionsPage? optionsPage;
        public static OptionsPage? OptionsPage
        {
            get
            {
                if (optionsPage == null)
                {
                    lock (lockObject)
                    {
                        if (optionsPage == null)
                        {
                            EnsurePackageLoaded();
                        }
                    }
                }
                return optionsPage;
            }

            private set => optionsPage = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelliSenseExtenderPackage"/> class.
        /// </summary>
        public IntelliSenseExtenderPackage()
        {
        }

        protected override async System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

            // When initialized asynchronously, we *may* be on a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            // Otherwise, remove the switch to the UI thread if you don't need it.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            OptionsPage = (OptionsPage)GetDialogPage(typeof(OptionsPage));
        }

        private static void EnsurePackageLoaded()
        {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            var shell = (IVsShell)GetGlobalService(typeof(SVsShell));

            if (shell.IsPackageLoaded(ref PackageGuid, out IVsPackage _) != VSConstants.S_OK)
            {
                ErrorHandler.Succeeded(shell.LoadPackage(ref PackageGuid, out _));
            }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }
    }
}
