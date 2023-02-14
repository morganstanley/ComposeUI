using System;
using System.Collections.Generic;
using System.CommandLine.Binding;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell
{
    internal class WindowBinder : BinderBase<MainWebWindow>
    {
        private readonly Option<string> _titleOption;
        private readonly Option<string> _urlOption;
        private readonly Option<int> _widthOption;
        private readonly Option<int> _heightOption;


        public WindowBinder(
            Option<string> titleOption,
            Option<string> urlOption,
            Option<int> widthOption,
            Option<int> heightOption)
        {
            _titleOption = titleOption;
            _urlOption = urlOption;
            _widthOption = widthOption;
            _heightOption = heightOption;
        }

        protected override MainWebWindow GetBoundValue(BindingContext bindingContext) =>
            new MainWebWindow(bindingContext.ParseResult.GetValueForOption(_urlOption))
            {
                Title = bindingContext.ParseResult.GetValueForOption(_titleOption) ?? "Compose Web Container",
                Width = bindingContext.ParseResult.GetValueForOption(_widthOption),
                Height = bindingContext.ParseResult.GetValueForOption(_heightOption)
            };
    }
}
