using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.JSInterop;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.Components.Pages.PhotoMode;

public partial class Browse : ComponentBase
{
    [Inject] private IDatabaseMethodService DbMethodService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private ICloudFlareImageService CloudFlareImageService { get; set; } = null!;
    [Inject] private Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> UserManager { get; set; } = null!;
    private ElementReference _loadMoreButton;
    private const int InitialLoadSize = 50;
    private bool _isLoading;
    private const uint ImageLoadSize = 30;
    private uint _currentLoadEnd;

    private UserImage[] _images = [];
    private UserImage[] _unfilteredImages = [];
    private UserImage[] _topHotImages = [];
    private Game[]? _games = [];
    private Guid _selectedGameId;
    private SortMode _selectedSortMode = SortMode.New;

    private UserImage? _selectedImage;
    private int _selectedImageIndex = -1;
    private bool _isNavigating;
    private bool _navigatedOnce;
    private string _navigationDirection = ""; // "next" or "prev"
    private bool _isLiked;
    private int _likesCount;
    private ApplicationUser? _currentUser;
    private bool _canDelete;
    private bool _showDeleteConfirm;

    protected override async Task OnInitializedAsync()
    {
        var uri = new Uri(NavigationManager.Uri);
        NameValueCollection queryParameters = HttpUtility.ParseQueryString(uri.Query);
        string? selectedGameId = queryParameters.Get("gameId");
        string? selectedSortMode = queryParameters.Get("sortMode");

        if (selectedGameId != null)
        {
            _selectedGameId = Guid.Parse(selectedGameId);
        }

        if (selectedSortMode != null)
        {
            _selectedSortMode = Enum.Parse<SortMode>(selectedSortMode);
        }
        LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            _currentUser = await dbContext.ApplicationUsers.FirstOrDefaultAsync(x => x.DiscordId == ulong.Parse(userId!));
        }

        _unfilteredImages = dbContext.UserImages
            .Include(x=>x.Game)
            .Include(x=>x.ImageLikes)
            .Include(x=>x.ApplicationUser)
            .OrderByDescending(x => x.UploadDateTime).ToArray();

        _games = await Cache.GetOrCreateAsync("GameOptionsKey", async _ => await dbContext.Games.ToArrayAsync());
        _images = _unfilteredImages;

        await ApplyFilterAsync(true);
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
        NavigationManager.NavigateTo($"/PhotoMode/Browse?gameId={_selectedGameId}&sortMode={_selectedSortMode}", forceLoad: false);
    }

    private async Task OnSortModeSelectedAsync(ChangeEventArgs e)
    {
        _selectedSortMode = Enum.TryParse(typeof(SortMode), e.Value?.ToString() ?? "New", out object? mode) ? (SortMode)mode : SortMode.New;
        await ApplyFilterAsync();
        NavigationManager.NavigateTo($"/PhotoMode/Browse?gameId={_selectedGameId}&sortMode={_selectedSortMode}", forceLoad: false);
    }

    private async Task ApplyFilterAsync(bool skipJsInterop = false)
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

        _topHotImages = GetTopHotImages(_images);

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
        if (skipJsInterop) return;
        await Task.Delay(10);
        await JsRuntime.InvokeVoidAsync("applyOnLoadToImages");
    }

    private UserImage[] GetTopHotImages(UserImage[] source)
    {
        return source.Select(x => new
            {
                Image = x,
                Weighting = x.ImageLikes!.Where(like => like.Date > DateTime.UtcNow.AddHours(-5))
                    .Select(like => new LikeWeighting { Likes = x.ImageLikes!.Count(imageLike => imageLike.Date.Date >= DateTime.UtcNow.Date.AddHours(-1)), Recency = DateTime.Now - like.Date })
                    .Sum(lw => lw.HotScore)
            })
            .OrderByDescending(x => x.Weighting)
            .Take(5)
            .Select(x => x.Image)
            .ToArray();
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
            await ApplyFilterAsync();
        }
        StateHasChanged();
    }

    private async Task OpenImage(Guid id)
    {
        _selectedImage = _images.FirstOrDefault(x => x.Id == id);
        if (_selectedImage == null) return;
        _selectedImageIndex = Array.IndexOf(_images, _selectedImage);
        _likesCount = _selectedImage.LikesCount;
        _isLiked = _currentUser != null && _selectedImage.ImageLikes != null && _selectedImage.ImageLikes.Any(x => x.DiscordId == _currentUser.DiscordId);
        _canDelete = await CheckDeletePermission(_selectedImage);
        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> CheckDeletePermission(UserImage image)
    {
        if (_currentUser == null) return false;
        if (image.DiscordId == _currentUser.DiscordId) return true;
        var userRoles = await UserManager.GetRolesAsync(_currentUser);
        return userRoles.Any(r => r is "Administrator" or "Moderator");
    }

    private void CloseImage()
    {
        _selectedImage = null;
        _selectedImageIndex = -1;
        _navigatedOnce = false;
    }

    private async Task NextImage()
    {
        if (_selectedImageIndex < _images.Length - 1 && !_isNavigating)
        {
            _navigationDirection = "next";
            _selectedImageIndex++;
            await SelectImageByIndex(_selectedImageIndex);
        }
    }

    private async Task PreviousImage()
    {
        if (_selectedImageIndex > 0 && !_isNavigating)
        {
            _navigationDirection = "prev";
            _selectedImageIndex--;
            await SelectImageByIndex(_selectedImageIndex);
        }
    }

    private async Task SelectImageByIndex(int index)
    {
        _isNavigating = true;
        _navigatedOnce = true;
        _selectedImage = _images[index];
        _likesCount = _selectedImage.LikesCount;
        _isLiked = _currentUser != null && _selectedImage.ImageLikes != null && _selectedImage.ImageLikes.Any(x => x.DiscordId == _currentUser.DiscordId);
        _canDelete = await CheckDeletePermission(_selectedImage);
        await InvokeAsync(StateHasChanged);
        
        // Wait for animation to finish (matching CSS duration, e.g., 500ms)
        await Task.Delay(500);
        _isNavigating = false;
        _navigationDirection = "";
        await InvokeAsync(StateHasChanged);

        if (index >= _currentLoadEnd - 5)
        {
            await LoadMoreImages();
        }
    }

    private async Task CopyImageUrl()
    {
        if (_selectedImage == null) return;
        string baseUrl = NavigationManager.BaseUri.TrimEnd('/');
        string url = $"{baseUrl}/i/{_selectedImage.Id}";
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", url);
    }

    private async Task DeleteImageAsync()
    {
        if (_selectedImage == null || !_canDelete) return;
        
        DeleteImageResponse response = await CloudFlareImageService.DeleteImageAsync(_selectedImage.Id);
        if (response.Success)
        {
            await DbMethodService.DeleteImageAsync(_selectedImage.Id);
            
            // Remove from local lists
            _images = _images.Where(x => x.Id != _selectedImage.Id).ToArray();
            _unfilteredImages = _unfilteredImages.Where(x => x.Id != _selectedImage.Id).ToArray();
            _topHotImages = _topHotImages.Where(x => x.Id != _selectedImage.Id).ToArray();
            
            CloseImage();
            _showDeleteConfirm = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ToggleLikeAsync()
    {
        if (_currentUser == null || _selectedImage == null) return;
        await DbMethodService.ToggleImageLikeAsync(_currentUser, _selectedImage);
        _isLiked = !_isLiked;
        _likesCount = await DbMethodService.GetImageLikesCountAsync(_selectedImage.Id);
        
        // Update the image in the list to reflect the new like count and likes list
        _selectedImage.LikesCount = _likesCount;
        // Note: In a real app we might want to refresh the ImageLikes collection too, 
        // but for UI toggle this is usually enough if we just want to show the count.
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
        [Display(Name = "Top today")]
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