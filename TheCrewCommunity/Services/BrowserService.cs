using Microsoft.JSInterop;

namespace TheCrewCommunity.Services;

public class BrowserService
{
    private readonly IJSRuntime _jsRuntime;
    public event Action<int, int>? OnResize;

    public BrowserService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<int> GetWindowWidth()
    {
        return await _jsRuntime.InvokeAsync<int>("eval", "window.innerWidth");
    }

    public async ValueTask<int> GetWindowHeight()
    {
        return await _jsRuntime.InvokeAsync<int>("eval", "window.innerHeight");
    }

    public async ValueTask InitializeResizeListener()
    {
        await _jsRuntime.InvokeVoidAsync("blazorHelpers.registerResizeCallback", 
            DotNetObjectReference.Create(this));
    }

    [JSInvokable]
    public void HandleWindowResize(int width, int height)
    {
        OnResize?.Invoke(width, height);
    }

}