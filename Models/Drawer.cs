// C#: Drawer.cs
using DesktopShortcutManager.Utils;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DesktopShortcutManager.Models
{
    // 继承ObservableObject以获得通知能力
    public class Drawer : ObservableObject
    {
        private bool _isEditing;
        [JsonIgnore]
        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }
        // Json序列化器需要一个无参构造函数
        public Drawer()
        {
            _name = "New Drawer";
            Items = new ObservableCollection<ShortcutItem>();

        }
        private bool _isDropTarget;
        [JsonIgnore] 
        public bool IsDropTarget
        {
            get => _isDropTarget;
            set { _isDropTarget = value; OnPropertyChanged(); }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ShortcutItem> Items { get; set; }

        private bool _isExpanded = true; // 默认为展开状态
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        // 为了方便，我们可以加一个带名字的构造函数
        [JsonConstructor]
        public Drawer(string name) : this()
        {
            _name = name;
        }
    }
}