using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Cli.Toolkit
{
    public class CliMenu
    {
        public string Title { get; set; }
        public List<MenuOption> Options { get; set; }
        public bool ClearScreenOnSelect { get; set; }

        public CliMenu(string title, bool clearScreenOnSelect = true)
        {
            Title = title;
            Options = new List<MenuOption>();
            ClearScreenOnSelect = clearScreenOnSelect;
        }

        public void AddOption(string label, Func<Task> action) { /* Async support */ }
        public void AddOption(string label, Action action) { /* Sync support */ }

        public async Task ShowAsync() { /* Handles rendering and selection */ }
    }

}
