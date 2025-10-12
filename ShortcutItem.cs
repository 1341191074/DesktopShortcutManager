using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DesktopShortcutManager
{
    public class ShortcutItem : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Path { get; set; }

        private ImageSource _icon;
        [JsonIgnore]
        public ImageSource Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        private bool _isEditing;
        [JsonIgnore]
        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Asynchronously loads the icon for this shortcut item.
        /// </summary>
        /// <param name="viewModel">The MainViewModel instance that contains the icon loading logic.</param>
        public async Task LoadIconAsync(MainViewModel viewModel)
        {
            Icon = await viewModel.GetIconForFileAsync(Path);
        }
    }
}