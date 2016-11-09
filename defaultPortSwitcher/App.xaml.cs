using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace defaultPortSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private TaskbarIcon notifyIcon;
        private ServerManager iisManager;
        private ContextMenu contextMenu;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

            iisManager = new ServerManager();
            contextMenu = new ContextMenu();
            contextMenu.Opened += ContextMenu_Opened;
            notifyIcon.ContextMenu = contextMenu;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            contextMenu.Items.Clear();
            foreach (Site site in iisManager.Sites)
            {
                BindingCollection bindings = site.Bindings;
                MenuItem menuItem = new MenuItem();
                menuItem.Header = site.Name.ToString();
                foreach (Binding binding in bindings)
                {
                    if (binding.BindingInformation == "*:80:")
                    {
                        // replace with bitmap later on, but who cares now?!
                        menuItem.Icon = "  >";
                        break; // also, break out of the foreach
                    }
                }
                menuItem.Click += MenuItem_Click;
                contextMenu.Items.Add(menuItem);
            }

            MenuItem exitItem = new MenuItem();
            exitItem.Header = "Exit";
            exitItem.Command = new DelegateCommand { CommandAction = () => System.Windows.Application.Current.Shutdown() };
            Separator separator = new Separator();
            contextMenu.Items.Add(separator);
            contextMenu.Items.Add(exitItem);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // loop through all sites and remove the current *:80: binding
            foreach (Site site in iisManager.Sites)
            {
                BindingCollection bindings = site.Bindings;
                Binding bindingToRemove = null;
                foreach (Binding binding in bindings)
                {
                    if (binding.BindingInformation == "*:80:")
                    {
                        bindingToRemove = binding;
                        break; // we know we only have one...
                    }
                }
                if (bindingToRemove != null)
                {
                    bindingToRemove.Delete(); // remove the old binding
                    bindings.Remove(bindingToRemove); // remove it from the current BindingCollection
                    iisManager.CommitChanges(); // commit the changes to iis
                    break; // and also break out of this foreach...
                }
            }

            // add a new *:80: binding to the chosen site
            Site siteToChange = iisManager.Sites.FirstOrDefault(s => s.Name == (string)((HeaderedItemsControl)sender).Header);
            siteToChange.Bindings.Add("*:80:", "http");
            iisManager.CommitChanges();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }

        private class DelegateCommand : ICommand
        {
            public Action CommandAction { get; set; }
            public Func<bool> CanExecuteFunc { get; set; }

            public void Execute(object parameter)
            {
                CommandAction();
            }

            public bool CanExecute(object parameter)
            {
                return CanExecuteFunc == null || CanExecuteFunc();
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}
