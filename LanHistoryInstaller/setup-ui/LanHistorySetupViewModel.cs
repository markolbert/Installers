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
    /// <summary>
    /// Extends the base class to define an installer wizard for Lan History Manager
    /// </summary>
    public sealed class LanHistorySetupViewModel : WixViewModel
    {
        private readonly string _license;
        private readonly string _intro;
        private bool _processRunning;

        /// <summary>
        /// Creates an instance of the view model linked to the provided Wix Bootstrapper application
        /// </summary>
        /// <param name="wixApp">the Wix Bootstrapper application which interfaces between the view model and
        /// the Wix Bootstrapper engine</param>
        public LanHistorySetupViewModel( IWixApp wixApp )
            : base( wixApp )
        {
            WindowTitle = "LanHistory Installer";

            _license = GetEmbeddedTextFile( "license.rtf" );
            _intro = GetEmbeddedTextFile( "intro.rtf" );

            Current.Stage = "start";
            MoveNext();
        }

        /// <summary>
        /// Defines the Wix installation actions supported by this installer; only LaunchAction.Install and
        /// LaunchAction.Uninstall are supported.
        /// </summary>
        public override IEnumerable<LaunchAction> SupportedActions { get; } = new LaunchAction[]
            { LaunchAction.Install, LaunchAction.Uninstall };

        /// <summary>
        /// Overrides the base implementation so that, if Lan History Manager is already installed and the
        /// installer is launched to install it again, the user is given the chance to uninstall Lan History
        /// Manager instead.
        /// 
        /// Also updates the UI so that the user can click the Next button to move to the next step of the wizard.
        /// 
        /// This can happen if the installer is launched from the commandline, or by double-clicking the installer
        /// through Windows File Explorer, because the default action is LaunchAction.Install.
        /// </summary>
        public override void OnDetectionComplete()
        {
            base.OnDetectionComplete();

            if ( BundleInstalled && LaunchAction != LaunchAction.Uninstall )
            {
                int response = WixApp.Dispatcher.Invoke<int>( () => new J4JMessageBox()
                    .Title( "Installation Status" )
                    .Message( "Lan History Manager is already installed. Do you want to uninstall it?" )
                    .ButtonText( "Uninstall", null, "Cancel" )
                    .ShowMessageBox() );

                if( response == 0 )
                {
                    LaunchAction = LaunchAction.Uninstall;
                    Current.Stage = "start";

                    WixApp.Dispatcher.Invoke( MoveNext );
                }
                else BootstrapperApp.CancelInstallation();
            }

            ( (StandardButtonsViewModel) Current.ButtonsViewModel ).NextViewModel.Show();
            ((IntroPanelViewModel)Current.PanelViewModel).Detecting = Visibility.Collapsed;
        }

        /// <summary>
        /// Overrides the base implementation to hide/collapse the Cancel button, and show
        /// the Next button.
        /// </summary>
        public override void OnInstallationComplete()
        {
            base.OnInstallationComplete();

            var btnVM = (StandardButtonsViewModel) Current.ButtonsViewModel;

            btnVM.CancelViewModel.Visibility = Visibility.Collapsed;
            btnVM.NextViewModel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Creates the UserControls -- panel and button set -- for each stage of the
        /// installer.
        /// </summary>
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
                            BootstrapperApp.StartDetect();

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

                    BootstrapperApp.Finish();

                    break;
            }
        }

        /// <summary>
        /// TODO: need to implement
        /// </summary>
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
            (bool okay, string mesg) = BootstrapperApp.ExecuteAction( LaunchAction );

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
