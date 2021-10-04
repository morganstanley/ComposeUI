/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */
using System;
using System.Windows.Input;

namespace DockManagerCore.Desktop
{

    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object> canExecute;
        private readonly Action<object> execute;
        event EventHandler ICommand.CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public DelegateCommand(Action<object> execute_, Predicate<object> canExecute_)
        {
            execute = execute_;
            canExecute = canExecute_;
        }
        public DelegateCommand(Action<object> execute_)
            : this(execute_, null)
        {
        }
        bool ICommand.CanExecute(object parameter_)
        {
            return canExecute == null || canExecute(parameter_);
        }
        void ICommand.Execute(object parameter_)
        {
            execute(parameter_);
        }
    }
}
