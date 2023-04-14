using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShortcutLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool _mainStackPanelCursorInside;
        private double _launcherIconMultiplier;
        private bool _animation_is_running = false;
        private bool _popupPinned = false;
        private System.Threading.SynchronizationContext _callersCtx;
        private MouseEventHandler _mouseEventHandler;
        private Point _mouseDownLocation;
        private bool _runAAdminsChecked;
        Timer _timer;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool SmallSizeChecked
        {
            get
            {
                return UCWithImage.SmallSizeChecked;
            }
            set
            {
                UCWithImage.SmallSizeChecked = value;
                RefreshView();
            }
        }
        public bool MediumSizeChecked
        {
            get
            {
                return UCWithImage.MediumSizeChecked;
            }
            set
            {
                UCWithImage.MediumSizeChecked = value;
                RefreshView();
            }
        }
        public bool LargeSizeChecked
        {
            get
            {
                return UCWithImage.LargeSizeChecked;
            }
            set
            {
                UCWithImage.LargeSizeChecked = value;
                RefreshView();
            }
        }

        public bool RunAsAdminChecked
        {
            get
            {
                return _runAAdminsChecked;
            }
            set
            {
                _runAAdminsChecked = value;
                Properties.Settings.Default.RunAsAdminChecked = value;
                Properties.Settings.Default.Save();
            }
        }

        public double WindowHeight
        {
            get
            {
                return /*_mainStackPanelPopup.IsOpen ? System.Windows.SystemParameters.WorkArea.Height * 0.4 : */System.Windows.SystemParameters.WorkArea.Height * 0.1;
            }
        }

        public double WindowWidth
        {
            get
            {
                return /*_mainStackPanelPopup.IsOpen ? System.Windows.SystemParameters.WorkArea.Width * 0.4 : */System.Windows.SystemParameters.WorkArea.Height * 0.1;
            }
        }

        public double MainStackPanelWidth
        {
            get
            {
                return Math.Max(0, System.Windows.SystemParameters.WorkArea.Width * 0.4);
            }
        }
        public double MainStackPanelHeight
        {
            get
            {
                return Math.Max(0, System.Windows.SystemParameters.WorkArea.Height * 0.4);
            }
        }

        public double MainStackPanelPinRowHeight
        {
            get
            {
                return Math.Max(0, MainStackPanelHeight*0.05);
            }
        }
        public double MainStackPanelShortcutContainerRowHeight
        {
            get
            {
                return Math.Max(0, MainStackPanelHeight * 0.95);
            }
        }

        public double ShortcutLauncherButtonWidth
        {
            get
            {
                return Math.Max(50, WindowWidth);
            }
        }

        public double ShortcutLauncherButtonHeight
        {
            get
            {
                return Math.Max(50 - (_launcherIconMultiplier * 50), WindowWidth / _launcherIconMultiplier);
            }
        }

        private void _VSLauncherButton_MouseMove(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("Leftbutton pressed: " + (Mouse.LeftButton == MouseButtonState.Pressed).ToString());
            double distance = 0;
            if(e != null)
                distance = Math.Max(distance, Math.Abs(Point.Subtract(e.GetPosition(this), _mouseDownLocation).Length));
            if (Mouse.LeftButton == MouseButtonState.Pressed && distance > 5)
            {
                (sender as ContentControl).Cursor = Cursors.SizeAll;

                if (!_animation_is_running)
                {
                    var decreaseOpacityAnim = new DoubleAnimation(0.5, (Duration)TimeSpan.FromSeconds(1));
                    this.BeginAnimation(UIElement.OpacityProperty, decreaseOpacityAnim);
                    _animation_is_running = true;
                }

                this.DragMove();
                Debug.WriteLine("dragging");
            }
            else
            {
                if (_animation_is_running)
                {
                    var increaseOpacityAnim = new DoubleAnimation(1, (Duration)TimeSpan.FromSeconds(1));
                    this.BeginAnimation(UIElement.OpacityProperty, increaseOpacityAnim);
                    _animation_is_running = false;
                }
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            _callersCtx = System.Threading.SynchronizationContext.Current;
            _mainStackPanelCursorInside = false;
            _runAAdminsChecked = Properties.Settings.Default.RunAsAdminChecked;
            this.DataContext = this;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = System.Windows.SystemParameters.WorkArea.Right * 0.9;
            this.Height = WindowHeight;
            this.Width = WindowWidth;
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.shortcut.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            _launcherIconMultiplier = bitmapSource.Width / bitmapSource.Height;
            ShortcutLauncherButton_ContentControl_Image.Source = bitmapSource;
            var bitmapSource1 = Imaging.CreateBitmapSourceFromHBitmap(_popupPinned ? Properties.Resources.Pin.GetHbitmap() : Properties.Resources.UnPin.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            PinUnpinButton_Image.Source = bitmapSource1;

            this.Title = "Shortcut Launcher";
            this.Loaded += Form1_Load;
            _TopStackPanel.DataContext = this;
            this.SizeChanged += MainWindow_SizeChanged;
            _ShortcutsContainer.Background = Brushes.Black;
            _ShortcutsContainer.Opacity = 0.5;
            _mainStackPanelPopup.PlacementTarget = _TopStackPanel;

            _mainStackPanelPopup.MouseLeave += _mainStackPanel_MouseLeave;
            _mainStackPanelPopup.MouseEnter += _mainStackPanel_MouseEnter;
            UCWithImage.ParentContainer = _ShortcutsContainer;

            ShortcutLauncherButton_ContentControl.MouseEnter += _VSLauncherButton_ContentControl_MouseEnter;
            ShortcutLauncherButton_ContentControl.MouseLeave += _VSLauncherButton_ContentControl_MouseLeave;
            ShortcutLauncherButton_ContentControl.PreviewMouseLeftButtonDown += _VSLauncherButton_PreviewMouseLeftButtonDown;
            ShortcutLauncherButton_ContentControl.PreviewMouseLeftButtonUp += _VSLauncherButton_PreviewMouseLeftButtonUp;
            _timer = new Timer(1500);
            _timer.Elapsed += (sender1, args) =>
            {
                if (!_mainStackPanelCursorInside)
                {
                    _callersCtx.Post((_) =>
                    {
                        if (!_popupPinned && !_mainStackPanelCursorInside && !_ShortcutsContainer.IsMouseDirectlyOver && !ShortcutLauncherButton_ContentControl.IsMouseDirectlyOver && _mainStackPanelPopup.IsOpen)
                        {
                            _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                            _ShortcutsContainer.Opacity = 0.5; 
                            _mainStackPanelPopup.IsOpen = false;
                            Debug.WriteLine("mainstackpanel visibility collapsed: _VSLauncherButton_MouseLeave");
                            RefreshView();
                        }
                    }, null);
                }
            };
            _mouseEventHandler = new MouseEventHandler(_VSLauncherButton_MouseMove);
            
            _mainStackPanelPopup.IsOpen = false;
        }

        private void _mainStackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            _mainStackPanelCursorInside = true;
            _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
            _ShortcutsContainer.Opacity = 0.5;
            var increaseOpacityAnim = new DoubleAnimation(1, (Duration)TimeSpan.FromSeconds(0.5));
            _ShortcutsContainer.BeginAnimation(UIElement.OpacityProperty, increaseOpacityAnim);
            Debug.WriteLine("_mainStackPanel_MouseEnter");

        }

        private void _mainStackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            _mainStackPanelCursorInside = false;
            _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
            _ShortcutsContainer.Opacity = 1;
            var decreaseOpacityAnim = new DoubleAnimation(0.5, (Duration)TimeSpan.FromSeconds(0.5));
            _ShortcutsContainer.BeginAnimation(UIElement.OpacityProperty, decreaseOpacityAnim);
            if (!_popupPinned)
            {
                _mainStackPanelPopup.IsOpen = false;
                Debug.WriteLine("mainstackpanel visibility collapsed: _mainStackPanel_MouseLeave");
                RefreshView();
            }
        }

        private void _VSLauncherButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as ContentControl).Cursor = Cursors.Arrow;
            Debug.WriteLine("_VSLauncherButton_PreviewMouseLeftButtonUp");
            ShortcutLauncherButton_ContentControl.MouseMove -= _mouseEventHandler;
            Debug.WriteLine("ShortcutLauncherButton_ContentControl.MouseMove unsubscribed");

            _mainStackPanelPopup.IsOpen = true;
            RefreshView();
            Debug.WriteLine("mainstackpanel visible: _VSLauncherButton_ContentControl_MouseEnter");

        }

        private void _VSLauncherButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mainStackPanelPopup.IsOpen = false;
            _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
            _ShortcutsContainer.Opacity = 0.5;
            Debug.WriteLine("mainstackpanel visibility collapsed: _VSLauncherButton_PreviewMouseLeftButtonDown");
            RefreshView();
            ShortcutLauncherButton_ContentControl.MouseMove += _mouseEventHandler;
            _mouseDownLocation = e.GetPosition(this);
            Debug.WriteLine("ShortcutLauncherButton_ContentControl.MouseMove subscribed");
        }


        private void _VSLauncherButton_ContentControl_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as ContentControl).Cursor = Cursors.Arrow;
            Debug.WriteLine("_VSLauncherButton_ContentControl_MouseLeave");
            _timer.AutoReset = false;
            _timer.Start();
        }

        private void _VSLauncherButton_ContentControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("_VSLauncherButton_ContentControl_MouseEnter");
            _timer.Stop();
            _VSLauncherButton_MouseMove(sender, null);
            _VSLauncherButton_PreviewMouseLeftButtonUp(sender, null);
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void RefreshView()
        {
            OnPropertyChanged("WindowHeight");
            OnPropertyChanged("WindowWidth");
            OnPropertyChanged("MainStackPanelWidth");
            OnPropertyChanged("ShortcutLauncherButtonWidth");
            OnPropertyChanged("ShortcutLauncherButtonHeight");
            OnPropertyChanged("MainStackPanelHeight");
            OnPropertyChanged("MainStackPanelShortcutContainerRowHeight");
            OnPropertyChanged("MainStackPanelPinRowHeight");
            OnPropertyChanged("RunAsChecked");
            OnPropertyChanged("SmallSizeChecked");
            OnPropertyChanged("MediumSizeChecked");
            OnPropertyChanged("LargeSizeChecked");
            _ShortcutsContainer.RefreshView();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshView();
        }

        public static string GetShortcutTargetPath(string shortcutFilename)
        {
            string tpath = null;
            string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);
            IWshRuntimeLibrary.IWshShortcut link = null;
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell(); //Create a new WshShell Interface
            try
            {
                link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutFilename);
            }
            catch (Exception)
            { }

            if (link != null)
            {
                tpath = link.TargetPath;
            }

            return string.IsNullOrEmpty(tpath) ? shortcutFilename : tpath;
        }

        public void GetShortcutDetails()
        {
            string pathsStr = Properties.Settings.Default.ShortcutPaths;
            ShortcutsJson shortcuts = JsonUtil.ReadToObject<ShortcutsJson>(pathsStr);
            switch (Properties.Settings.Default.IconSize)
            {
                case 0:
                    UCWithImage.SmallSizeChecked = true;
                    break;
                case 1:
                    UCWithImage.MediumSizeChecked = true;
                    break;
                case 2:
                    UCWithImage.LargeSizeChecked = true;
                    break;
                default:
                    break;
            }

            List<ShortcutJson> itemsToRemove = new List<ShortcutJson>();
            foreach (ShortcutJson item in shortcuts.ShortcutJsonList)
            {
                bool itemadded = false;
                if (!string.IsNullOrEmpty(item.Path) && File.Exists(item.Path))
                    itemadded = _ShortcutsContainer.AddItem(item, this);
                
                if(!itemadded)
                    itemsToRemove.Add(item);
            }
            if (itemsToRemove.Count > 0)
            {
                foreach (ShortcutJson item in itemsToRemove)
                {
                    shortcuts.ShortcutJsonList.Remove(item);
                }
                Properties.Settings.Default.ShortcutPaths = JsonUtil.ReadToString<ShortcutsJson>(shortcuts);
                Properties.Settings.Default.Save();
            }
            RefreshView();
        }

        public void RemoveShortcut(UCWithImage shortcut)
        {
            string pathsStr = Properties.Settings.Default.ShortcutPaths;
            ShortcutsJson shortcuts = JsonUtil.ReadToObject<ShortcutsJson>(pathsStr);

            ShortcutJson shortcutJson = shortcuts.ShortcutJsonList.Find(x => string.Compare(x.Path, shortcut.FilePath, true) == 0);
            shortcuts.ShortcutJsonList.Remove(shortcutJson);
            Properties.Settings.Default.ShortcutPaths = JsonUtil.ReadToString<ShortcutsJson>(shortcuts);
            Properties.Settings.Default.Save();
            _ShortcutsContainer.buttonContainer.Children.Remove(shortcut);
        }

        public void AddShortcut(string filepath)
        {
            string pathsStr = Properties.Settings.Default.ShortcutPaths;
            ShortcutsJson shortcuts = null;
            if (!string.IsNullOrEmpty(pathsStr))
                shortcuts = JsonUtil.ReadToObject<ShortcutsJson>(pathsStr);

            if (shortcuts != null && shortcuts.ShortcutJsonList.Find(x => string.Compare(x.Path, filepath, true) == 0) != null)
                return;

            ShortcutJson item = new ShortcutJson();
            item.Path = filepath;
            item.TargetPath = GetShortcutTargetPath(filepath);
            item.Caption = System.IO.Path.GetFileName(item.TargetPath);

            if (string.IsNullOrEmpty(pathsStr))
            {
                shortcuts = new ShortcutsJson();
            }
            bool itemadded = _ShortcutsContainer.AddItem(item, this);
            if(itemadded)
            {
                shortcuts.ShortcutJsonList.Add(item);
                Properties.Settings.Default.ShortcutPaths = JsonUtil.ReadToString<ShortcutsJson>(shortcuts);
                Properties.Settings.Default.Save();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetShortcutDetails();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuItem_Close_Clicked(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem).Name == "Close1")
            {
                this.Close();
            }
        }

        private void PinUnpinButton_Click(object sender, RoutedEventArgs e)
        {
            _popupPinned = !_popupPinned;
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                _popupPinned ? Properties.Resources.Pin.GetHbitmap() : Properties.Resources.UnPin.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            PinUnpinButton_Image.Source = bitmapSource;
            if (!_mainStackPanelPopup.IsOpen && _popupPinned)
            {
                _mainStackPanelPopup.IsOpen = true;
                _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                _ShortcutsContainer.Opacity = 0.5;
                var increaseOpacityAnim = new DoubleAnimation(1, (Duration)TimeSpan.FromSeconds(0.5));
                _ShortcutsContainer.BeginAnimation(UIElement.OpacityProperty, increaseOpacityAnim);
                RefreshView();
                Debug.WriteLine("mainstackpanel visible: _VSLauncherButton_ContentControl_MouseDoubleClick");
            }
        }

        private void _mainStackPanelPopup_Opened(object sender, EventArgs e)
        {
            this.Height = WindowHeight;
            this.Width = WindowWidth;
            RefreshView();
        }

        private void _mainStackPanelPopup_Closed(object sender, EventArgs e)
        {
            this.Height = WindowHeight;
            this.Width = WindowWidth;
            RefreshView();
        }
    }
}
