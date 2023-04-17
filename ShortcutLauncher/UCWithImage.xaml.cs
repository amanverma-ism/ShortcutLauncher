using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShortcutLauncher
{
    /// <summary>
    /// Interaction logic for ButtonWithImage.xaml
    /// </summary>
    public partial class UCWithImage : UserControl, INotifyPropertyChanged
    {
        private static bool _smallSizeChecked = false;
        private static bool _mediumSizeChecked = false;
        private static bool _largeSizeChecked = false;
        public event PropertyChangedEventHandler PropertyChanged;
        private Visibility _textBoxVisibility = Visibility.Collapsed;
        private Visibility _textBlockVisibility = Visibility.Visible;
        private MainWindow _mainWindow;

        public Visibility TextBoxVisible
        {
            get
            {
                return _textBoxVisibility;
            }
            set
            {
                if (_textBoxVisibility != value)
                {
                    _textBoxVisibility = value;
                    _textBlockVisibility = value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    OnPropertyChanged("TextBlockVisible");
                    OnPropertyChanged("TextBoxVisible");
                    System.Threading.SynchronizationContext.Current.Post((_) =>
                    {
                        if (value == Visibility.Visible)
                        {
                            ParentWindow.Activate();
                            CaptionEditor.Focus();
                            Keyboard.Focus(CaptionEditor);
                            CaptionEditor.ScrollToEnd();
                            CaptionEditor.SelectionStart = CaptionEditor.Text.Length;
                            CaptionEditor.SelectionLength = 0;
                        }
                    }, null);
                }
            }
        }

        public double ControlHeight
        {
            get
            {
                double height = this.Height;
                if (_smallSizeChecked == true)
                {
                    height = Math.Max(0, (ParentContainer.ActualHeight / 5.0) + ((ParentContainer.ActualHeight / 5.0) * 0.35));
                }
                else if (_mediumSizeChecked == true)
                {
                    height = Math.Max(0, (ParentContainer.ActualHeight / 4.0) + ((ParentContainer.ActualHeight / 4.0) * 0.35));
                }
                else if (_largeSizeChecked == true)
                {
                    height = Math.Max(0, (ParentContainer.ActualHeight / 3.0) + ((ParentContainer.ActualHeight / 3.0) * 0.35));
                }
                return height;
            }
        }
        public double ControlWidth
        {
            get
            {
                double width = this.Width;

                if (_smallSizeChecked == true)
                {
                    width = Math.Max(0, ParentContainer.ActualHeight / 5.0);
                }
                else if (_mediumSizeChecked == true)
                {
                    width = Math.Max(0, ParentContainer.ActualHeight / 3.5);
                }
                else if (_largeSizeChecked == true)
                {
                    width = Math.Max(0, ParentContainer.ActualHeight / 3.0);
                }
                return width;
            }
        }
        public Visibility TextBlockVisible
        {
            get
            {
                return _textBlockVisibility;
            }
            set
            {
                if (_textBlockVisibility != value)
                {
                    _textBlockVisibility = value;
                    _textBoxVisibility = value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    OnPropertyChanged("TextBoxVisible");
                    OnPropertyChanged("TextBlockVisible");
                }
            }
        }
        public static bool SmallSizeChecked
        {
            get
            {
                return _smallSizeChecked;
            }
            set
            {
                if (_smallSizeChecked != true)
                {
                    _smallSizeChecked = true;
                    _mediumSizeChecked = false;
                    _largeSizeChecked = false;
                    Properties.Settings.Default.IconSize = 0;
                    Properties.Settings.Default.Save();
                    foreach (UCWithImage item in (ParentContainer.buttonContainer).Children)
                    {
                        item.RefreshView();
                    }
                }
            }
        }
        public static bool MediumSizeChecked
        {
            get
            {
                return _mediumSizeChecked;
            }
            set
            {
                if (_mediumSizeChecked != true)
                {
                    _mediumSizeChecked = true;
                    _largeSizeChecked = false;
                    _smallSizeChecked = false;
                    Properties.Settings.Default.IconSize = 1;
                    Properties.Settings.Default.Save();
                    foreach (UCWithImage item in (ParentContainer.buttonContainer).Children)
                    {
                        item.RefreshView();
                    }
                }
            }
        }
        public static bool LargeSizeChecked
        {
            get
            {
                return _largeSizeChecked;
            }
            set
            {
                if (_largeSizeChecked != true)
                {
                    _largeSizeChecked = true;
                    _mediumSizeChecked = false;
                    _smallSizeChecked = false;
                    Properties.Settings.Default.IconSize = 2;
                    Properties.Settings.Default.Save();
                    foreach (UCWithImage item in (ParentContainer.buttonContainer).Children)
                    {
                        item.RefreshView();
                    }
                }
            }
        }

        public double CaptionFontSize
        {
            get
            {
                if (_largeSizeChecked)
                    return Math.Max(10, TextRowHeight * 0.75);
                else if (_mediumSizeChecked)
                    return Math.Max(10, TextRowHeight * 0.7);
                else
                    return Math.Max(10, TextRowHeight * 0.68);
            }
        }

        public double XFontSize
        {
            get
            {

                return Math.Max(10, double.IsNaN((12 / 17.5) * (DeleteButtonCornerRadius * 2.0)) ? 10 : ((12 / 17.5) * (DeleteButtonCornerRadius * 2.0)));
            }
        }
        public double DeleteButtonCornerRadius
        {
            get
            {
                return Math.Max(0.1, DeleteRowHeight / 2.0);
            }
        }
        public double TextBlockCornerRadius
        {
            get
            {
                return Math.Max(0.1, (TextRowHeight / 2) );
            }
        }
        public double DeleteRowHeight
        {
            get
            {
                return Math.Max(0, this.Width * 0.15);
            }
        }
        public double TextRowHeight
        {
            get
            {
                return Math.Max(0, this.Width * 0.2);
            }
        }
        public double ImageRowHeight
        {
            get
            {
                return Math.Max(0, this.Width - (this.Width * 0.35));
            }
        }
        public string FilePath { get; set; }
        public string TargetPath { get; set; }
        public MainWindow ParentWindow
        {
            get
            {
                return _mainWindow;
            }
            set
            {
                _mainWindow = value;
                _mainWindow.PinStateChanged += _mainWindow_PinStateChanged;
            }
        }

        private void _mainWindow_PinStateChanged(object sender, EventArgs e)
        {
            this.AllowDrop = _mainWindow.PopupPinned;
        }

        public static ShortcutButtonContainer ParentContainer { get; set; }
        private bool _mouseClicked;
        public UCWithImage()
        {
            _mouseClicked = false;
            InitializeComponent();
            this.DataContext = this;
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void RefreshView()
        {

            OnPropertyChanged("ControlHeight");
            OnPropertyChanged("ControlWidth");
            OnPropertyChanged("TextRowHeight");
            OnPropertyChanged("ImageRowHeight");
            OnPropertyChanged("DeleteRowHeight");
            OnPropertyChanged("DeleteButtonCornerRadius");
            OnPropertyChanged("TextBlockVisible");
            OnPropertyChanged("TextBoxVisible");
            OnPropertyChanged("TextBlockCornerRadius");
            OnPropertyChanged("XFontSize");
            OnPropertyChanged("CaptionFontSize");
        }

        private void ShortcutClicked()
        {
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(string.IsNullOrEmpty(TargetPath) ? @FilePath : @TargetPath);
            if (ParentWindow.RunAsAdminChecked)
            {
                info.UseShellExecute = true;
                info.Verb = "runas";
                try
                {
                    System.Diagnostics.Process.Start(info);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            else
            {

                info.UseShellExecute = string.IsNullOrEmpty(TargetPath) ? true : false;
                info.Verb = "";
                System.Diagnostics.Process.Start(info);
            }
        }

        private void LargeSize_Checked(object sender, RoutedEventArgs e)
        {
            LargeSizeChecked = true;
        }

        private void MediumSize_Checked(object sender, RoutedEventArgs e)
        {
            MediumSizeChecked = true;
        }

        private void SmallSize_Checked(object sender, RoutedEventArgs e)
        {
            SmallSizeChecked = true;
        }

        private void Caption_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBoxVisible = Visibility.Visible;
        }

        private void Caption_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                TextBoxVisible = Visibility.Collapsed;
                CaptionEditor_TextChanged();
            }
        }

        private void ButtonWithImage_ControlImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _mouseClicked = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_mouseClicked && e.LeftButton == MouseButtonState.Pressed && ParentWindow.PopupPinned)
            {
                // Package the data.
                DataObject data = new DataObject();
                data.SetData("UCWithImageObject", this);

                // Initiate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            // If the DataObject contains string data, extract it.
            if (e.Data.GetDataPresent("UCWithImageObject") && ParentWindow.PopupPinned)
            {
                UCWithImage obj = e.Data.GetData("UCWithImageObject") as UCWithImage;
                if(obj != null)
                {
                    e.Effects = DragDropEffects.Move;
                    ParentWindow.ReorderShortcut(obj, this);
                }
            }
            e.Handled = true;
        }

        private void ButtonWithImage_ControlImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseClicked && e.ChangedButton == MouseButton.Left)
            {
                ShortcutClicked();
            }
        }

        private void ButtonWithImage_ControlImage_MouseLeave(object sender, MouseEventArgs e)
        {
            _mouseClicked = false;
        }

        private void CaptionEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxVisible = Visibility.Collapsed;
            CaptionEditor_TextChanged();
        }

        public void CaptionEditor_TextChanged()
        {
            if (this.FilePath != null)
            {
                string pathsStr = Properties.Settings.Default.ShortcutPaths;
                ShortcutsJson shortcuts = null;
                if (!string.IsNullOrEmpty(pathsStr))
                    shortcuts = JsonUtil.ReadToObject<ShortcutsJson>(pathsStr);
                ShortcutJson Obj = shortcuts.ShortcutJsonList.Find(x => string.Compare(x.Path, this.FilePath, true) == 0);
                Obj.Caption = CaptionEditor.Text;
                Properties.Settings.Default.ShortcutPaths = JsonUtil.ReadToString<ShortcutsJson>(shortcuts);
                Properties.Settings.Default.Save();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.RemoveShortcut(this);
        }
    }
}
