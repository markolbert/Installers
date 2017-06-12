using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Olbert.Wix;
using Olbert.Wix.ViewModels;

namespace Olbert.LanHistorySetupUI
{
    public class LanHistorySetupApp : WixApp
    {
        private LanHistorySetupViewModel _viewModel;

        protected override IWixViewModel WixViewModel => _viewModel ?? ( _viewModel = new LanHistorySetupViewModel( this ) );

        public override (bool, string) ExecuteAction( LaunchAction action )
        {
            bool okay = false;
            string mesg = null;

            switch( action )
            {
                case LaunchAction.Install:
                    if (WixViewModel.InstallState == InstallState.NotPresent)
                    {
                        okay = true;
                        Engine.Plan(LaunchAction.Install);
                    }
                    else mesg = "The software is already installed";

                    break;

                case LaunchAction.Uninstall:
                    if (WixViewModel.InstallState == InstallState.Present)
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
