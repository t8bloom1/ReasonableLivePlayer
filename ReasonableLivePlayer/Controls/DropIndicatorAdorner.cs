using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ReasonableLivePlayer;

/// <summary>
/// Draws a horizontal white line across the ListBox at the specified Y position
/// to indicate where a dragged item will be dropped.
/// </summary>
public class DropIndicatorAdorner : Adorner
{
    private static readonly Pen LinePen = new(Brushes.White, 2) { DashStyle = DashStyles.Solid };

    public double LineY { get; set; }

    public DropIndicatorAdorner(UIElement adornedElement, double lineY)
        : base(adornedElement)
    {
        LineY = lineY;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = AdornedElement.RenderSize.Width;
        dc.DrawLine(LinePen, new Point(0, LineY), new Point(width, LineY));
    }
}
