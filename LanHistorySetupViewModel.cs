using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Olbert.Wix;
using Olbert.Wix.buttons;
using Olbert.Wix.panels;
using Olbert.Wix.ViewModels;

namespace Olbert.LanHistorySetupUI
{
    public class LanHistorySetupViewModelAutofac : WixAutofacModule<LanHistorySetupViewModel>
    {
    }

    public class LanHistorySetupViewModel : WixViewModel
    {
        private enum Stage
        {
            Intro,
            License,
            Finish
        }

        private Stage _stage = Stage.Intro;
        private readonly string _license;
        private readonly string _intro;

        public LanHistorySetupViewModel()
        {
            WindowTitle = "LanHistory Installer";

            _license = GetEmbeddedTextFile( "license.rtf" );
            _intro = GetEmbeddedTextFile( "intro.rtf" );

            var vm = new TextPanelViewModel { Text = _intro };

            CurrentButtons = new WixStandardButtons() { DataContext = vm.GetButtonsViewModel() };
            CurrentPanel = new WixTextScroller { DataContext = vm };
        }

        protected override void MoveNext()
        {
            switch( _stage )
            {
                case Stage.Intro:
                    var introVM = new LicensePanelViewModel { Text = _license };

                    CurrentButtons.DataContext = introVM.GetButtonsViewModel();
                    CurrentPanel = new WixLicense { DataContext = introVM };

                    _stage = Stage.License;

                    break;

                case Stage.License:
                    var finishVM = new FinishPanelViewModel();

                    CurrentButtons.DataContext = finishVM.GetButtonsViewModel();
                    CurrentPanel = new WixLicense { DataContext = finishVM };

                    _stage = Stage.Finish;

                    break;
            }
        }

        protected override void MovePrevious()
        {
            switch( _stage )
            {
                case Stage.License:
                    break;

                case Stage.Finish:
                    break;
            }
        }

    }
}
