using System.Windows;

namespace DesktopShortcutManager
{
    // 这是一个标准的WPF辅助类，用于解决在无法直接访问DataContext的元素（如ContextMenu）中进行数据绑定的问题。
    // 它继承自Freezable，因此可以被放置在资源字典中。
    public class BindingProxy : Freezable
    {
        // 重写Freezable的标准方法
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        // 定义一个名为"Data"的依赖属性，我们将用它来“持有”我们的DataContext
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        public object Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
    }
}