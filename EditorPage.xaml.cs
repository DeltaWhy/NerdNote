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
using MarkdownDeep;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace NerdNote
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class EditorPage : NerdNote.Common.LayoutAwarePage
    {
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
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (pageState == null)
            {
                editorBox.Text = "# First note\n\nWrite your first note here!";
            }
            else if (pageState.ContainsKey("NoteText"))
            {
                editorBox.Text = pageState["NoteText"].ToString();
            }

            refreshTimer.Start();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (pageState != null)
            {
                pageState["NoteText"] = editorBox.Text.ToString();
            }
        }

        private async void CompileNote()
        {
            if (tempFile == null)
                tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("test.html", CreationCollisionOption.ReplaceExisting);

            html = md.Transform(editorBox.Text.ToString());

            await FileIO.WriteTextAsync(tempFile, html, Windows.Storage.Streams.UnicodeEncoding.Utf8);

            outputBox.Visibility = Visibility.Visible;
            outputBox.NavigateToString(html);
            ShowWebRect();
        }

        private void editorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            refreshTimer.Stop();
            refreshTimer.Start();
        }

        private void OnTick(object sender, object e)
        {
            CompileNote();
            refreshTimer.Stop();
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
    }
}
