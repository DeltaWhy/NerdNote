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

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace NerdNote
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class FolderView : NerdNote.Common.LayoutAwarePage
    {
        private StorageFolder documents = KnownFolders.DocumentsLibrary;
        private StorageFolder notes;
        private StorageFolder current;

        public FolderView()
        {
            this.InitializeComponent();
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
            notes = await documents.CreateFolderAsync("NerdNote", CreationCollisionOption.OpenIfExists);
            if (pageState != null && pageState.ContainsKey("Path"))
            {
                current = await StorageFolder.GetFolderFromPathAsync(pageState["Path"].ToString());
            }
            else
            {
                current = notes;
            }
            RefreshGrid();
        }

        private void itemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            IStorageItem item = (IStorageItem)e.ClickedItem;
            if (item.IsOfType(StorageItemTypes.Folder))
            {
                current = (StorageFolder)item;
                RefreshGrid();
            }
        }

        private async void RefreshGrid()
        {
            IReadOnlyList<StorageFolder> folders = await current.GetFoldersAsync();
            IReadOnlyList<StorageFile> files = await current.GetFilesAsync();
            IEnumerable<IStorageItem> contents = folders.Concat<IStorageItem>(files);

            itemGridView.ItemsSource = contents;
        }
    }
}
