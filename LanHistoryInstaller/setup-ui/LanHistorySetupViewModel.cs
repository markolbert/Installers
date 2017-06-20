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
        private const string UninstallIntroText = "Thanx for trying Lan History Manager";

        private readonly string _license;
        private readonly string _intro;
        private LaunchAction _origAction;
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
                else WixApp.Dispatcher.InvokeShutdown();
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
        /// Creates the UserControls -- panel and button set -- when user clicks Next.
        /// </summary>
        protected override void MoveNext()
        {
            switch( Current.Stage.ToLower() )
            {
                case "start":
                    // moving past start of wizard; the panel to display depends on whether 
                    // we're installing or uninstalling

                    // store the action we were launched with, so we can step forward 
                    // and backwards through the wizard correctly
                    _origAction = LaunchAction;

                    switch( LaunchAction )
                    {
                        case LaunchAction.Install:
                            _processRunning = IsProcessRunning( "LanHistory" );

                            if( _processRunning )
                            {
                                CreatePanel( WixFinish.PanelID );

                                ( (FinishPanelViewModel) Current.PanelViewModel ).Text =
                                    "Lan History Manager is running. Please exit it and re-launch the installer.";

                                var btnVM = (StandardButtonsViewModel)Current.ButtonsViewModel;

                                btnVM.PreviousViewModel.Hide();
                                btnVM.NextViewModel.Text = "Exit";
                                btnVM.CancelViewModel.Hide();
                            }
                            else DisplayIntroPanel( _intro );

                            break;

                        case LaunchAction.Uninstall:
                            DisplayIntroPanel( UninstallIntroText, "uninstall" );
                            break;
                    }

                    break;

                case "uninstall":
                    // moving past the uninstall panel; display progress of uninstall
                    DisplayExecutionProgress();

                    break;

                case WixIntro.PanelID:
                    // moving past intro panel; either get the action to perform if we weren't
                    // launched with either install or uninstall, or display the license panel
                    if( _origAction == LaunchAction.Unknown ) CreatePanel( WixAction.PanelID );
                    else DisplayLicensePanel();

                    break;

                case WixAction.PanelID:
                    // moving past the action selection panel; display license panel
                    DisplayLicensePanel();
                    break;

                case WixLicense.PanelID:
                    // moving past license panel; if there are no prerequisites, start
                    // installation and display progress.
                    // if there are prerequisites, display them
                    if( BundleProperties.Prerequisites.Count == 0 ) DisplayExecutionProgress();
                    else
                    {
                        CreatePanel( WixDependencies.PanelID );

                        ( (DependencyPanelViewModel) Current.PanelViewModel ).Dependencies =
                            BundleProperties.Prerequisites;
                    }

                    break;

                case WixDependencies.PanelID:
                    // moving past the dependencies/prerequisites panel; start installation
                    // and display progress
                    DisplayExecutionProgress();
                    break;

                case WixProgress.PanelID:
                    // moving past the progress of installation/uninstallation panel; display
                    // the finishing up panel
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
                    // finishing the wizard. close the UI, after first launching the app and
                    // displaying the online help if we were requested to do so
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

                    WixApp.Dispatcher.InvokeShutdown();

                    break;

                default:
                    // moving past unhandled stage; alert user about problem
                    WixApp.Dispatcher.Invoke<int>( () => new J4JMessageBox()
                        .Title( "Unknown Stage" )
                        .Message( $"An unhandled stage ({Current.Stage}) was encountered." )
                        .ButtonText( "Okay" )
                        .ShowMessageBox() );

                    break;
            }
        }

        protected override void MovePrevious()
        {
            switch (Current.Stage.ToLower())
            {
                case WixAction.PanelID:
                    // moving back to introductory panel
                    DisplayIntroPanel(_intro);
                    break;

                case WixLicense.PanelID:
                    // moving back from the license panel. if we were launched with some
                    // action other than install or uninstall, we need to land on the select
                    // action panel. otherwise, go back to the intro panel, and display the
                    // appropriate introductory text.
                    switch( _origAction )
                    {
                        case LaunchAction.Install:
                            DisplayIntroPanel( _intro );
                            break;

                        case LaunchAction.Uninstall:
                            DisplayIntroPanel( UninstallIntroText, "uninstall" );
                            break;


                        default:
                            CreatePanel( WixAction.PanelID );
                            break;
                    }

                    break;

                case WixDependencies.PanelID:
                    // moving back from dependencies/prerequisites; display the license panel.
                    DisplayLicensePanel();
                    break;

                // all other stages don't support moving backwards, so if we are trying to move
                // back from one of them, something's wrong. notify the user and do nothing.
                default:
                    WixApp.Dispatcher.Invoke<int>(() => new J4JMessageBox()
                        .Title("Unknown Stage")
                        .Message($"An unhandled stage ({Current.Stage}) was encountered.")
                        .ButtonText("Okay")
                        .ShowMessageBox());

                    break;
            }
        }

        private void DisplayIntroPanel( string text, string stage = null )
        {
            CreatePanel( WixIntro.PanelID, stage );

            ( (IntroPanelViewModel) Current.PanelViewModel ).Text = text;

            var btnVM = (StandardButtonsViewModel)Current.ButtonsViewModel;
            btnVM.PreviousViewModel.Hide();

            btnVM.NextViewModel.Hide();
            BootstrapperApp.StartDetect();
        }

        private void DisplayLicensePanel()
        {
            CreatePanel(WixLicense.PanelID);

            ((LicensePanelViewModel)Current.PanelViewModel).Text = _license;
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

    }
}
