using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Cribbage
{
    public static class GridExtensions
    {
        public static double PixelWidth(this Grid grid, int col, int colSpan)
        {
            double width = 0;
            for (var i = 0; i < colSpan; i++) width += grid.ColumnDefinitions[i + col].ActualWidth;

            return width;
        }

        public static double PixelWidth(this Grid grid, FrameworkElement el)
        {
            var col = Grid.GetColumn(el);
            var colSpan = Grid.GetColumnSpan(el);
            return grid.PixelWidth(col, colSpan);
        }

        public static double PixelHeight(this Grid grid, int row, int rowSpan)
        {
            double height = 0;
            for (var i = 0; i < rowSpan; i++) height += grid.RowDefinitions[i + row].ActualHeight;

            return height;
        }

        public static double PixelHeight(this Grid grid, FrameworkElement el)
        {
            var row = Grid.GetRow(el);
            var rowSpan = Grid.GetRowSpan(el);
            return grid.PixelHeight(row, rowSpan);
        }
    }
}