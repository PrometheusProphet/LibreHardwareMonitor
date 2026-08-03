// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ComputerVitals.Wpf;

public sealed class SelectableText : TextBox
{
    static SelectableText()
    {
        TextProperty.OverrideMetadata(typeof(SelectableText), new FrameworkPropertyMetadata(string.Empty));
    }

    public SelectableText()
    {
        IsReadOnly = true;
        IsUndoEnabled = false;
        IsTabStop = false;
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Background = Brushes.Transparent;
        Foreground = Brushes.White;
        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        CaretBrush = Brushes.Transparent;
        FocusVisualStyle = null;
        Cursor = Cursors.IBeam;
    }
}
