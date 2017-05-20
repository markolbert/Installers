using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Olbert.JumpForJoy.WPF;
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

        public LanHistorySetupViewModel( IWixApp wixApp )
            : base( wixApp )
        {
            WindowTitle = "LanHistory Installer";

            _license = GetEmbeddedTextFile( "license.rtf" );
            _intro = GetEmbeddedTextFile( "intro.rtf" );

            Current.Stage = "start";
            MoveNext();
        }

        public override IEnumerable<LaunchAction> SupportedActions { get; } = new LaunchAction[]
            { LaunchAction.Install, LaunchAction.Uninstall };

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
                    var curStage = LaunchAction == LaunchAction.Install ? null : "uninstall";
                    var introText = LaunchAction == LaunchAction.Install ? _intro : "Thanx for trying Lan History Manager";

                    CreatePanel(WixIntro.PanelID, curStage);

                    ((IntroPanelViewModel)Current.PanelViewModel).Text = introText;

                    var btnVM = (StandardButtonsViewModel)Current.ButtonsViewModel;
                    btnVM.PreviousViewModel.Hide();
                    btnVM.NextViewModel.Hide();

                    WixApp.StartDetect();

                    break;

                case "uninstall":
                    DisplayExecutionProgress();

                    break;

                case WixIntro.PanelID:
                    // if no action was specified, get one
                    if( LaunchAction == LaunchAction.Unknown ) CreatePanel( "actions" );
                    else DisplayLicensePanel();

                    break;

                case WixAction.PanelID:
                    DisplayLicensePanel();
                    break;

                case WixLicense.PanelID:
                    // see if we have anything to detect
                    if( BundleProperties.Prerequisites.Count == 0 ) DisplayExecutionProgress();
                    else
                    {
                        CreatePanel( WixDependencies.PanelID );

                        ( (DependencyPanelViewModel) Current.PanelViewModel ).Dependencies =
                            BundleProperties.Prerequisites;
                    }

                    break;

                case WixDependencies.PanelID:
                    DisplayExecutionProgress();
                    break;

                case WixProgress.PanelID:
                    CreatePanel( WixFinish.PanelID );

                    ( (FinishPanelViewModel) Current.PanelViewModel ).Text = "All done!";

                    break;

                case WixFinish.PanelID:
                    WixApp.Finish();
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

        private void DisplayExecutionProgress()
        {
            (bool okay, string mesg) = WixApp.ExecuteAction( LaunchAction );

            if( okay ) CreatePanel( WixProgress.PanelID );
            else
            {
                CreatePanel( WixTextScroller.PanelID, WixFinish.PanelID );

                ( (TextPanelViewModel) Current.PanelViewModel ).Text = mesg;

                var btnVM = (StandardButtonsViewModel) Current.ButtonsViewModel;
                btnVM.CancelViewModel.Visibility = Visibility.Collapsed;
                btnVM.PreviousViewModel.Visibility = Visibility.Collapsed;
                btnVM.NextViewModel.Text = "Finish";
            }
        }

        private void DisplayLicensePanel()
        {
            CreatePanel(WixLicense.PanelID);

            ((LicensePanelViewModel)Current.PanelViewModel).Text = _license;
        }
    }
}
