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
        private bool _popupForcefullyOpened = false;
        private bool _ShortcutLauncherControl_MouseEntered = false;
        private System.Threading.SynchronizationContext _callersCtx;
        private MouseEventHandler _mouseEventHandler;
        private Point _mouseDownLocation;
        private bool _runAAdminsChecked;
        Timer _timer;
        public event EventHandler PinStateChanged;
        private DoubleAnimation _increasePopupOpacityAnim;
        private DoubleAnimation _decreasePopupOpacityAnim;
        private DoubleAnimation _increaseShortcutIconOpacityAnim;
        private DoubleAnimation _decreaseShortcutIconOpacityAnim;
        BitmapSource _pinBitmapSource;
        BitmapSource _unpinBitmapSource;

        public event PropertyChangedEventHandler PropertyChanged;
        private bool _contextMenuOpened = false;
        public bool ContextMenuOpened
        {
            get
            {
                return _contextMenuOpened;
            }
        }

        public bool MainStackPanelCursorInside
        {
            get { return _mainStackPanelCursorInside; }
        }

        public bool PopupForcefullyOpened
        {
            get
            {
                return _popupForcefullyOpened;
            }
            set
            {
                _popupForcefullyOpened = value;
                if (!_mainStackPanelPopup.IsOpen && _popupForcefullyOpened)
                {
                    OpenPopup(false);
                }
            }
        }
        public bool PopupPinned
        {
            get
            {
                return _popupPinned;
            }
            set
            {
                _popupPinned = value;
                OnPinUnpinChange();
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
                OnPropertyChanged("RunAsAdminChecked");
                _ShortcutsContainer.RaisePropertyChangedEvent("RunAsAdminChecked");
            }
        }

        public double WindowHeight
        {
            get
            {
                return System.Windows.SystemParameters.WorkArea.Height * 0.08;
            }
        }

        public double WindowWidth
        {
            get
            {
                return System.Windows.SystemParameters.WorkArea.Height * 0.08;
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
                return Math.Max(0, MainStackPanelHeight * 0.05);
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


        public MainWindow()
        {
            InitializeComponent();
            _decreaseShortcutIconOpacityAnim = new DoubleAnimation(0.5, (Duration)TimeSpan.FromSeconds(1));
            _increaseShortcutIconOpacityAnim = new DoubleAnimation(1, (Duration)TimeSpan.FromSeconds(1));
            _decreasePopupOpacityAnim = new DoubleAnimation(0.5, (Duration)TimeSpan.FromSeconds(1));
            _increasePopupOpacityAnim = new DoubleAnimation(1, (Duration)TimeSpan.FromSeconds(1));

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
            ShortcutLauncherControl_Image.Source = bitmapSource;
            var bitmapSource1 = Imaging.CreateBitmapSourceFromHBitmap(_popupPinned ? Properties.Resources.Pin.GetHbitmap() : Properties.Resources.UnPin.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            PinUnpinButton_Image.Source = bitmapSource1;

            this.Title = "Shortcut Launcher";
            this.Loaded += Form1_Load;
            _TopStackPanel.DataContext = this;
            this.SizeChanged += MainWindow_SizeChanged;
            _ShortcutsContainer.Background = Brushes.Black;
            _ShortcutsContainer.Opacity = 0.5;
            _mainStackPanelPopup.PlacementTarget = _TopStackPanel;
            _pinBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.Pin.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            _unpinBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.UnPin.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            _mainStackPanelPopup.MouseLeave += _mainStackPanel_MouseLeave;
            _mainStackPanelPopup.MouseEnter += _mainStackPanel_MouseEnter;
            UCWithImage.ParentContainer = _ShortcutsContainer;

            ShortcutLauncherControl.MouseEnter += ShortcutLauncherControl_MouseEnter;
            ShortcutLauncherControl.MouseLeave += ShortcutLauncherControl_MouseLeave;
            ShortcutLauncherControl.PreviewMouseLeftButtonDown += ShortcutLauncherControl_PreviewMouseLeftButtonDown;
            ShortcutLauncherControl.PreviewMouseLeftButtonUp += ShortcutLauncherControl_PreviewMouseLeftButtonUp;
            _timer = new Timer(1500);
            _timer.Elapsed += (sender1, args) =>
            {
                if (!_mainStackPanelCursorInside)
                {
                    _callersCtx.Post((_) =>
                    {
                        if (!_popupPinned && !_popupForcefullyOpened && !_mainStackPanelCursorInside && !_ShortcutLauncherControl_MouseEntered && !_ShortcutsContainer.IsMouseDirectlyOver && !ShortcutLauncherControl.IsMouseDirectlyOver && _mainStackPanelPopup.IsOpen)
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
            _mouseEventHandler = new MouseEventHandler(ShortcutLauncherControl_MouseMove);

            _mainStackPanelPopup.IsOpen = false;
            _ShortcutsContainer.ParentWindow = this;
        }

        internal void ClearAllShortcuts()
        {
            string pathsStr = Properties.Settings.Default.ShortcutPaths;
            ShortcutsJson shortcuts = JsonUtil.ReadToObject<ShortcutsJson>(pathsStr);
            shortcuts.ShortcutJsonList.Clear();
            Properties.Settings.Default.ShortcutPaths = JsonUtil.ReadToString<ShortcutsJson>(shortcuts);
            Properties.Settings.Default.Save();
            _ShortcutsContainer.RemoveAllItems();
        }

        private void _mainStackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            _mainStackPanelCursorInside = true;
            _timer.Stop();
            if (!_popupPinned && _mainStackPanelPopup.IsOpen && !_ShortcutsContainer.ContextMenuOpened && Math.Abs(_ShortcutsContainer.Opacity - 1.0) > double.Epsilon)
            {
                _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                //_ShortcutsContainer.Opacity = 0.5;
                _ShortcutsContainer.BeginAnimation(UIElement.OpacityProperty, _increasePopupOpacityAnim);
            }
            Debug.WriteLine("_mainStackPanel_MouseEnter");

        }

        internal void _mainStackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            _mainStackPanelCursorInside = false;
            Debug.WriteLine("_mainStackPanel_MouseLeave");
            if (!_popupPinned && !_ShortcutsContainer.ContextMenuOpened && !_popupForcefullyOpened)
            {
                _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                _ShortcutsContainer.BeginAnimation(UIElement.OpacityProperty, _decreasePopupOpacityAnim);
                _timer.AutoReset = false;
                _timer.Start();
            }
        }

        #region ShortcutLauncherControl MouseEvents


        private void ShortcutLauncherControl_MouseMove(object sender, MouseEventArgs e)
        {
            double distance = 0;
            if (e != null)
                distance = Math.Max(distance, Math.Abs(Point.Subtract(e.GetPosition(this), _mouseDownLocation).Length));
            if (Mouse.LeftButton == MouseButtonState.Pressed && distance > 5)
            {
                (sender as ContentControl).Cursor = Cursors.SizeAll;
                if (!_popupPinned && !_popupForcefullyOpened)
                    ClosePopup();

                if (!_animation_is_running)
                {
                    this.BeginAnimation(UIElement.OpacityProperty, null);
                    this.BeginAnimation(UIElement.OpacityProperty, _decreaseShortcutIconOpacityAnim);
                    _animation_is_running = true;
                }

                this.DragMove();
            }
            else
            {
                if (_animation_is_running)
                {
                    this.BeginAnimation(UIElement.OpacityProperty, null);
                    this.BeginAnimation(UIElement.OpacityProperty, _increaseShortcutIconOpacityAnim);
                    _animation_is_running = false;
                }
            }
        }

        private void ShortcutLauncherControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as ContentControl).Cursor = Cursors.Arrow;
            Debug.WriteLine("_VSLauncherButton_PreviewMouseLeftButtonUp");
            ShortcutLauncherControl.MouseMove -= _mouseEventHandler;
            Debug.WriteLine("ShortcutLauncherControl.MouseMove unsubscribed");

            if (!_mainStackPanelPopup.IsOpen)
                OpenPopup(false, 0.5);

            Debug.WriteLine("mainstackpanel visible: _VSLauncherButton_ContentControl_MouseEnter");

        }

        private void ShortcutLauncherControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownLocation = e.GetPosition(this);
            ShortcutLauncherControl.MouseMove += _mouseEventHandler;
            Debug.WriteLine("ShortcutLauncherControl.MouseMove subscribed");
        }


        private void ShortcutLauncherControl_MouseLeave(object sender, MouseEventArgs e)
        {
            _ShortcutLauncherControl_MouseEntered = false;
            (sender as ContentControl).Cursor = Cursors.Arrow;
            Debug.WriteLine("ShortcutLauncherControl_MouseLeave");
            if (!_popupPinned && !_contextMenuOpened && !_popupForcefullyOpened)
            {
                _timer.AutoReset = false;
                _timer.Start();
            }
        }

        private void ShortcutLauncherControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("ShortcutLauncherControl_MouseEnter");
            _ShortcutLauncherControl_MouseEntered = true;
            _timer.Stop();
            if (!_mainStackPanelPopup.IsOpen)
                OpenPopup(false, 0.5);
        }
        #endregion

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
            OnPropertyChanged("RunAsAdminChecked");
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

                if (!itemadded)
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
            _ShortcutsContainer.RemoveItem(shortcut);
        }

        public void ReorderShortcut(UCWithImage sourceObj, UCWithImage destinationObj)
        {
            string pathsStr = Properties.Settings.Default.ShortcutPaths;
            ShortcutsJson shortcuts = JsonUtil.ReadToObject<ShortcutsJson>(pathsStr);
            ShortcutJson sourceJson = shortcuts.ShortcutJsonList.Find(x => string.Compare(x.Path, sourceObj.FilePath, true) == 0);
            int newIndex = _ShortcutsContainer.ReorderShortcut(sourceObj, destinationObj);
            shortcuts.ShortcutJsonList.Remove(sourceJson);
            shortcuts.ShortcutJsonList.Insert(newIndex, sourceJson);
            Properties.Settings.Default.ShortcutPaths = JsonUtil.ReadToString<ShortcutsJson>(shortcuts);
            Properties.Settings.Default.Save();
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
            if (itemadded)
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

        private void MenuItem_Close_Clicked(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem).Name == "Close1")
            {
                this.Close();
            }
        }

        public void OpenPopup(bool withAnimation = true, double newOpacity = 1)
        {
            if (!_mainStackPanelPopup.IsOpen)
            {
                _mainStackPanelPopup.IsOpen = true;
                _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                if (withAnimation)
                {
                    _ShortcutsContainer.Opacity = 0.5;
                    var increaseOpacityAnim = new DoubleAnimation(newOpacity, (Duration)TimeSpan.FromSeconds(0.5));
                    _ShortcutsContainer.BeginAnimation(UIElement.OpacityProperty, increaseOpacityAnim);
                }
                else
                    _ShortcutsContainer.Opacity = newOpacity;

                RefreshView();
                Debug.WriteLine("mainstackpanel visible: OpenPopup()");
            }
        }

        public void ClosePopup()
        {
            if (_mainStackPanelPopup.IsOpen)
            {
                _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                _ShortcutsContainer.Opacity = 0.5;
                _mainStackPanelPopup.IsOpen = false;
                RefreshView();
            }
            Debug.WriteLine("mainstackpanel visibility collapsed: ClosePopup()");
        }

        public void OnPinUnpinChange()
        {
            PinUnpinButton_Image.Source = _popupPinned ? _pinBitmapSource : _unpinBitmapSource;
            if (!_mainStackPanelPopup.IsOpen && _popupPinned)
            {
                OpenPopup();
                Debug.WriteLine("mainstackpanel visible: OnPinUnpinChange()");
            }
            PinStateChanged.Invoke(this, new EventArgs());
        }

        public void PinUnpinButton_Click(object sender, RoutedEventArgs e)
        {
            PopupPinned = !_popupPinned;
            _ShortcutsContainer.RaisePropertyChangedEvent("PinContextMenuText");
            if (!_popupPinned && !MainStackPanelCursorInside)
            {
                _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                _ShortcutsContainer.Opacity = 0.5;
            }

            if (!_popupPinned)
            {
                _timer.AutoReset = false;
                _timer.Start();
            }
        }

        private void ShortcutLauncherControl_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _contextMenuOpened = true;
            PopupForcefullyOpened = true;
            if (_mainStackPanelPopup.IsOpen)
            {
                _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                _ShortcutsContainer.Opacity = 1;
            }
        }

        public void ResetAndStartTimer()
        {
            _timer.AutoReset = false;
            _timer.Start();
        }

        private void ShortcutLauncherControl_ContextMenuClosed(object sender, RoutedEventArgs e)
        {
            _contextMenuOpened = false;
            if (!_ShortcutsContainer.ContextMenuOpened)
            {
                PopupForcefullyOpened = false;
                if (!MainStackPanelCursorInside && !_ShortcutLauncherControl_MouseEntered)
                {
                    if (!_popupPinned)
                    {
                        _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                        _ShortcutsContainer.Opacity = 0.5;
                    }
                    _timer.AutoReset = false;
                    _timer.Start();
                }
                else if (_mainStackPanelPopup.IsOpen && !MainStackPanelCursorInside && _ShortcutLauncherControl_MouseEntered && !_popupPinned)
                {
                    _ShortcutsContainer.BeginAnimation(OpacityProperty, null);
                    _ShortcutsContainer.Opacity = 0.5;
                }
            }
        }

    }
}
