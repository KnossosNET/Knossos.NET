using Avalonia;
using Avalonia.Controls;
using System.Linq;

namespace Knossos.NET.Views
{
    public partial class ModLocalFilesView : UserControl
    {
        public ModLocalFilesView()
        {
            InitializeComponent();
            OpenModButton.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsVisibleProperty) UpdateLayout();
            };
            SizeChanged += (_, _) => UpdateLayout();
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            var width = Bounds.Width;
            PageLayout.Margin = width < 540 ? new Thickness(12, 16, 0, 16) : new Thickness(24, 20, 12, 20);
            ArrangeCards(StorageCards, width >= 960 ? 4 : width >= 500 ? 2 : 1);
            ArrangeCards(FileActions, width >= 1140 ? 5 : width >= 700 ? 3 : width >= 480 ? 2 : 1);
        }

        private static void ArrangeCards(Grid grid, int columns)
        {
            var items = grid.Children.Where(c => c.IsVisible).ToArray();
            columns = System.Math.Min(columns, items.Length);
            if (columns == 0) return;
            var rows = (items.Length + columns - 1) / columns;
            if (grid.ColumnDefinitions.Count != columns)
            {
                grid.ColumnDefinitions.Clear();
                for (int i = 0; i < columns; i++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }
            if (grid.RowDefinitions.Count != rows)
            {
                grid.RowDefinitions.Clear();
                for (int i = 0; i < rows; i++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
            for (int i = 0; i < items.Length; i++)
            {
                Grid.SetColumn(items[i], i % columns);
                Grid.SetRow(items[i], i / columns);
            }
        }
    }
}
