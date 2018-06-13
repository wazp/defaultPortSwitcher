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
        private Site currentSite;

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
                MenuItem menuItem = new MenuItem();
                menuItem.Header = site.Name.ToString();

                if(site.State == ObjectState.Started)
                {
                    currentSite = site;
                    // replace with bitmap later on, but who cares now?!
                    menuItem.Icon = "  >";
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
            // TODO: We should maybe stop and start app pools associated with the sites as well!

            // stop the currently running site
            ApplicationPool currentAppPool = getAppPool(currentSite);
            currentAppPool.Stop();
            currentSite.Stop();

            // find the currently selected site and start it.
            Site siteToStart = iisManager.Sites.FirstOrDefault(s => s.Name == (string)((HeaderedItemsControl)sender).Header);
            ApplicationPool appPool = getAppPool(siteToStart);
            appPool.Start();
            appPool.
            siteToStart.Start();
            iisManager.CommitChanges();
        }

        private ApplicationPool getAppPool (Site site)
        {
            string appPoolName = site.Applications[0].ApplicationPoolName;
            ApplicationPool appPool = iisManager.ApplicationPools[appPoolName];
            return appPool;
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
