using Battleship.GameCore;

namespace BattleshipMaui.ViewModels;

public sealed class EnemyTargetingStrategy
{
    private readonly int _size;
    private readonly Random _random;
    private readonly CpuDifficulty _difficulty;
    private readonly Queue<BoardCoordinate> _huntQueue;
    private readonly LinkedList<BoardCoordinate> _targetQueue = new();
    private readonly HashSet<BoardCoordinate> _attempted = new();
    private readonly List<BoardCoordinate> _activeHits = new();
    private int _easyFocusTurnsRemaining;

    public int PendingTargetCount => _targetQueue.Count;
    public int AttemptedCount => _attempted.Count;
    public int RemainingShots => (_size * _size) - _attempted.Count;

    public EnemyTargetingStrategy(int size, Random random, CpuDifficulty difficulty = CpuDifficulty.Standard)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Board size must be greater than zero.");

        _size = size;
        _difficulty = difficulty;
        _random = random ?? new Random();

        var parityCells = new List<BoardCoordinate>();
        var nonParityCells = new List<BoardCoordinate>();

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var coordinate = new BoardCoordinate(row, col);
                if ((row + col) % 2 == 0)
                    parityCells.Add(coordinate);
                else
                    nonParityCells.Add(coordinate);
            }
        }

        Shuffle(parityCells, _random);
        Shuffle(nonParityCells, _random);

        _huntQueue = new Queue<BoardCoordinate>(parityCells.Concat(nonParityCells));
    }

    public BoardCoordinate GetNextShot()
    {
        bool mustUseTargetQueue = _huntQueue.Count == 0;
        if ((mustUseTargetQueue || ShouldUseTargetQueue()) && TryDequeueTargetShot(out var target))
        {
            if (_difficulty == CpuDifficulty.Easy && _easyFocusTurnsRemaining > 0)
                _easyFocusTurnsRemaining--;

            return target;
        }

        while (_huntQueue.Count > 0)
        {
            var candidate = _huntQueue.Dequeue();
            if (_attempted.Add(candidate))
                return candidate;
        }

        throw new InvalidOperationException("No remaining shots.");
    }

    public void RegisterShotOutcome(BoardCoordinate shot, AttackResult result)
    {
        _attempted.Add(shot);

        switch (result)
        {
            case AttackResult.Sunk:
                _activeHits.Clear();
                _targetQueue.Clear();
                _easyFocusTurnsRemaining = 0;
                return;

            case AttackResult.Hit:
                if (!_activeHits.Contains(shot))
                    _activeHits.Add(shot);

                if (_difficulty == CpuDifficulty.Easy)
                    _easyFocusTurnsRemaining = 1;

                RebuildTargetQueue();
                return;

            default:
                return;
        }
    }

    private void RebuildTargetQueue()
    {
        _targetQueue.Clear();

        if (_activeHits.Count == 0)
            return;

        if (_activeHits.Count == 1)
        {
            if (_difficulty == CpuDifficulty.Hard)
                AddAdjacentCandidatesByReach(_activeHits[0]);
            else
                AddAdjacentCandidates(_activeHits[0]);

            return;
        }

        bool sameRow = _activeHits.All(h => h.Row == _activeHits[0].Row);
        bool sameCol = _activeHits.All(h => h.Col == _activeHits[0].Col);

        if (sameRow)
        {
            int row = _activeHits[0].Row;
            int minCol = _activeHits.Min(h => h.Col);
            int maxCol = _activeHits.Max(h => h.Col);

            AddCandidate(row, minCol - 1);
            AddCandidate(row, maxCol + 1);
        }
        else if (sameCol)
        {
            int col = _activeHits[0].Col;
            int minRow = _activeHits.Min(h => h.Row);
            int maxRow = _activeHits.Max(h => h.Row);

            AddCandidate(minRow - 1, col);
            AddCandidate(maxRow + 1, col);
        }
        else
        {
            foreach (var hit in _activeHits)
            {
                if (_difficulty == CpuDifficulty.Hard)
                    AddAdjacentCandidatesByReach(hit);
                else
                    AddAdjacentCandidates(hit);
            }
        }

        if (_targetQueue.Count == 0)
        {
            foreach (var hit in _activeHits)
            {
                if (_difficulty == CpuDifficulty.Hard)
                    AddAdjacentCandidatesByReach(hit);
                else
                    AddAdjacentCandidates(hit);
            }
        }
    }

    private void AddAdjacentCandidates(BoardCoordinate hit)
    {
        AddCandidate(hit.Row - 1, hit.Col);
        AddCandidate(hit.Row + 1, hit.Col);
        AddCandidate(hit.Row, hit.Col - 1);
        AddCandidate(hit.Row, hit.Col + 1);
    }

    private void AddCandidate(int row, int col)
    {
        var candidate = new BoardCoordinate(row, col);
        if (!InBounds(candidate))
            return;

        if (_attempted.Contains(candidate))
            return;

        if (_targetQueue.Contains(candidate))
            return;

        _targetQueue.AddLast(candidate);
    }

    private void AddAdjacentCandidatesByReach(BoardCoordinate hit)
    {
        var rankedDirections = new List<(int Row, int Col, int Reach)>
        {
            BuildDirectionalCandidate(hit, -1, 0),
            BuildDirectionalCandidate(hit, 1, 0),
            BuildDirectionalCandidate(hit, 0, -1),
            BuildDirectionalCandidate(hit, 0, 1)
        };

        foreach (var candidate in rankedDirections
                     .Where(c => c.Reach >= 0)
                     .OrderByDescending(c => c.Reach)
                     .ThenBy(_ => _random.Next()))
        {
            AddCandidate(candidate.Row, candidate.Col);
        }
    }

    private (int Row, int Col, int Reach) BuildDirectionalCandidate(BoardCoordinate hit, int rowDelta, int colDelta)
    {
        int row = hit.Row + rowDelta;
        int col = hit.Col + colDelta;
        var candidate = new BoardCoordinate(row, col);
        if (!InBounds(candidate) || _attempted.Contains(candidate))
            return (row, col, -1);

        int reach = 0;
        int scanRow = row;
        int scanCol = col;
        while (InBounds(new BoardCoordinate(scanRow, scanCol)) &&
               !_attempted.Contains(new BoardCoordinate(scanRow, scanCol)))
        {
            reach++;
            scanRow += rowDelta;
            scanCol += colDelta;
        }

        return (row, col, reach);
    }

    private bool ShouldUseTargetQueue()
    {
        if (_targetQueue.Count == 0)
            return false;

        if (_difficulty != CpuDifficulty.Easy)
            return true;

        if (_easyFocusTurnsRemaining > 0)
            return true;

        return _random.NextDouble() < 0.4;
    }

    private bool TryDequeueTargetShot(out BoardCoordinate shot)
    {
        while (_targetQueue.Count > 0)
        {
            var candidate = _targetQueue.First!.Value;
            _targetQueue.RemoveFirst();

            if (_attempted.Add(candidate))
            {
                shot = candidate;
                return true;
            }
        }

        shot = default;
        return false;
    }

    private bool InBounds(BoardCoordinate coordinate)
    {
        return coordinate.Row >= 0 &&
               coordinate.Row < _size &&
               coordinate.Col >= 0 &&
               coordinate.Col < _size;
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
