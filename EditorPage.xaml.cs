using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.ViewManagement;
using MarkdownDeep;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace NerdNote
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class EditorPage : NerdNote.Common.LayoutAwarePage
    {
        private StorageFile file;
        private StorageFile tempFile;
        private DispatcherTimer refreshTimer = new DispatcherTimer();
        private Markdown md = new Markdown();
        private WebViewBrush webBrush;
        private string html = "";
        
        public EditorPage()
        {
            this.InitializeComponent();
            refreshTimer.Tick += new System.EventHandler<object>(this.OnTick);
            refreshTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
        }


        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("MRUToken"))
            {
                LoadFile(await Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(ApplicationData.Current.LocalSettings.Values["MRUToken"].ToString()));
            }
            else
            {
                LoadFile(new Uri("ms-appx:///Help/Sample.md"));
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private async void LoadFile(StorageFile file)
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values["MRUToken"] = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                this.file = file;
                string src = await FileIO.ReadTextAsync(file);
                editorBox.Text = src;
                CompileNote();
            }
            catch (Exception e)
            {
                file = null;
                editorBox.Text = "An error occured loading this file.";
                CompileNote();
            }
        }

        private async void LoadFile(Uri uri)
        {
            LoadFile(await StorageFile.GetFileFromApplicationUriAsync(uri));
        }

        private async void LoadFile(string str)
        {
            LoadFile(new Uri(str));
        }

        private async void CompileNote()
        {
            /*if (tempFile == null)
                tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("test.html", CreationCollisionOption.ReplaceExisting);*/

            html = "<html><head><link href=\"ms-appx-web:///Assets/Style.css\" rel=\"stylesheet\" type=\"text/css\"/></head>" +
                "<body>" + md.Transform(editorBox.Text.ToString()) + "</body></html>";

            //await FileIO.WriteTextAsync(tempFile, html, Windows.Storage.Streams.UnicodeEncoding.Utf8);

            outputBox.Visibility = Visibility.Visible;
            outputBox.NavigateToString(html);
            ShowWebRect();
        }

        private void editorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            refreshTimer.Stop();
            refreshTimer.Start();
        }

        private async void OnTick(object sender, object e)
        {
            refreshTimer.Stop();
            try
            {
                await FileIO.WriteTextAsync(file, editorBox.Text.ToString());
            }
            catch (System.UnauthorizedAccessException ex)
            {
                //do something about it
                return;
            }
            CompileNote();
        }

        private void ShowWebRect()
        {
            if (webBrush == null)
            {
                webBrush = new WebViewBrush();
                webBrush.SetSource(outputBox);
                outputRect.Fill = webBrush;
            }
            webBrush.Redraw();
            outputBox.Visibility = Visibility.Collapsed;
        }

        private void ShowWebView()
        {
            outputBox.Visibility = Visibility.Visible;
        }

        private void outputRect_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ShowWebView();
        }

        private async void NewFile(object sender, RoutedEventArgs e)
        {
            bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            if (!unsnapped)
            {
                return;
            }
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Markdown", new List<string>() { ".md" });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Note";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                await FileIO.WriteTextAsync(file, "");
                // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    LoadFile(file);
                }
                else
                {
                }
            }
            else
            {
            }
        }

        private async void OpenFile(object sender, RoutedEventArgs e)
        {
            bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            if (!unsnapped)
            {
                return;
            }

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".md");
            openPicker.FileTypeFilter.Add(".mkd");
            openPicker.FileTypeFilter.Add(".markdown");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                LoadFile(file);
                // Application now has read/write access to the picked file
                ApplicationData.Current.LocalSettings.Values["MRUToken"] = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                this.file = file;
                string src = await FileIO.ReadTextAsync(file);
                editorBox.Text = src;
                CompileNote();
            }
            else
            {
            }
        }

        private void RenameFile(object sender, RoutedEventArgs e)
        {

        }
    }
}
