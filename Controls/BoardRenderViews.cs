using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.Graphics;
#if WINDOWS
using Microsoft.Maui.Graphics.Platform;
#endif

namespace BattleshipMaui.Controls;

public abstract class BoardRenderViewBase : GraphicsView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(BoardRenderViewBase),
        default(IEnumerable),
        propertyChanged: OnItemsSourceChanged);

    private readonly List<BoardCellVm> _cells = new();
    private INotifyCollectionChanged? _observableItemsSource;
    private IDispatcherTimer? _timer;
    private double _animationPhase;
    private bool _lastReduceMotion;

    protected BoardRenderViewBase()
    {
        InputTransparent = true;
        BackgroundColor = Colors.Transparent;
        HandlerChanged += OnHandlerChanged;
        PropertyChanged += OnSelfPropertyChanged;
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    protected IReadOnlyList<BoardCellVm> Cells => _cells;

    protected double AnimationPhase => _animationPhase;

    private static void OnItemsSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        ((BoardRenderViewBase)bindable).ResetItemsSource(newValue as IEnumerable);
    }

    private void ResetItemsSource(IEnumerable? itemsSource)
    {
        DetachItemsSource();
        _cells.Clear();

        if (itemsSource is not null)
        {
            foreach (var item in itemsSource)
            {
                if (item is not BoardCellVm cell)
                    continue;

                _cells.Add(cell);
                cell.PropertyChanged += OnCellPropertyChanged;
            }

            if (itemsSource is INotifyCollectionChanged observable)
            {
                _observableItemsSource = observable;
                _observableItemsSource.CollectionChanged += OnItemsCollectionChanged;
            }
        }

        Invalidate();
    }

    private void DetachItemsSource()
    {
        if (_observableItemsSource is not null)
        {
            _observableItemsSource.CollectionChanged -= OnItemsCollectionChanged;
            _observableItemsSource = null;
        }

        foreach (var cell in _cells)
            cell.PropertyChanged -= OnCellPropertyChanged;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ResetItemsSource(ItemsSource);
    }

    private void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Invalidate();
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        UpdateTimerState();
    }

    private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsVisible))
            UpdateTimerState();
    }

    private void UpdateTimerState()
    {
        if (Handler is null || !IsVisible || Dispatcher is null)
        {
            StopTimer();
            return;
        }

        _timer ??= CreateAnimationTimer();

        if (!_timer.IsRunning)
            _timer.Start();
    }

    private IDispatcherTimer CreateAnimationTimer()
    {
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(33);
        timer.Tick += OnAnimationTick;
        return timer;
    }

    private void StopTimer()
    {
        if (_timer?.IsRunning == true)
            _timer.Stop();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (AnimationRuntimeSettings.ReduceMotion)
        {
            if (!_lastReduceMotion)
                Invalidate();

            _lastReduceMotion = true;
            return;
        }

        _lastReduceMotion = false;
        _animationPhase += 0.04 * Math.Max(0.45, AnimationRuntimeSettings.SpeedMultiplier);
        if (_animationPhase > Math.PI * 32)
            _animationPhase = 0;

        Invalidate();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Invalidate();
    }
}

public sealed class OceanBoardSurfaceView : BoardRenderViewBase
{
    private const float BoardPerspectiveTilt = 0.09f;

    public static readonly BindableProperty IsPlayerBoardProperty = BindableProperty.Create(
        nameof(IsPlayerBoard),
        typeof(bool),
        typeof(OceanBoardSurfaceView),
        false,
        propertyChanged: (bindable, _, _) => ((OceanBoardSurfaceView)bindable).Invalidate());

    private readonly OceanBoardSurfaceDrawable _drawable;

    public OceanBoardSurfaceView()
    {
        _drawable = new OceanBoardSurfaceDrawable(this);
        Drawable = _drawable;
    }

    public bool IsPlayerBoard
    {
        get => (bool)GetValue(IsPlayerBoardProperty);
        set => SetValue(IsPlayerBoardProperty, value);
    }

    private void DrawSurface(ICanvas canvas, RectF dirtyRect)
    {
        if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
            return;

        float boardWidth = dirtyRect.Width;
        float boardHeight = dirtyRect.Height;
        float cellSize = Math.Min(boardWidth, boardHeight) / BoardViewModel.Size;
        float phase = (float)AnimationPhase;

        DrawOceanBackdrop(canvas, boardWidth, boardHeight, phase);
        DrawSubsurfaceContours(canvas, boardWidth, boardHeight, phase);
        DrawWaveBands(canvas, boardWidth, boardHeight, phase);
        DrawCells(canvas, cellSize, phase);
        DrawSpecularSweep(canvas, boardWidth, boardHeight, phase);
        DrawGridBloom(canvas, boardWidth, boardHeight);
    }

    private void DrawOceanBackdrop(ICanvas canvas, float boardWidth, float boardHeight, float phase)
    {
        var deepWater = ResolveColor("GameColorSurface", IsPlayerBoard ? "#0b3352" : "#082d49");
        var midWater = ResolveColor("GameColorPanel", IsPlayerBoard ? "#12496f" : "#0f4165");
        var crest = ResolveColor("GameColorAccentSoft", "#3d8fc2");
        var abyss = Darken(deepWater, 0.26f);

        canvas.FillColor = deepWater;
        canvas.FillRectangle(0, 0, boardWidth, boardHeight);

        canvas.FillColor = WithAlpha(abyss, 0.32f);
        canvas.FillEllipse(-boardWidth * 0.12f, boardHeight * 0.54f, boardWidth * 1.24f, boardHeight * 0.66f);

        for (int layer = 0; layer < 6; layer++)
        {
            float width = boardWidth * (0.72f + (layer * 0.08f));
            float height = boardHeight * (0.2f + (layer * 0.04f));
            float x = ((boardWidth - width) * 0.5f) + (MathF.Sin((phase * 0.45f) + layer) * 10f);
            float y = (boardHeight * (0.1f + (layer * 0.1f))) + (MathF.Cos((phase * 0.34f) + (layer * 0.7f)) * 8f);

            canvas.FillColor = WithAlpha(Lighten(midWater, 0.22f + (layer * 0.03f)), 0.055f + (layer * 0.018f));
            canvas.FillEllipse(x, y, width, height);
        }

        canvas.FillColor = WithAlpha(crest, 0.08f);
        canvas.FillEllipse(boardWidth * 0.18f, boardHeight * 0.08f, boardWidth * 0.64f, boardHeight * 0.32f);

        canvas.FillColor = WithAlpha(Lighten(crest, 0.22f), 0.06f);
        canvas.FillEllipse(boardWidth * 0.08f, -boardHeight * 0.04f, boardWidth * 0.9f, boardHeight * 0.28f);
    }

    private void DrawSubsurfaceContours(ICanvas canvas, float boardWidth, float boardHeight, float phase)
    {
        var trough = ResolveColor("GameColorPanel", IsPlayerBoard ? "#153b62" : "#12324f");
        var trench = WithAlpha(Darken(trough, 0.4f), 0.12f);
        var shelf = WithAlpha(Lighten(trough, 0.18f), 0.08f);

        for (int ridge = 0; ridge < 5; ridge++)
        {
            float y = (boardHeight * (0.18f + (ridge * 0.17f))) + (MathF.Sin((phase * 0.42f) + ridge) * 9f);
            float width = boardWidth * (0.74f + (ridge * 0.08f));
            float height = boardHeight * (0.1f + (ridge * 0.03f));
            float x = ((boardWidth - width) * 0.5f) + (MathF.Cos((phase * 0.31f) + ridge) * 12f);

            canvas.FillColor = ridge % 2 == 0 ? trench : shelf;
            canvas.FillEllipse(x, y, width, height);
        }
    }

    private void DrawWaveBands(ICanvas canvas, float boardWidth, float boardHeight, float phase)
    {
        var crest = ResolveColor("GameColorAccent", "#7dd7ff");
        var foam = ResolveColor("GameColorTextPrimary", "#dff6ff");
        var trough = Darken(ResolveColor("GameColorSurfaceAlt", "#164468"), 0.14f);

        for (int band = 0; band < 11; band++)
        {
            float baseY = 18f + (band * 40f);
            float amplitude = 4f + ((band % 3) * 2.1f);
            float stroke = 2.2f + ((band + 1) % 2);
            var path = new PathF();
            var shadowPath = new PathF();

            for (float x = -28; x <= boardWidth + 28; x += 18)
            {
                float y = baseY
                    + (MathF.Sin((x * 0.028f) + (phase * 1.4f) + (band * 0.78f)) * amplitude)
                    + (MathF.Cos((x * 0.014f) - (phase * 0.62f) + band) * (amplitude * 0.45f));
                float shadowY = y + 5.5f + ((band % 2) * 0.8f);

                if (x <= -28)
                {
                    path.MoveTo(x, y);
                    shadowPath.MoveTo(x, shadowY);
                }
                else
                {
                    path.LineTo(x, y);
                    shadowPath.LineTo(x, shadowY);
                }
            }

            canvas.StrokeColor = WithAlpha(trough, 0.16f + ((band % 2) * 0.02f));
            canvas.StrokeSize = stroke + 5.2f;
            canvas.DrawPath(shadowPath);

            canvas.StrokeColor = WithAlpha(crest, 0.06f + ((band % 3) * 0.015f));
            canvas.StrokeSize = stroke + 3.2f;
            canvas.DrawPath(path);

            canvas.StrokeColor = WithAlpha(foam, 0.09f + ((band % 2) * 0.02f));
            canvas.StrokeSize = stroke;
            canvas.DrawPath(path);
        }

        for (int caustic = 0; caustic < 7; caustic++)
        {
            float x = (caustic * 74f) - 42f + (MathF.Sin((phase * 0.55f) + caustic) * 14f);
            float width = 34f + ((caustic % 3) * 6f);
            canvas.FillColor = WithAlpha(foam, 0.03f + ((caustic % 2) * 0.01f));
            canvas.FillRoundedRectangle(x, -18, width, boardHeight + 36, 14);
        }
    }

    private void DrawCells(ICanvas canvas, float cellSize, float phase)
    {
        foreach (var cell in Cells)
        {
            float x = cell.Col * cellSize;
            float y = cell.Row * cellSize;
            float inset = 2.2f;
            float width = cellSize - (inset * 2);
            float height = cellSize - (inset * 2);
            float corner = 6.5f;
            var plate = new RectF(x + inset, y + inset, width, height);

            Color baseColor = WithAlpha(cell.CellFillColor, ResolveCellFillOpacity(cell));
            Color glossColor = WithAlpha(Lighten(cell.CellFillColor, 0.38f), 0.22f);
            Color shadowColor = WithAlpha(Darken(cell.CellFillColor, 0.5f), 0.32f);
            Color deepShadow = WithAlpha(Darken(cell.CellFillColor, 0.68f), 0.22f);
            Color ridgeHighlight = WithAlpha(Lighten(cell.CellFillColor, 0.62f), 0.2f);
            float shimmer = (MathF.Sin((phase * 1.25f) + (cell.Row * 0.7f) + (cell.Col * 0.58f)) + 1f) * 0.5f;

            canvas.FillColor = deepShadow;
            canvas.FillRoundedRectangle(plate.X + 2.2f, plate.Y + 2.6f, plate.Width, plate.Height, corner + 1.2f);

            canvas.FillColor = baseColor;
            canvas.FillRoundedRectangle(plate.X, plate.Y, plate.Width, plate.Height, corner);

            canvas.FillColor = WithAlpha(ridgeHighlight, 0.08f + (shimmer * 0.05f));
            canvas.FillRoundedRectangle(plate.X + 1f, plate.Y + 1f, plate.Width - 2f, plate.Height - 2f, corner - 0.8f);

            canvas.FillColor = WithAlpha(glossColor, 0.08f + (shimmer * 0.08f));
            canvas.FillRoundedRectangle(plate.X + 1.2f, plate.Y + 1.2f, plate.Width - 2.4f, plate.Height * 0.38f, 5.4f);

            canvas.FillColor = shadowColor;
            canvas.FillRoundedRectangle(plate.X + 2f, plate.Bottom - (plate.Height * 0.22f), plate.Width - 4f, plate.Height * 0.16f, 4.4f);

            DrawCellBevel(canvas, plate, phase, cell);
            DrawMicroRipples(canvas, plate, phase, cell);
        }
    }

    private static float ResolveCellFillOpacity(BoardCellVm cell)
    {
        if (cell.IsHitMarkerVisible)
            return 0.36f;

        if (cell.IsMissMarkerVisible)
            return 0.26f;

        if (cell.IsTargetLockVisible)
            return 0.38f;

        if (cell.IsPlayerBoard && cell.HasShip)
            return 0.42f;

        return 0.2f;
    }

    private static void DrawMicroRipples(ICanvas canvas, RectF plate, float phase, BoardCellVm cell)
    {
        Color rippleColor = WithAlpha(Lighten(cell.CellFillColor, 0.5f), 0.08f);
        float centerY = plate.Center.Y;
        float bandHeight = plate.Height * 0.12f;

        for (int ripple = 0; ripple < 2; ripple++)
        {
            float y = centerY - bandHeight + (ripple * (bandHeight * 1.35f));
            float offset = MathF.Sin((phase * 1.9f) + (cell.Row * 0.9f) + (cell.Col * 0.5f) + ripple) * 2.2f;
            var path = new PathF();
            path.MoveTo(plate.X + 4, y + offset);
            path.LineTo(plate.X + (plate.Width * 0.34f), y - offset);
            path.LineTo(plate.X + (plate.Width * 0.68f), y + (offset * 0.6f));
            path.LineTo(plate.Right - 4, y - (offset * 0.4f));

            canvas.StrokeColor = rippleColor;
            canvas.StrokeSize = 1.2f;
            canvas.DrawPath(path);
        }
    }

    private static void DrawCellBevel(ICanvas canvas, RectF plate, float phase, BoardCellVm cell)
    {
        float shimmer = (MathF.Sin((phase * 1.05f) + (cell.Row * 0.33f) + (cell.Col * 0.81f)) + 1f) * 0.5f;
        Color topEdge = WithAlpha(Lighten(cell.CellStrokeColor, 0.42f), 0.18f + (shimmer * 0.05f));
        Color bottomEdge = WithAlpha(Darken(cell.CellStrokeColor, 0.38f), 0.24f);
        float lip = MathF.Max(2.2f, plate.Width * 0.08f);

        canvas.FillColor = topEdge;
        canvas.FillRoundedRectangle(plate.X + 1.4f, plate.Y + 1.4f, plate.Width - 2.8f, lip, 3.2f);

        canvas.FillColor = topEdge;
        canvas.FillRoundedRectangle(plate.X + 1.2f, plate.Y + 1.2f, lip, plate.Height - 2.4f, 3.2f);

        canvas.FillColor = bottomEdge;
        canvas.FillRoundedRectangle(plate.X + 1.6f, plate.Bottom - lip - 1.4f, plate.Width - 3.2f, lip, 3.2f);

        canvas.FillColor = bottomEdge;
        canvas.FillRoundedRectangle(plate.Right - lip - 1.4f, plate.Y + 1.6f, lip, plate.Height - 3.2f, 3.2f);
    }

    private void DrawSpecularSweep(ICanvas canvas, float boardWidth, float boardHeight, float phase)
    {
        var foam = ResolveColor("GameColorTextPrimary", "#e5fbff");
        float sweepWidth = boardWidth * 0.28f;
        float sweepX = ((phase * 44f) % (boardWidth + (sweepWidth * 2))) - sweepWidth;
        var path = new PathF();
        path.MoveTo(sweepX, 0);
        path.LineTo(sweepX + sweepWidth, 0);
        path.LineTo(sweepX + (sweepWidth * (1 + BoardPerspectiveTilt)), boardHeight);
        path.LineTo(sweepX - (sweepWidth * BoardPerspectiveTilt), boardHeight);
        path.Close();

        canvas.FillColor = WithAlpha(foam, AnimationRuntimeSettings.ReduceMotion ? 0.03f : 0.05f);
        canvas.FillPath(path);
    }

    private void DrawGridBloom(ICanvas canvas, float boardWidth, float boardHeight)
    {
        var edgeGlow = ResolveColor("GameColorAccentSoft", "#4faee0");
        canvas.StrokeColor = WithAlpha(edgeGlow, 0.12f);
        canvas.StrokeSize = 2f;
        canvas.DrawRoundedRectangle(2, 2, boardWidth - 4, boardHeight - 4, 8);

        canvas.StrokeColor = WithAlpha(Lighten(edgeGlow, 0.35f), 0.08f);
        canvas.StrokeSize = 1.1f;
        canvas.DrawRoundedRectangle(7, 7, boardWidth - 14, boardHeight - 14, 6);
    }

    private sealed class OceanBoardSurfaceDrawable(OceanBoardSurfaceView owner) : IDrawable
    {
        private readonly OceanBoardSurfaceView _owner = owner;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            _owner.DrawSurface(canvas, dirtyRect);
        }
    }

    private static Color ResolveColor(string resourceKey, string fallbackHex)
    {
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true && resource is Color color)
            return color;

        return Color.FromArgb(fallbackHex);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.Red, color.Green, color.Blue, Clamp01(alpha));
    }

    private static Color Lighten(Color color, float amount)
    {
        amount = Clamp01(amount);
        return new Color(
            color.Red + ((1 - color.Red) * amount),
            color.Green + ((1 - color.Green) * amount),
            color.Blue + ((1 - color.Blue) * amount),
            color.Alpha);
    }

    private static Color Darken(Color color, float amount)
    {
        amount = Clamp01(amount);
        float factor = 1 - amount;
        return new Color(color.Red * factor, color.Green * factor, color.Blue * factor, color.Alpha);
    }

    private static float Clamp01(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}

public sealed class BoardEffectsView : BoardRenderViewBase
{
    private const string SourceExplosionPath = @"C:\MSSA Code-github\BattleshipMaui\Resources\Images\explosion.png";

    private readonly BoardEffectsDrawable _drawable;
    private Microsoft.Maui.Graphics.IImage? _explosionImage;
    private bool _isExplosionImageLoading;

    public BoardEffectsView()
    {
        _drawable = new BoardEffectsDrawable(this);
        Drawable = _drawable;
        _ = EnsureExplosionImageAsync();
    }

    private void DrawEffects(ICanvas canvas, RectF dirtyRect)
    {
        if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
            return;

        float cellSize = Math.Min(dirtyRect.Width, dirtyRect.Height) / BoardViewModel.Size;
        float phase = (float)AnimationPhase;

        foreach (var cell in Cells)
        {
            float x = cell.Col * cellSize;
            float y = cell.Row * cellSize;
            var rect = new RectF(x, y, cellSize, cellSize);

            if (cell.IsSunkSmokeVisible)
                DrawSunkSmoke(canvas, rect, phase, cell);

            if (cell.IsTargetLockVisible)
                DrawTargetLock(canvas, rect, phase, cell);

            if (cell.IsMissMarkerVisible)
                DrawMissSplash(canvas, rect, phase, cell);

            if (cell.IsHitMarkerVisible)
                DrawHitBlast(canvas, rect, phase, cell);
        }
    }

    private static void DrawSunkSmoke(ICanvas canvas, RectF rect, float phase, BoardCellVm cell)
    {
        float cellSeed = (cell.Row * 0.73f) + (cell.Col * 0.41f);
        Color smokeShadow = WithAlpha(Color.FromArgb("#313741"), 0.16f);
        Color smokeDense = WithAlpha(Color.FromArgb("#69717d"), 0.18f);
        Color smokeSoft = WithAlpha(Color.FromArgb("#d9e0e8"), 0.14f);
        Color ember = WithAlpha(Color.FromArgb("#ff9f4a"), 0.11f);
        Color heatGlow = WithAlpha(Color.FromArgb("#ffcf7b"), 0.06f);

        canvas.FillColor = heatGlow;
        canvas.FillEllipse(
            rect.Center.X - (rect.Width * 0.23f),
            rect.Center.Y + (rect.Height * 0.02f),
            rect.Width * 0.46f,
            rect.Height * 0.2f);

        for (int plume = 0; plume < 5; plume++)
        {
            float time = phase + cellSeed + (plume * 0.82f);
            float width = rect.Width * (0.28f + (plume * 0.075f));
            float height = rect.Height * (0.2f + (plume * 0.06f));
            float driftX = MathF.Sin((time * 0.92f) + plume) * (2.8f + (plume * 0.9f));
            float rise = (MathF.Cos((time * 0.67f) + plume) * 2.2f) - (plume * 4.3f);
            float x = rect.Center.X - (width * 0.5f) + driftX;
            float y = rect.Center.Y - (height * 0.32f) + rise;

            canvas.FillColor = plume switch
            {
                0 => smokeShadow,
                1 or 2 => smokeDense,
                _ => smokeSoft
            };
            canvas.FillEllipse(x, y, width, height);

            float wispWidth = width * 0.54f;
            float wispHeight = height * 0.58f;
            canvas.FillColor = WithAlpha(smokeSoft, 0.08f + (plume * 0.012f));
            canvas.FillEllipse(
                x + (width * 0.18f),
                y - (height * 0.2f),
                wispWidth,
                wispHeight);
        }

        for (int wisp = 0; wisp < 2; wisp++)
        {
            float time = phase + cellSeed + (wisp * 1.35f);
            float width = rect.Width * (0.22f + (wisp * 0.08f));
            float height = rect.Height * (0.14f + (wisp * 0.05f));
            float x = rect.Center.X - (width * 0.5f) + (MathF.Sin((time * 1.3f) + wisp) * 4.4f);
            float y = rect.Y - (rect.Height * 0.04f) - (wisp * 5.8f) + (MathF.Cos(time * 0.8f) * 1.4f);

            canvas.FillColor = WithAlpha(smokeSoft, 0.09f - (wisp * 0.015f));
            canvas.FillEllipse(x, y, width, height);
        }

        canvas.FillColor = ember;
        canvas.FillEllipse(
            rect.Center.X - (rect.Width * 0.12f),
            rect.Center.Y + (rect.Height * 0.04f),
            rect.Width * 0.24f,
            rect.Height * 0.14f);
    }

    private static void DrawTargetLock(ICanvas canvas, RectF rect, float phase, BoardCellVm cell)
    {
        Color accent = WithAlpha(Lighten(cell.CellStrokeColor, 0.16f), 0.88f);
        Color halo = WithAlpha(Lighten(cell.CellStrokeColor, 0.35f), 0.22f);
        float pulse = (MathF.Sin((phase * 2.4f) + (cell.Row * 0.46f) + (cell.Col * 0.39f)) + 1f) * 0.5f;
        float inset = rect.Width * 0.16f;
        float arm = rect.Width * 0.2f;
        float centerSize = rect.Width * 0.14f;
        float pulseSize = rect.Width * (0.48f + (pulse * 0.18f));
        float outerPulseSize = rect.Width * (0.72f + (pulse * 0.22f));

        canvas.FillColor = WithAlpha(halo, 0.1f + (pulse * 0.08f));
        canvas.FillEllipse(
            rect.Center.X - (pulseSize * 0.5f),
            rect.Center.Y - (pulseSize * 0.5f),
            pulseSize,
            pulseSize);

        canvas.StrokeColor = WithAlpha(halo, 0.3f + (pulse * 0.2f));
        canvas.StrokeSize = 1.6f;
        canvas.DrawEllipse(
            rect.Center.X - (outerPulseSize * 0.5f),
            rect.Center.Y - (outerPulseSize * 0.5f),
            outerPulseSize,
            outerPulseSize);

        canvas.StrokeColor = accent;
        canvas.StrokeSize = 1.6f;

        canvas.DrawLine(rect.X + inset, rect.Y + inset, rect.X + inset + arm, rect.Y + inset);
        canvas.DrawLine(rect.X + inset, rect.Y + inset, rect.X + inset, rect.Y + inset + arm);

        canvas.DrawLine(rect.Right - inset, rect.Y + inset, rect.Right - inset - arm, rect.Y + inset);
        canvas.DrawLine(rect.Right - inset, rect.Y + inset, rect.Right - inset, rect.Y + inset + arm);

        canvas.DrawLine(rect.X + inset, rect.Bottom - inset, rect.X + inset + arm, rect.Bottom - inset);
        canvas.DrawLine(rect.X + inset, rect.Bottom - inset, rect.X + inset, rect.Bottom - inset - arm);

        canvas.DrawLine(rect.Right - inset, rect.Bottom - inset, rect.Right - inset - arm, rect.Bottom - inset);
        canvas.DrawLine(rect.Right - inset, rect.Bottom - inset, rect.Right - inset, rect.Bottom - inset - arm);

        canvas.FillColor = WithAlpha(accent, 0.9f);
        canvas.FillRoundedRectangle(
            rect.Center.X - (centerSize * 0.5f),
            rect.Center.Y - (centerSize * 0.5f),
            centerSize,
            centerSize,
            centerSize * 0.5f);

        float coreRing = rect.Width * (0.22f + (pulse * 0.04f));
        canvas.StrokeColor = WithAlpha(accent, 0.6f + (pulse * 0.2f));
        canvas.StrokeSize = 1.2f;
        canvas.DrawEllipse(
            rect.Center.X - (coreRing * 0.5f),
            rect.Center.Y - (coreRing * 0.5f),
            coreRing,
            coreRing);
    }

    private static void DrawMissSplash(ICanvas canvas, RectF rect, float phase, BoardCellVm cell)
    {
        Color foam = WithAlpha(cell.MissPegFillColor, 0.96f);
        Color rim = WithAlpha(cell.MissPegStrokeColor, 0.88f);
        Color glow = WithAlpha(Lighten(cell.MissPegCapColor, 0.26f), 0.18f);
        float pulse = (MathF.Sin((phase * 2.4f) + (cell.Row * 0.6f) + (cell.Col * 0.35f)) + 1f) * 0.5f;
        float ringSize = rect.Width * (0.48f + (pulse * 0.08f));
        float outerSize = rect.Width * (0.64f + (pulse * 0.1f));

        canvas.FillColor = glow;
        canvas.FillEllipse(rect.Center.X - (outerSize * 0.5f), rect.Center.Y - (outerSize * 0.5f), outerSize, outerSize);

        canvas.StrokeColor = rim;
        canvas.StrokeSize = 1.8f;
        canvas.DrawEllipse(rect.Center.X - (ringSize * 0.5f), rect.Center.Y - (ringSize * 0.5f), ringSize, ringSize);

        canvas.StrokeColor = WithAlpha(rim, 0.45f);
        canvas.StrokeSize = 1.1f;
        canvas.DrawEllipse(rect.Center.X - (outerSize * 0.5f), rect.Center.Y - (outerSize * 0.5f), outerSize, outerSize);

        float core = rect.Width * 0.24f;
        canvas.FillColor = foam;
        canvas.FillEllipse(rect.Center.X - (core * 0.5f), rect.Center.Y - (core * 0.5f), core, core);

        float cap = rect.Width * 0.12f;
        canvas.FillColor = WithAlpha(Lighten(foam, 0.12f), 0.94f);
        canvas.FillEllipse(rect.Center.X - (cap * 0.5f), rect.Center.Y - (cap * 0.9f), cap, cap);

        for (int droplet = 0; droplet < 3; droplet++)
        {
            float dropletSize = (rect.Width * 0.08f) + (droplet * 0.8f);
            float x = rect.Center.X + ((droplet - 1) * rect.Width * 0.16f);
            float y = rect.Center.Y - (rect.Height * (0.18f + (droplet * 0.03f))) - (pulse * 4f);
            canvas.FillColor = WithAlpha(foam, 0.76f - (droplet * 0.1f));
            canvas.FillEllipse(x - (dropletSize * 0.5f), y - (dropletSize * 0.5f), dropletSize, dropletSize);
        }
    }

    private void DrawHitBlast(ICanvas canvas, RectF rect, float phase, BoardCellVm cell)
    {
        float pulse = (MathF.Sin((phase * 3.2f) + (cell.Row * 0.42f) + (cell.Col * 0.27f)) + 1f) * 0.5f;
        float flameWidth = rect.Width * (0.76f + (pulse * 0.08f));
        float flameHeight = rect.Height * (0.72f + (pulse * 0.12f));
        float flameX = rect.Center.X - (flameWidth * 0.5f);
        float flameY = rect.Center.Y - (flameHeight * 0.2f);

        Color outerFlame = WithAlpha(Color.FromArgb("#ff6400"), 0.24f + (pulse * 0.1f));
        Color innerFlame = WithAlpha(Color.FromArgb("#ffd97a"), 0.3f + (pulse * 0.12f));

        canvas.FillColor = outerFlame;
        canvas.FillEllipse(flameX, flameY, flameWidth, flameHeight);

        canvas.FillColor = innerFlame;
        canvas.FillEllipse(
            rect.Center.X - (rect.Width * 0.26f),
            rect.Center.Y - (rect.Height * 0.08f),
            rect.Width * 0.52f,
            rect.Height * 0.44f);

        if (_explosionImage is not null)
        {
            float imageInset = rect.Width * 0.08f;
            canvas.SaveState();
            canvas.Rotate((float)cell.HitMarkerRotation, rect.Center.X, rect.Center.Y);
            canvas.DrawImage(
                _explosionImage,
                rect.X + imageInset,
                rect.Y + imageInset,
                rect.Width - (imageInset * 2),
                rect.Height - (imageInset * 2));
            canvas.RestoreState();
            return;
        }

        DrawFallbackBlast(canvas, rect, pulse);
    }

    private static void DrawFallbackBlast(ICanvas canvas, RectF rect, float pulse)
    {
        float radius = (rect.Width * 0.2f) + (pulse * 2.4f);
        Color spoke = WithAlpha(Color.FromArgb("#ffe6a6"), 0.95f);
        Color ember = WithAlpha(Color.FromArgb("#ff8429"), 0.78f);

        canvas.StrokeColor = spoke;
        canvas.StrokeSize = 2.2f;

        for (int spokeIndex = 0; spokeIndex < 8; spokeIndex++)
        {
            float angle = (MathF.PI * 2f * spokeIndex) / 8f;
            float x1 = rect.Center.X + (MathF.Cos(angle) * radius * 0.4f);
            float y1 = rect.Center.Y + (MathF.Sin(angle) * radius * 0.4f);
            float x2 = rect.Center.X + (MathF.Cos(angle) * radius * 1.4f);
            float y2 = rect.Center.Y + (MathF.Sin(angle) * radius * 1.4f);
            canvas.DrawLine(x1, y1, x2, y2);
        }

        canvas.FillColor = ember;
        canvas.FillEllipse(rect.Center.X - radius, rect.Center.Y - radius, radius * 2, radius * 2);
    }

    private async Task EnsureExplosionImageAsync()
    {
        if (_explosionImage is not null || _isExplosionImageLoading)
            return;

        _isExplosionImageLoading = true;

        try
        {
#if WINDOWS
            await using var stream = await ResolveExplosionImageStreamAsync().ConfigureAwait(false);
            if (stream is null)
            {
                CrashLog.Write("BoardEffectsView.LoadExplosionImage", null, "explosion.png stream was null");
                return;
            }

            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory).ConfigureAwait(false);
            memory.Position = 0;

            var imageLoader = new PlatformImageLoadingService();
            _explosionImage = imageLoader.FromStream(memory, ImageFormat.Png);
            CrashLog.Write("BoardEffectsView.LoadExplosionImage", null, $"Loaded=True Bytes={memory.Length}");
#endif
        }
        catch (Exception ex)
        {
            CrashLog.Write("BoardEffectsView.LoadExplosionImage", ex);
        }
        finally
        {
            _isExplosionImageLoading = false;
            MainThread.BeginInvokeOnMainThread(Invalidate);
        }
    }

    private static async Task<Stream?> ResolveExplosionImageStreamAsync()
    {
        string[] diskCandidates =
        {
            Path.Combine(AppContext.BaseDirectory, "explosion.png"),
            Path.Combine(AppContext.BaseDirectory, "Resources", "Images", "explosion.png"),
            SourceExplosionPath
        };

        foreach (var candidate in diskCandidates)
        {
            if (File.Exists(candidate))
                return File.OpenRead(candidate);
        }

        string[] packageCandidates =
        {
            "explosion.png",
            "Resources/Images/explosion.png"
        };

        foreach (var candidate in packageCandidates)
        {
            try
            {
                return await FileSystem.Current.OpenAppPackageFileAsync(candidate).ConfigureAwait(false);
            }
            catch
            {
                // Try the next path.
            }
        }

        try
        {
            return File.OpenRead(SourceExplosionPath);
        }
        catch
        {
            return null;
        }
    }

    private sealed class BoardEffectsDrawable(BoardEffectsView owner) : IDrawable
    {
        private readonly BoardEffectsView _owner = owner;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            _owner.DrawEffects(canvas, dirtyRect);
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.Red, color.Green, color.Blue, Clamp01(alpha));
    }

    private static Color Lighten(Color color, float amount)
    {
        amount = Clamp01(amount);
        return new Color(
            color.Red + ((1 - color.Red) * amount),
            color.Green + ((1 - color.Green) * amount),
            color.Blue + ((1 - color.Blue) * amount),
            color.Alpha);
    }

    private static float Clamp01(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}
