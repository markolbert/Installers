using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Olbert.Wix;
using Olbert.Wix.buttons;
using Olbert.Wix.panels;
using Olbert.Wix.viewmodels;
using Olbert.Wix.ViewModels;

namespace Olbert.LanHistorySetupUI
{
    public sealed class LanHistorySetupViewModel : WixViewModel
    {
        private readonly string _license;
        private readonly string _intro;

        public LanHistorySetupViewModel()
        {
            WindowTitle = "LanHistory Installer";

            _license = GetEmbeddedTextFile( "license.rtf" );
            _intro = GetEmbeddedTextFile( "intro.rtf" );

            Current.Stage = "start";
            MoveNext();
        }

        public override bool IsActionSupported( LaunchAction action )
        {
            if( base.IsActionSupported( action ) ) return true;

            switch( action )
            {
                case LaunchAction.Install:
                case LaunchAction.Uninstall:
                    return true;
            }

            return false;
        }

        public override void OnDetectionComplete()
        {
            base.OnDetectionComplete();

            ( (StandardButtonsViewModel) Current.ButtonsViewModel ).NextViewModel.Show();
            ((IntroPanelViewModel)Current.PanelViewModel).Detecting = Visibility.Collapsed;
        }

        public override void OnInstallationComplete()
        {
            base.OnInstallationComplete();

            var btnVM = (StandardButtonsViewModel) Current.ButtonsViewModel;

            btnVM.CancelViewModel.Visibility = Visibility.Collapsed;
            btnVM.NextViewModel.Visibility = Visibility.Visible;
        }

        protected override void MoveNext()
        {
            switch( Current.Stage.ToLower() )
            {
                case "start":
                    CreatePanel(WixIntro.PanelID);

                    ((IntroPanelViewModel)Current.PanelViewModel).Text = _intro;

                    var btnVM = (StandardButtonsViewModel) Current.ButtonsViewModel;
                    btnVM.PreviousViewModel.Hide();
                    btnVM.NextViewModel.Hide();

                    OnStartDetect();

                    break;

                case WixIntro.PanelID:
                    CreatePanel( WixLicense.PanelID );

                    ( (LicensePanelViewModel) Current.PanelViewModel ).Text = _license;

                    break;

                case WixLicense.PanelID:
                    // see if we have anything to detect
                    if( BundleProperties.Prerequisites.Count == 0 ) DisplayProgressPanel();
                    else
                    {
                        CreatePanel( WixDependencies.PanelID );

                        ( (DependencyPanelViewModel) Current.PanelViewModel ).Dependencies =
                            BundleProperties.Prerequisites;
                    }

                    break;

                case WixDependencies.PanelID:
                    DisplayProgressPanel();
                    break;

                case WixProgress.PanelID:
                    CreatePanel( WixFinish.PanelID );

                    ( (FinishPanelViewModel) Current.PanelViewModel ).Text = "All done!";

                    break;

                case WixFinish.PanelID:
                    OnFinished();
                    break;
            }
        }

        protected override void MovePrevious()
        {
            switch( Current.Stage.ToLower() )
            {
                case WixLicense.PanelID:
                    break;

                case WixFinish.PanelID:
                    break;
            }
        }

        private void DisplayProgressPanel()
        {
            CreatePanel( WixProgress.PanelID );

            OnAction( LaunchAction.Install );
        }
    }
}
