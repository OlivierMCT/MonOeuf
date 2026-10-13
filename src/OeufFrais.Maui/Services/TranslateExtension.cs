using Microsoft.Maui.Controls.Xaml;

namespace OeufFrais.Services;

/// <summary>Utilisation en XAML : Text="{loc:Translate Key=Main_Titre}".</summary>
[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
        => new Binding($"[{Key}]", BindingMode.OneWay, source: LocalizationResourceManager.Instance);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
