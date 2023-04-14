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
                return (((((Parent as Grid).Parent as System.Windows.Controls.Primitives.Popup).Parent as StackPanel).Parent as Grid).Parent as MainWindow);
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
            this.AllowDrop = true;
            this.DragEnter += ButtonContainer_DragEnter;
            this.DragOver += ButtonContainer_DragOver;
            this.Drop += ButtonContainer_Drop;
            this.MouseEnter += ShortcutButtonContainer_MouseEnter;
            this.MouseLeave += ShortcutButtonContainer_MouseLeave;
        }

        private void ShortcutButtonContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Opacity = 0.5;
        }

        private void ShortcutButtonContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Opacity = 1;
        }

        public void RefreshView()
        {
            OnPropertyChanged("ContainerHeight");
            OnPropertyChanged("ContainerWidth");
            foreach(UCWithImage buttonWithImage in buttonContainer.Children)
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
                    item.RefreshView();
                }
            }
            catch(Exception)
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
    }
}
