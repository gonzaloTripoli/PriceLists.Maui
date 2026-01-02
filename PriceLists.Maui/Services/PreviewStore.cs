using PriceLists.Core.Models;

namespace PriceLists.Maui.Services;

public class PreviewStore
{
    public ImportPreview? Current { get; private set; }

    public void SetPreview(ImportPreview preview)
    {
        Current = preview;
    }

    public ImportPreview? TakePreview()
    {
        var preview = Current;
        Current = null;
        return preview;
    }
}
