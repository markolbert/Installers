using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Microsoft.Win32;
using Olbert.JumpForJoy.WPF;
using Olbert.Wix;
using Olbert.Wix.Panels;
using Olbert.Wix.ViewModels;

namespace Olbert.LanHistorySetupUI
{
    public sealed class LanHistorySetupViewModel : WixViewModel
    {
        private readonly string _license;
        private readonly string _intro;
        private bool _processRunning;

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
            if (BundleInstalled && LaunchAction != LaunchAction.Uninstall)
            {
                int response = -1;

                try
                {
                    response = new J4JMessageBox().Title( "Installation Status" )
                        .Message( "Lan History Manager is already installed. Do you want to uninstall it?" )
                        .ButtonText( "Uninstall", null, "Cancel" )
                        .ShowMessageBox();
                }
                catch( Exception e )
                {
                    
                }

                if (response == 0)
                {
                    LaunchAction = LaunchAction.Uninstall;
                    Current.Stage = "start";
                    MoveNext();
                }
                else WixApp.CancelInstallation();
            }

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
                    switch( LaunchAction )
                    {
                        case LaunchAction.Install:
                            _processRunning = IsProcessRunning( "LanHistory" );

                            if( _processRunning )
                            {
                                CreatePanel( WixFinish.PanelID );

                                ( (FinishPanelViewModel) Current.PanelViewModel ).Text =
                                    "Lan History Manager is running. Please exit it and re-launch the installer.";
                            }
                            else
                            {
                                CreatePanel( WixIntro.PanelID );

                                ( (IntroPanelViewModel) Current.PanelViewModel ).Text = _intro;
                            }

                            break;

                        case LaunchAction.Uninstall:
                            CreatePanel( WixIntro.PanelID, "uninstall" );

                            ((IntroPanelViewModel)Current.PanelViewModel).Text = "Thanx for trying Lan History Manager";

                            break;
                    }


                    var btnVM = (StandardButtonsViewModel) Current.ButtonsViewModel;
                    btnVM.PreviousViewModel.Hide();

                    switch( Current.Stage )
                    {
                        case WixFinish.PanelID:
                            btnVM.NextViewModel.Text = "Exit";
                            btnVM.CancelViewModel.Hide();

                            break;

                        default:
                            btnVM.NextViewModel.Hide();
                            WixApp.StartDetect();

                            break;
                    }

                    break;

                case "uninstall":
                    if( LaunchAction == LaunchAction.Uninstall )
                    {
                        foreach( Process lhProc in Process.GetProcessesByName( "LanHistory" ) )
                        {
                            lhProc.Kill();
                        }
                    }

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

                    var finishVM = (FinishPanelViewModel) Current.PanelViewModel;

                    finishVM.Text = "All done!";

                    if( LaunchAction == LaunchAction.Install )
                    {
                        finishVM.ShowHelpVisibility = Visibility.Visible;
                        finishVM.LaunchAppVisibility = Visibility.Visible;
                    }

                    break;

                case WixFinish.PanelID:
                    var finishVM2 = (FinishPanelViewModel) Current.PanelViewModel;

                    if( LaunchAction == LaunchAction.Install && !_processRunning )
                    {
                        if( finishVM2.LaunchApp )
                        {
                            using( RegistryKey hklm = Environment.Is64BitOperatingSystem
                                ? RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry64 )
                                : RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry32 ) )
                            {
                                using( RegistryKey key =
                                    hklm.OpenSubKey( @"SOFTWARE\Jump for Joy Software\Install Locations" ) )
                                {
                                    if( key != null )
                                    {
                                        var exePath = key.GetValue( "LanHistory" ) as string;
                                        if( !String.IsNullOrEmpty( exePath ) )
                                            Process.Start( new ProcessStartInfo( exePath ) );
                                    }
                                }
                            }

                        }

                        if( finishVM2.ShowHelp )
                            Process.Start( "http://www.JumpForJoySoftware.com/Lan-History-Manager" );
                    }

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
