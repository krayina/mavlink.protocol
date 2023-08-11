using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace TerritoryDefence.UI.Toolkit.Extensions;

public static class FrameworkElementExtensions
{
	public static TranslateTransform InitializeTransform(this FrameworkElement element)
	{
		var renderTransform = element.RenderTransform as TranslateTransform;

		if (renderTransform is null)
		{
			renderTransform = new TranslateTransform();
			element.RenderTransform = renderTransform;
		}
		return renderTransform;
	}
}
