using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui.ViewModels;

public interface IBackgroundMusicService
{
    void ApplySettings(bool enabled, double volume);
}

public sealed class BackgroundMusicService : IBackgroundMusicService, IDisposable
{
    private const string TrackFileName = "War_Music_Background_25_Volume.mp3";

#if WINDOWS
    private readonly Windows.Media.Playback.MediaPlayer? _player;
    private bool _sourceLoaded;
#endif

    public BackgroundMusicService()
    {
#if WINDOWS
        try
        {
            _player = new Windows.Media.Playback.MediaPlayer
            {
                IsLoopingEnabled = true,
                AutoPlay = false,
                AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.GameMedia
            };
        }
        catch
        {
            _player = null;
        }
#endif
    }

    public void ApplySettings(bool enabled, double volume)
    {
#if WINDOWS
        if (_player is null)
            return;

        _player.Volume = Math.Clamp(volume, 0, 1);

        if (!enabled)
        {
            _player.Pause();
            return;
        }

        EnsureSourceLoaded();
        _player.Play();
#else
        _ = enabled;
        _ = volume;
#endif
    }

#if WINDOWS
    private void EnsureSourceLoaded()
    {
        if (_player is null || _sourceLoaded)
            return;

        string? path = ResolveTrackPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        _player.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(path));
        _sourceLoaded = true;
    }

    private static string? ResolveTrackPath()
    {
        string appBase = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(appBase, TrackFileName),
            Path.Combine(appBase, "Resources", "Audio", TrackFileName),
            Path.Combine(FileSystem.Current.AppDataDirectory, TrackFileName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
#endif

    public void Dispose()
    {
#if WINDOWS
        _player?.Dispose();
#endif
    }
}

public sealed class NoOpBackgroundMusicService : IBackgroundMusicService
{
    public void ApplySettings(bool enabled, double volume)
    {
        _ = enabled;
        _ = volume;
    }
}
