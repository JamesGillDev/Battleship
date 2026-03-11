using Microsoft.Maui.ApplicationModel;
#if WINDOWS
using Windows.Globalization;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
#endif

namespace BattleshipMaui.ViewModels;

public interface IGameFeedbackService
{
    void Play(
        GameFeedbackCue cue,
        bool soundEnabled,
        double soundFxVolume,
        bool hapticsEnabled,
        bool reduceMotion,
        bool commanderVoiceEnabled,
        string? shipName = null);
}

public sealed class DefaultGameFeedbackService : IGameFeedbackService
{
    private const string SurfaceExplosionTrack = "soundreality-explosion-fx-343683.mp3";
    private const string SubmarineExplosionTrack = "daviddumaisaudio-large-underwater-explosion-190270.mp3";
    private static readonly object MissTrackLock = new();
    private static readonly string[] MissExplosionTracks =
    {
        "Waterside_Explosion_Water_Sound_Effects1.mp3",
        "Waterside_Explosion_Water_Sound_Effects2.mp3",
        "Waterside_Explosion_Water_Sound_Effects3.mp3",
        "Waterside_Explosion_Water_Sound_Effects4.mp3"
    };
    private static readonly string[] LossOutcomeTracks =
    {
        AppAudio.LossSting,
        AppAudio.EnemyWonCall
    };
    private static readonly string[] EndgameOutcomeTracks =
    {
        AppAudio.WarOverCall,
        AppAudio.VictorySting,
        AppAudio.VictoryCall
    };
    private static int _lastMissTrackIndex = -1;
    private static int _lastLossOutcomeTrackIndex = -1;
    private static int _lastEndgameOutcomeTrackIndex = -1;

#if WINDOWS
    private static readonly object EffectsLock = new();
    private static readonly Lazy<Dictionary<string, MediaPlayer>?> EffectsPlayers =
        new(CreateEffectsPlayers, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly SemaphoreSlim CommanderVoiceLock = new(1, 1);
    private static readonly Lazy<Dictionary<string, MediaPlayer>?> CommanderVoicePlayers =
        new(CreateCommanderVoicePlayers, LazyThreadSafetyMode.ExecutionAndPublication);
    private static MediaPlayer? _commanderVoicePlayer;
    private static SpeechSynthesizer? _commanderVoiceSynthesizer;
    private static SpeechSynthesisStream? _activeCommanderVoiceStream;
#endif

    public void Play(
        GameFeedbackCue cue,
        bool soundEnabled,
        double soundFxVolume,
        bool hapticsEnabled,
        bool reduceMotion,
        bool commanderVoiceEnabled,
        string? shipName = null)
    {
        if (soundEnabled)
        {
            TryPlaySound(cue, shipName, soundFxVolume, commanderVoiceEnabled);
            if (commanderVoiceEnabled)
                _ = TrySpeakCommanderCueAsync(cue, soundFxVolume);
        }

        if (hapticsEnabled)
            TryPlayHaptics(cue, reduceMotion);
    }

    private static void TryPlaySound(GameFeedbackCue cue, string? shipName, double soundFxVolume, bool commanderVoiceEnabled)
    {
        double fxVolume = Math.Clamp(soundFxVolume, 0, 1);
        if (fxVolume <= 0)
            return;

        string? effectTrack = cue switch
        {
            GameFeedbackCue.Miss => SelectMissExplosionTrack(),
            GameFeedbackCue.Hit or GameFeedbackCue.Sunk or GameFeedbackCue.PlayerSunk => ResolveShipHitTrack(shipName),
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(effectTrack))
        {
            double trackVolume = ResolveTrackVolume(effectTrack, fxVolume);
            if (TryPlayAudioTrack(effectTrack, trackVolume))
                return;
        }

        if (commanderVoiceEnabled && HasDedicatedCommanderCallout(cue))
            return;

        TryPlayToneFallback(cue);
    }

    private static string SelectMissExplosionTrack()
    {
        lock (MissTrackLock)
        {
            if (MissExplosionTracks.Length == 1)
                return MissExplosionTracks[0];

            int selected;
            do
            {
                selected = Random.Shared.Next(MissExplosionTracks.Length);
            } while (selected == _lastMissTrackIndex);

            _lastMissTrackIndex = selected;
            return MissExplosionTracks[selected];
        }
    }

    private static string ResolveShipHitTrack(string? shipName)
    {
        string normalized = NormalizeShipName(shipName);
        if (normalized.Contains("submarine", StringComparison.Ordinal))
            return SubmarineExplosionTrack;

        return SurfaceExplosionTrack;
    }

    private static double ResolveTrackVolume(string effectTrack, double fallbackVolume)
    {
        if (string.Equals(effectTrack, SubmarineExplosionTrack, StringComparison.OrdinalIgnoreCase))
            return 0.20;

        return Math.Clamp(fallbackVolume, 0, 1);
    }

    private static string NormalizeShipName(string? shipName)
    {
        if (string.IsNullOrWhiteSpace(shipName))
            return string.Empty;

        return new string(shipName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static void TryPlayToneFallback(GameFeedbackCue cue)
    {
        try
        {
            var sequence = cue switch
            {
                GameFeedbackCue.TargetLocked => new[]
                {
                    new ToneStep(720, 54, 8),
                    new ToneStep(880, 68)
                },
                GameFeedbackCue.Miss => new[]
                {
                    new ToneStep(520, 24, 6),
                    new ToneStep(440, 30, 4),
                    new ToneStep(390, 36)
                },
                GameFeedbackCue.Hit => new[]
                {
                    new ToneStep(930, 32, 8),
                    new ToneStep(1180, 55)
                },
                GameFeedbackCue.Sunk => new[]
                {
                    new ToneStep(860, 46, 12),
                    new ToneStep(990, 56, 8),
                    new ToneStep(1160, 72)
                },
                GameFeedbackCue.PlayerSunk => new[]
                {
                    new ToneStep(610, 54, 10),
                    new ToneStep(520, 58, 8),
                    new ToneStep(430, 74)
                },
                GameFeedbackCue.Win => new[]
                {
                    new ToneStep(760, 52, 10),
                    new ToneStep(940, 52, 10),
                    new ToneStep(1160, 86, 18),
                    new ToneStep(1320, 94)
                },
                GameFeedbackCue.Loss => new[]
                {
                    new ToneStep(520, 76, 12),
                    new ToneStep(420, 90, 10),
                    new ToneStep(320, 122)
                },
                GameFeedbackCue.Draw => new[]
                {
                    new ToneStep(650, 62, 10),
                    new ToneStep(650, 62)
                },
                GameFeedbackCue.NewGame => new[]
                {
                    new ToneStep(680, 34, 6),
                    new ToneStep(840, 40, 8),
                    new ToneStep(1020, 52)
                },
                GameFeedbackCue.PlacementComplete => new[]
                {
                    new ToneStep(840, 46, 6),
                    new ToneStep(980, 52)
                },
                GameFeedbackCue.PlaceShip => new[]
                {
                    new ToneStep(520, 22, 4),
                    new ToneStep(610, 28, 5),
                    new ToneStep(560, 22)
                },
                _ => new[]
                {
                    new ToneStep(500, 34)
                }
            };

            _ = Task.Run(async () =>
            {
                foreach (var tone in sequence)
                {
                    try
                    {
                        Console.Beep(tone.Frequency, tone.DurationMs);
                    }
                    catch
                    {
                    }

                    if (tone.GapMs > 0)
                    {
                        try
                        {
                            await Task.Delay(tone.GapMs).ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                    }
                }
            });
        }
        catch
        {
            // Audio feedback is non-critical.
        }
    }

    private static bool TryPlayAudioTrack(string fileName, double volume)
    {
#if WINDOWS
        try
        {
            var players = EffectsPlayers.Value;
            if (players is null)
                return false;

            if (!players.TryGetValue(fileName, out var player))
                return false;

            lock (EffectsLock)
            {
                player.Volume = Math.Clamp(volume, 0, 1);
                player.Pause();

                TimeSpan clipStartOffset = ResolveClipStartOffset(fileName);
                if (player.PlaybackSession is not null)
                    player.PlaybackSession.Position = clipStartOffset;

                player.Play();

                if (clipStartOffset > TimeSpan.Zero && player.PlaybackSession is not null)
                    player.PlaybackSession.Position = clipStartOffset;
            }

            return true;
        }
        catch
        {
            return false;
        }
#else
        _ = fileName;
        _ = volume;
        return false;
#endif
    }

#if WINDOWS
    private static async Task TrySpeakCommanderCueAsync(GameFeedbackCue cue, double soundFxVolume)
    {
        if (TryPlayCommanderVoiceClip(cue, soundFxVolume))
            return;

        string? phrase = ResolveCommanderFallbackPhrase(cue);

        if (string.IsNullOrWhiteSpace(phrase))
            return;

        double volume = Math.Clamp(Math.Max(0.24, soundFxVolume), 0, 1);
        bool lockHeld = false;

        try
        {
            await CommanderVoiceLock.WaitAsync().ConfigureAwait(false);
            lockHeld = true;

            _activeCommanderVoiceStream?.Dispose();
            _activeCommanderVoiceStream = null;

            _commanderVoiceSynthesizer ??= CreateCommanderVoiceSynthesizer();
            _commanderVoicePlayer ??= new MediaPlayer
            {
                IsLoopingEnabled = false,
                AutoPlay = false,
                AudioCategory = MediaPlayerAudioCategory.GameEffects,
                Volume = volume
            };

            string ssml = BuildCommanderSsml(phrase);
            _activeCommanderVoiceStream = await _commanderVoiceSynthesizer.SynthesizeSsmlToStreamAsync(ssml);

            _commanderVoicePlayer.Pause();
            _commanderVoicePlayer.Source = MediaSource.CreateFromStream(_activeCommanderVoiceStream, _activeCommanderVoiceStream.ContentType);
            _commanderVoicePlayer.Volume = volume;
            _commanderVoicePlayer.Play();
        }
        catch
        {
        }
        finally
        {
            if (lockHeld)
                CommanderVoiceLock.Release();
        }
    }

    private static bool TryPlayCommanderVoiceClip(GameFeedbackCue cue, double soundFxVolume)
    {
        string[]? clips = SelectCommanderVoiceClips(cue);
        if (clips is null || clips.Length == 0)
            return false;

        try
        {
            double volume = Math.Clamp(Math.Max(0.24, soundFxVolume), 0, 1);
            _ = PlayCommanderVoiceSequenceAsync(clips, volume);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasDedicatedCommanderCallout(GameFeedbackCue cue)
    {
        return ResolveCommanderVoiceClipPool(cue)?.Count > 0
            || !string.IsNullOrWhiteSpace(ResolveCommanderFallbackPhrase(cue));
    }

    private static string? ResolveCommanderFallbackPhrase(GameFeedbackCue cue)
    {
        return cue switch
        {
            GameFeedbackCue.TargetLocked => "Target locked.",
            GameFeedbackCue.Hit => "Direct hit!",
            GameFeedbackCue.Miss => "Target missed.",
            GameFeedbackCue.Sunk => "Enemy vessel destroyed.",
            GameFeedbackCue.PlayerSunk => "Your vessel has been destroyed.",
            GameFeedbackCue.Win => "Victory secured.",
            GameFeedbackCue.Loss => "Enemy won the engagement.",
            GameFeedbackCue.Draw => "War over.",
            _ => null
        };
    }

    private static string[]? SelectCommanderVoiceClips(GameFeedbackCue cue)
    {
        IReadOnlyList<string>? pool = ResolveCommanderVoiceClipPool(cue);
        if (pool is null || pool.Count == 0)
            return null;

        if (pool.Count == 1)
            return new[] { pool[0] };

        return new[] { SelectRandomTrack(pool, cue) };
    }

    private static IReadOnlyList<string>? ResolveCommanderVoiceClipPool(GameFeedbackCue cue)
    {
        return cue switch
        {
            GameFeedbackCue.TargetLocked => new[] { AppAudio.TargetLocked },
            GameFeedbackCue.Hit => new[] { AppAudio.CommanderTargetHit },
            GameFeedbackCue.Miss => new[] { AppAudio.CommanderTargetMiss },
            GameFeedbackCue.Sunk => new[] { AppAudio.CommanderTargetSunk },
            GameFeedbackCue.PlayerSunk => new[] { AppAudio.CommanderPlayerSunk },
            GameFeedbackCue.Win or GameFeedbackCue.Draw => EndgameOutcomeTracks,
            GameFeedbackCue.Loss => LossOutcomeTracks,
            _ => null
        };
    }

    private static string SelectRandomTrack(IReadOnlyList<string> tracks, GameFeedbackCue cue)
    {
        lock (MissTrackLock)
        {
            if (tracks.Count == 1)
                return tracks[0];

            int lastTrackIndex = cue is GameFeedbackCue.Loss
                ? _lastLossOutcomeTrackIndex
                : _lastEndgameOutcomeTrackIndex;

            int selectedIndex;
            do
            {
                selectedIndex = Random.Shared.Next(tracks.Count);
            } while (selectedIndex == lastTrackIndex);

            if (cue is GameFeedbackCue.Loss)
                _lastLossOutcomeTrackIndex = selectedIndex;
            else
                _lastEndgameOutcomeTrackIndex = selectedIndex;

            return tracks[selectedIndex];
        }
    }

    private static async Task PlayCommanderVoiceSequenceAsync(IReadOnlyList<string> clips, double volume)
    {
        bool lockHeld = false;

        try
        {
            await CommanderVoiceLock.WaitAsync().ConfigureAwait(false);
            lockHeld = true;

            var players = CommanderVoicePlayers.Value;
            if (players is null)
                return;

            foreach (string clip in clips)
            {
                if (!players.TryGetValue(clip, out var player))
                    continue;

                lock (EffectsLock)
                {
                    player.Volume = volume;
                    player.Pause();
                    if (player.PlaybackSession is not null)
                        player.PlaybackSession.Position = TimeSpan.Zero;
                    player.Play();
                }

                await Task.Delay(ResolveCommanderClipDuration(clip)).ConfigureAwait(false);
            }
        }
        catch
        {
        }
        finally
        {
            if (lockHeld)
                CommanderVoiceLock.Release();
        }
    }

    private static TimeSpan ResolveCommanderClipDuration(string clip)
    {
        return clip switch
        {
            AppAudio.TargetLocked => TimeSpan.FromMilliseconds(1600),
            AppAudio.CommanderTargetHit => TimeSpan.FromMilliseconds(1200),
            AppAudio.CommanderTargetMiss => TimeSpan.FromMilliseconds(1700),
            AppAudio.CommanderTargetSunk => TimeSpan.FromMilliseconds(1800),
            AppAudio.CommanderPlayerSunk => TimeSpan.FromMilliseconds(1800),
            AppAudio.VictorySting => TimeSpan.FromMilliseconds(1100),
            AppAudio.VictoryCall => TimeSpan.FromMilliseconds(2100),
            AppAudio.LossSting => TimeSpan.FromMilliseconds(1300),
            AppAudio.EnemyWonCall => TimeSpan.FromMilliseconds(2500),
            AppAudio.WarOverCall => TimeSpan.FromMilliseconds(1600),
            _ => TimeSpan.FromMilliseconds(1500)
        };
    }

    private static SpeechSynthesizer CreateCommanderVoiceSynthesizer()
    {
        var synthesizer = new SpeechSynthesizer();

        try
        {
            VoiceInformation? preferredVoice = SpeechSynthesizer.AllVoices
                .FirstOrDefault(voice =>
                    voice.Gender == VoiceGender.Male &&
                    voice.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));

            if (preferredVoice is not null)
                synthesizer.Voice = preferredVoice;
        }
        catch
        {
        }

        return synthesizer;
    }

    private static string BuildCommanderSsml(string phrase)
    {
        return $$"""
        <speak version="1.0" xml:lang="en-US">
          <voice xml:lang="en-US">
            <prosody pitch="-24%" rate="-8%" volume="+0dB">
              {{phrase}}
            </prosody>
          </voice>
        </speak>
        """;
    }

    private static Dictionary<string, MediaPlayer>? CreateEffectsPlayers()
    {
        try
        {
            var tracks = MissExplosionTracks
                .Concat(new[] { SurfaceExplosionTrack, SubmarineExplosionTrack })
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var players = new Dictionary<string, MediaPlayer>(StringComparer.OrdinalIgnoreCase);
            foreach (var track in tracks)
            {
                string? path = AppAudio.ResolvePath(track);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var player = new MediaPlayer
                {
                    IsLoopingEnabled = false,
                    AutoPlay = false,
                    AudioCategory = MediaPlayerAudioCategory.GameEffects,
                    Volume = 1,
                    Source = MediaSource.CreateFromUri(new Uri(path))
                };

                players[track] = player;
            }

            return players.Count == 0 ? null : players;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, MediaPlayer>? CreateCommanderVoicePlayers()
    {
        try
        {
            string[] clips =
            {
                AppAudio.TargetLocked,
                AppAudio.CommanderTargetHit,
                AppAudio.CommanderTargetMiss,
                AppAudio.CommanderTargetSunk,
                AppAudio.CommanderPlayerSunk,
                AppAudio.VictorySting,
                AppAudio.VictoryCall,
                AppAudio.LossSting,
                AppAudio.EnemyWonCall,
                AppAudio.WarOverCall
            };

            var players = new Dictionary<string, MediaPlayer>(StringComparer.OrdinalIgnoreCase);
            foreach (string clip in clips)
            {
                string? path = AppAudio.ResolvePath(clip);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                players[clip] = new MediaPlayer
                {
                    IsLoopingEnabled = false,
                    AutoPlay = false,
                    AudioCategory = MediaPlayerAudioCategory.GameEffects,
                    Volume = 1,
                    Source = MediaSource.CreateFromUri(new Uri(path))
                };
            }

            return players.Count == 0 ? null : players;
        }
        catch
        {
            return null;
        }
    }

    private static TimeSpan ResolveClipStartOffset(string fileName)
    {
        if (fileName.StartsWith("Waterside_Explosion_Water_Sound_Effects", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromMilliseconds(110);

        return TimeSpan.Zero;
    }
#endif

    private static void TryPlayHaptics(GameFeedbackCue cue, bool reduceMotion)
    {
        try
        {
            if (reduceMotion)
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                return;
            }

            if (cue is GameFeedbackCue.Win)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(95));
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                return;
            }

            if (cue is GameFeedbackCue.Sunk or GameFeedbackCue.PlayerSunk)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(72));
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                return;
            }

            if (cue is GameFeedbackCue.Loss or GameFeedbackCue.Draw)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(70));
                return;
            }

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Haptics may not be available on this device.
        }
    }
}

file readonly record struct ToneStep(int Frequency, int DurationMs, int GapMs = 0);
