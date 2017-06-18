using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Olbert.Wix;
using Olbert.Wix.ViewModels;

namespace Olbert.LanHistorySetupUI
{
    /// <summary>
    /// Extends the base class to provide a simple install/uninstall Wix Bootstrapper app for 
    /// Lan History Manager.
    /// </summary>
    public class LanHistorySetupApp : WixApp
    {
        private LanHistorySetupViewModel _viewModel;

        /// <summary>
        /// Overrides the base implementation to return an instance of LanHistorySetupViewModel, which
        /// defines the detailed implementation of the setup wizard for Lan History Manager.
        /// </summary>
        protected override IWixViewModel ViewModel => _viewModel ?? ( _viewModel = new LanHistorySetupViewModel( this ) );

        /// <summary>
        /// Executes an install or uninstall action for Lan History Manager
        /// </summary>
        /// <param name="action">the installer action to take, either LaunchAction.Install or
        /// LaunchAction.Uninstall; all other Wix actions are not implemented</param>
        /// <returns>(true, null) if the action succeeds, (false, error message) if it doesn't</returns>
        public override (bool, string) ExecuteAction( LaunchAction action )
        {
            bool okay = false;
            string mesg = null;

            switch( action )
            {
                case LaunchAction.Install:
                    if (ViewModel.InstallState == InstallState.NotPresent)
                    {
                        okay = true;
                        Engine.Plan(LaunchAction.Install);
                    }
                    else mesg = "The software is already installed";

                    break;

                case LaunchAction.Uninstall:
                    if (ViewModel.InstallState == InstallState.Present)
                    {
                        okay = true;

                        Engine.Plan(LaunchAction.Uninstall);
                    }
                    else mesg = "The software is not installed";

                    break;

                default:
                    mesg = $"{action} not implemented by {nameof(LanHistorySetupApp)}";

                    break;
            }

            return (okay, mesg);
        }

    }
}
