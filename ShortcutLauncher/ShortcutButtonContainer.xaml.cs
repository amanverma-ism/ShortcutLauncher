using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShortcutLauncher
{
    /// <summary>
    /// Interaction logic for VisualStudioButtonContainer.xaml
    /// </summary>
    public partial class ShortcutButtonContainer : UserControl, INotifyPropertyChanged
    {
        private MainWindow _mainWindow;
        private bool _messageBoxShowing = false;
        private bool _contextMenuOpened = false;
        public bool ContextMenuOpened
        {
            get
            {
                return _contextMenuOpened;
            }
        }
        WrapPanel ButtonContainer
        {
            get
            {
                return buttonContainer;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow ParentWindow
        {
            get
            {
                return _mainWindow;
            }
            set
            {
                _mainWindow = value;
                _mainWindow.PinStateChanged += ParentWindow_PinStateChanged;
            }
        }

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
                return _mainWindow != null ? _mainWindow.RunAsAdminChecked : Properties.Settings.Default.RunAsAdminChecked;
            }
            set
            {
                if (_mainWindow != null)
                    _mainWindow.RunAsAdminChecked = value;
                OnPropertyChanged("RunAsAdminChecked");
            }
        }

        public double ContainerHeight
        {
            get
            {
                return ParentWindow.MainStackPanelShortcutContainerRowHeight;
            }
        }
        public double ContainerWidth
        {
            get
            {
                return ParentWindow.MainStackPanelWidth;
            }
        }

        public string PinContextMenuText
        {
            get
            {
                return (PinContextMenuChecked ? "UnPin" : "Pin");
            }
        }

        public bool PinContextMenuChecked
        {
            get
            {
                bool popupPinned = false;
                if (_mainWindow != null)
                {
                    popupPinned = _mainWindow.PopupPinned;
                }
                return popupPinned;
            }
            set
            {
                if (_mainWindow != null)
                {
                    _mainWindow.PopupPinned = value;
                }
            }
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ShortcutButtonContainer()
        {
            InitializeComponent();
            this.DataContext = this;
            _mainGrid.DataContext = this;
            buttonContainer.DataContext = this;
            this.DragEnter += ButtonContainer_DragEnter;
            this.DragOver += ButtonContainer_DragOver;
            this.Drop += ButtonContainer_Drop;
        }

        private void ParentWindow_PinStateChanged(object sender, EventArgs e)
        {
            this.AllowDrop = ParentWindow.PopupPinned;
        }

        internal void RaisePropertyChangedEvent(string str)
        {
            OnPropertyChanged(str);
        }

        public void RefreshView()
        {
            OnPropertyChanged("ContainerHeight");
            OnPropertyChanged("ContainerWidth");
            OnPropertyChanged("PinContextMenuText");
            OnPropertyChanged("PinContextMenuChecked");
            OnPropertyChanged("SmallSizeChecked");
            OnPropertyChanged("MediumSizeChecked");
            OnPropertyChanged("RunAsAdminChecked");
            OnPropertyChanged("LargeSizeChecked");
            foreach (UCWithImage buttonWithImage in buttonContainer.Children)
            {
                buttonWithImage.RefreshView();
            }
        }

        private void ButtonContainer_Drop(object sender, DragEventArgs e)
        {
            string filename = string.Empty;

            if ((e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = e.Data.GetData("FileName") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                    }
                }
            }

            if (string.IsNullOrEmpty(filename) == false && System.IO.Path.HasExtension(filename) && File.Exists(filename))
            {
                FileInfo fileinfo = new FileInfo(filename);
                ParentWindow.AddShortcut(filename);
            }
        }

        private void ButtonContainer_DragOver(object sender, DragEventArgs e)
        {
            string filename = string.Empty;

            if ((e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = e.Data.GetData("FileName") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                    }
                }
            }

            if (string.IsNullOrEmpty(filename) == false && System.IO.Path.HasExtension(filename) && File.Exists(filename))
            {
                FileInfo fileinfo = new FileInfo(filename);
                e.Effects = DragDropEffects.Copy;
            }
        }

        private void ButtonContainer_DragEnter(object sender, DragEventArgs e)
        {
            string filename = string.Empty;

            if ((e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = e.Data.GetData("FileName") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                    }
                }
            }

            if (string.IsNullOrEmpty(filename) == false && System.IO.Path.HasExtension(filename) && File.Exists(filename))
            {
                FileInfo fileinfo = new FileInfo(filename);
                e.Effects = DragDropEffects.Copy;
            }
        }

        public void RemoveItem(UCWithImage item)
        {
            buttonContainer.Children.Remove(item);
        }

        public void RemoveAllItems()
        {
            buttonContainer.Children.Clear();
        }

        public bool AddItem(ShortcutJson jsonObj, MainWindow parentWindow)
        {
            bool itemadded = false;
            try
            {
                UCWithImage item = new UCWithImage();
                var imagesource = ExtractAssociatedIconImage(jsonObj.Path);
                if (imagesource != null)
                {
                    item.ButtonWithImage_ControlImage.Source = imagesource;
                    item.FilePath = jsonObj.Path;
                    item.TargetPath = jsonObj.TargetPath;
                    item.CaptionEditor.Text = jsonObj.Caption;
                    item.ParentWindow = parentWindow;
                    buttonContainer.Children.Add(item);
                    itemadded = true;
                }
            }
            catch (Exception)
            {

            }
            return itemadded;
        }

        private BitmapSource ExtractAssociatedIconImage(string path)
        {
            Icon ico = null;
            string ext = System.IO.Path.GetExtension(path);
            BitmapSource bitmapSource = null;
            if (ext != null && (string.Compare(ext, ".lnk", true) == 0 || string.Compare(ext, ".exe", true) == 0 || string.Compare(ext, ".bat", true) == 0))
            {
                try
                {
                    ico = Icon.ExtractAssociatedIcon(@path);
                }
                catch (ArgumentException)
                {
                    return null;
                }

                bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(ico.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            return bitmapSource;
        }

        public int ReorderShortcut(UCWithImage sourceObj, UCWithImage destinationObj)
        {
            int sourceIndex = buttonContainer.Children.IndexOf(sourceObj);
            int destinationIndex = buttonContainer.Children.IndexOf(destinationObj);
            buttonContainer.Children.Remove(sourceObj);
            buttonContainer.Children.Insert(destinationIndex, sourceObj);
            return destinationIndex;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.PopupForcefullyOpened = true;
            _messageBoxShowing = true;
            MessageBoxResult res = MessageBox.Show(ParentWindow, "Are you sure you want to clear all the shortcuts?", "Confirmation Dialog", MessageBoxButton.YesNo);
            ParentWindow.PopupForcefullyOpened = false;
            _messageBoxShowing = false;
            if (res == MessageBoxResult.Yes)
            {
                ParentWindow.ClearAllShortcuts();
            }
            if (!ParentWindow.MainStackPanelCursorInside)
            {
                ParentWindow.ResetAndStartTimer();
            }
        }

        private void ShortcutContainer_ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            _contextMenuOpened = false;
            System.Diagnostics.Debug.WriteLine("PinContextMenuChecked = " + PinContextMenuChecked.ToString());
            if (!_messageBoxShowing && !ParentWindow.ContextMenuOpened)
            {
                ParentWindow.PopupForcefullyOpened = false;
                if(!ParentWindow.MainStackPanelCursorInside && !PinContextMenuChecked)
                    ParentWindow.ResetAndStartTimer();
            }
        }
        private void ShortcutContainer_ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _contextMenuOpened = true;
            ParentWindow.PopupForcefullyOpened = true;
        }

        private void PinUnPin_Click(object sender, RoutedEventArgs e)
        {
            PinContextMenuChecked = !PinContextMenuChecked;
            OnPropertyChanged("PinContextMenuText");
        }
    }
}
