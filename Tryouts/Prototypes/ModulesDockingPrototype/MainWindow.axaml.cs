/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using Avalonia.Controls;
using Avalonia.Threading;
using MorganStanley.ComposeUI.Tryouts.Core.BasicModels.Modules;
using NP.Avalonia.UniDock;
using NP.Avalonia.UniDockService;
using NP.Concepts.Behaviors;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MorganStanley.ComposeUI.Prototypes.ModulesDockingPrototype
{
    public partial class MainWindow : Window
    {
        ProcessesViewModel _viewModel;

        IUniDockService _uniDockService;

        int _newDockId = 0;

        private const string DockSerializationFileName = "DockSerialization.xml";
        private const string VMSerializationFileName = "DockVMSerialization.xml";

        public ProcessDockLayoutBehavior ActionsBehavior { get; }

        public MainWindow()
        {
        }

        public MainWindow(ProcessesViewModel viewModel)
        {
            InitializeComponent();
            _uniDockService = (IUniDockService)
                this.Resources["TheDockManager"]!;

            _uniDockService.DockItemsViewModels =
                new ObservableCollection<DockItemViewModelBase>();

            _viewModel = viewModel;

            ActionsBehavior =
                new ProcessDockLayoutBehavior
                (
                    _uniDockService,
                    _viewModel,
                    DockSerializationFileName,
                    VMSerializationFileName,
                    "MainProcessesTab",
                    "EmbeddedWindowTemplate",
                    "EmbeddedWindowHeaderTemplate"
                );

            DataContext = _viewModel;

            this.Closed += MainWindow_Closed;

            SaveButton.Click += SaveButton_Click;

            LoadButton.Click += LoadButton_Click;
        }

        private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ActionsBehavior.Save();
        }

        private void LoadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ActionsBehavior.Load();
        }

        private void MainWindow_Closed(object? sender, System.EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
