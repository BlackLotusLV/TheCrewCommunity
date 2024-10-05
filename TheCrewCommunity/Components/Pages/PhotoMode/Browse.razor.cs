using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.JSInterop;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Components.Pages.PhotoMode;

public partial class Browse : ComponentBase
{
    private ElementReference _loadMoreButton;
    private const int InitialLoadSize = 50;
    private bool _isLoading;
    private const uint ImageLoadSize = 30;
    private uint _currentLoadEnd;

    private UserImage[] _images = [];
    private UserImage[] _unfilteredImages = [];
    private Game[]? _games = [];
    private Guid _selectedGameId;
    private SortMode _selectedSortMode = SortMode.New;

    protected override async Task OnInitializedAsync()
    {
        LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        _unfilteredImages = dbContext.UserImages
            .Include(x=>x.Game)
            .Include(x=>x.ImageLikes)
            .OrderByDescending(x => x.UploadDateTime).ToArray();

        _games = await Cache.GetOrCreateAsync("GameOptionsKey", async _ => await dbContext.Games.ToArrayAsync());
        _images = _unfilteredImages;

        SetCurrentLoadEnd();
    }

    private async Task OnGameSelected(ChangeEventArgs e)
    {
        if (e.Value is null)
        {
            Logger.LogDebug("Value of selected object is null. Stopping process");
            return;
        }
        _selectedGameId = Guid.Parse(e.Value.ToString() ?? string.Empty);
        await ApplyFilterAsync();
    }

    private async Task OnSortModeSelectedAsync(ChangeEventArgs e)
    {
        _selectedSortMode = Enum.TryParse(typeof(SortMode), e.Value?.ToString() ?? "New", out object? mode) ? (SortMode)mode : SortMode.New;
        await ApplyFilterAsync();
    }

    private async Task ApplyFilterAsync()
    {
        if (_selectedGameId == Guid.Empty)
        {
            _images = _unfilteredImages;
            Logger.LogDebug(CustomLogEvents.PhotoBrowse,"No game selected, loading all images");
        }
        else
        {
            _images = _unfilteredImages.Where(x => x.GameId == _selectedGameId).ToArray();
            Logger.LogDebug(CustomLogEvents.PhotoBrowse, "Game filter set to {GameId}", _selectedGameId);
        }

        switch (_selectedSortMode)
        {
            case SortMode.New:
                _images = _images.OrderByDescending(x => x.UploadDateTime).ToArray();
                break;
            case SortMode.Hot:
                SortImagesByHotness();
                break;
            case SortMode.TopToday:
                SortImagesByLikes(1);
                break;
            case SortMode.TopWeek:
                SortImagesByLikes(7);
                break;
            case SortMode.TopMonth:
                SortImagesByLikes(30);
                break;
            case SortMode.TopYear:
                SortImagesByLikes(365);
                break;
            case SortMode.TopAllTime:
                _images = _images.OrderByDescending(x => x.LikesCount).ToArray();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        SetCurrentLoadEnd();
        await Task.Delay(10);
        await JsRuntime.InvokeVoidAsync("applyOnLoadToImages");
    }

    private void SortImagesByHotness()
    {
        var imagesAndLikes = _images.Select(x =>
        {
            return new
            {
                Image = x,
                Weighting = x.ImageLikes!.Where(like => like.Date > DateTime.UtcNow.AddHours(-5))
                    .Select(like => new LikeWeighting { Likes = x.ImageLikes!.Count(imageLike => imageLike.Date.Date >= DateTime.UtcNow.Date.AddHours(-1)), Recency = DateTime.Now - like.Date})
                    .Sum(lw => lw.HotScore)
            };
        });
        _images = imagesAndLikes.OrderByDescending(x => x.Weighting).Select(x => x.Image).ToArray();
    }

    private void SortImagesByLikes(int days)
    {
        var imagesAndLikes = _images.Select(x =>
        {
            return new
            {
                Image = x,
                Likes = x.ImageLikes!.Count(like => like.Date.Date >= DateTime.UtcNow.Date.AddDays(-days))
            };
        });
        _images = imagesAndLikes.OrderByDescending(x => x.Likes).Select(x => x.Image).ToArray();
    }

    private void SetCurrentLoadEnd()
    {
        if (_images.Length<InitialLoadSize-1)
        {
            _currentLoadEnd = (uint)_images.Length;
        }
        else
        {
            _currentLoadEnd = InitialLoadSize - 1;
        }
    }

    [JSInvokable]
    private  async Task LoadMoreImages()
    {
        _isLoading = true;
        if (_currentLoadEnd >= _images.Length)
        {
            return;
        }

        _currentLoadEnd += ImageLoadSize;
        if (_currentLoadEnd > _images.Length)
        {
            _currentLoadEnd = (uint)_images.Length;
        }

        await InvokeAsync(StateHasChanged);
        Logger.LogDebug(CustomLogEvents.PhotoBrowse,"Loaded more images");
        await Task.Delay(10);
        await JsRuntime.InvokeVoidAsync("applyOnLoadToImages");
        Logger.LogDebug(CustomLogEvents.PhotoBrowse,"Adding vertical class to images");
        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("OnLoad", DotNetObjectReference.Create(this), _loadMoreButton);
        }
    }

    private Task OpenImage(Guid id)
    {
        NavigationManager.NavigateTo($"/i/{id}");
        return Task.CompletedTask;
    }

    private static string GetEnumName(Enum value)
    {
        return value.GetType()
            .GetMember(value.ToString())
            .First()
            .GetCustomAttribute<DisplayAttribute>()
            ?.GetName() ?? "[No Name]";
    }

    private enum SortMode
    {
        [Display(Name = "New")]
        New,
        [Display(Name = "Hot")]
        Hot,
        [Display(Name = "New Today")]
        TopToday,
        [Display(Name = "Top this week")]
        TopWeek,
        [Display(Name = "Top this month")]
        TopMonth,
        [Display(Name = "Top this year")]
        TopYear,
        [Display(Name = "Top of all Time")]
        TopAllTime
    }
    private  class LikeWeighting
    {
        public int Likes { get; init; }
        public TimeSpan Recency { get; init; }
        public double HotScore => Likes / Recency.TotalHours;
    }
}