using System;
using System.Collections.Generic;
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
    public partial class ShortcutButtonContainer : UserControl
    {
        WrapPanel ButtonContainer
        {
            get
            {
                return buttonContainer;
            }
        }
        public ShortcutButtonContainer()
        {
            InitializeComponent();
        }

        public void AddItem(string filepath, MainWindow parentWindow)
        {
            ButtonWithImage item = new ButtonWithImage();
            var imagesource = ExtractAssociatedIconImage(filepath);
            if (imagesource != null)
            {
                item.ButtonWithImage_ControlImage.Source = imagesource;
                item.FilePath = filepath;
                item.ParentWindow = parentWindow;
                buttonContainer.Children.Add(item);
            }
        }

        private void buttonContainer_DragEnter(object sender, DragEventArgs e)
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

            if(string.IsNullOrEmpty(filename) == false && System.IO.Path.HasExtension(filename) && File.Exists(filename))
            {
                FileInfo fileinfo = new FileInfo(filename);
                e.Effects = DragDropEffects.Copy;
            }
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
                catch (ArgumentException e)
                {
                    return null;
                }

                bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(ico.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            
            return bitmapSource;
        }
    }
}
