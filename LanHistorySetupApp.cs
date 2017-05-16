using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Olbert.Wix;
using Olbert.Wix.ViewModels;

namespace Olbert.LanHistorySetupUI
{
    public class LanHistorySetupApp : WixApp
    {
        private LanHistorySetupViewModel _viewModel;

        protected override IWixViewModel WixViewModel
        {
            get
            {
                if( _viewModel == null ) _viewModel = new LanHistorySetupViewModel();

                return _viewModel;
            }
        }

        protected override void OnAction( EngineActionEventArgs args )
        {
            base.OnAction( args );
            if( args == null ) return;

            switch( args.Action )
            {
                case LaunchAction.Install:
                    if( WixViewModel.InstallState == InstallState.NotPresent )
                    {
                        args.Processed = true;

                        Engine.Plan( LaunchAction.Install );
                    }
                    else args.Message = "The software is already installed";

                    break;

                case LaunchAction.Uninstall:
                    if( WixViewModel.InstallState == InstallState.Present )
                    {
                        args.Processed = true;

                        Engine.Plan( LaunchAction.Uninstall );
                    }
                    else args.Message = "The software is not installed";

                    break;

                default:
                    args.Processed = false;
                    args.Message = $"{args.Action} not implemented by {nameof(LanHistorySetupApp)}";

                    break;
            }
        }
    }
}
