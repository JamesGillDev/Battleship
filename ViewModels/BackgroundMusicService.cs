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
    private CancellationTokenSource? _fadeCts;
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

        double targetVolume = Math.Clamp(volume, 0, 1);

        if (!enabled)
        {
            _fadeCts?.Cancel();
            _player.Pause();
            return;
        }

        EnsureSourceLoaded();

        bool isStartingPlayback = _player.PlaybackSession.PlaybackState != Windows.Media.Playback.MediaPlaybackState.Playing;
        if (isStartingPlayback)
        {
            _player.Volume = 0;
            _player.Play();
            _ = FadeToVolumeAsync(targetVolume, TimeSpan.FromSeconds(2));
            return;
        }

        _player.Volume = targetVolume;
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

        foreach (var uri in ResolveTrackUris())
        {
            try
            {
                _player.Source = Windows.Media.Core.MediaSource.CreateFromUri(uri);
                _sourceLoaded = true;
                return;
            }
            catch
            {
                // Keep probing fallback URIs until one resolves.
            }
        }
    }

    private static IEnumerable<Uri> ResolveTrackUris()
    {
        string appBase = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(appBase, TrackFileName),
            Path.Combine(appBase, "Resources", "Audio", TrackFileName),
            Path.Combine(appBase, "Assets", TrackFileName),
            Path.Combine(appBase, "AppX", TrackFileName),
            Path.Combine(FileSystem.Current.AppDataDirectory, TrackFileName)
        };

        yield return new Uri($"ms-appx:///{TrackFileName}");
        yield return new Uri($"ms-appx:///Resources/Audio/{TrackFileName}");
        yield return new Uri($"ms-appx:///Assets/{TrackFileName}");

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                yield return new Uri(candidate);
        }
    }

    private async Task FadeToVolumeAsync(double targetVolume, TimeSpan duration)
    {
        if (_player is null)
            return;

        _fadeCts?.Cancel();
        _fadeCts = new CancellationTokenSource();
        CancellationToken ct = _fadeCts.Token;

        double start = _player.Volume;
        const int steps = 24;
        int delayMs = Math.Max(20, (int)(duration.TotalMilliseconds / steps));

        for (int step = 1; step <= steps; step++)
        {
            if (ct.IsCancellationRequested)
                return;

            double t = (double)step / steps;
            _player.Volume = start + ((targetVolume - start) * t);
            await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        _player.Volume = targetVolume;
    }
#endif

    public void Dispose()
    {
#if WINDOWS
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
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
