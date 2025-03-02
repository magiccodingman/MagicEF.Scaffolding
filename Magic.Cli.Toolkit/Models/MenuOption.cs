using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Cli.Toolkit
{
    public class MenuOption
    {
        public string Label { get; }
        public Func<Task>? AsyncAction { get; }
        public Action? SyncAction { get; }

        public MenuOption(string label, Func<Task> action)
        {
            Label = label;
            AsyncAction = action;
        }

        public MenuOption(string label, Action action)
        {
            Label = label;
            SyncAction = action;
        }

        public async Task ExecuteAsync()
        {
            if (AsyncAction != null) await AsyncAction();
            else SyncAction?.Invoke();
        }
    }

}
