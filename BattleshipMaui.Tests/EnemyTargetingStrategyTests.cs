using Battleship.GameCore;
using BattleshipMaui.ViewModels;

namespace BattleshipMaui.Tests;

public class EnemyTargetingStrategyTests
{
    [Fact]
    [Trait("Category", "Core9")]
    public void GetNextShot_ReturnsUniqueInBoundsCoordinates()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(5));
        var seen = new HashSet<BoardCoordinate>();

        for (int i = 0; i < 100; i++)
        {
            var shot = strategy.GetNextShot();
            Assert.InRange(shot.Row, 0, 9);
            Assert.InRange(shot.Col, 0, 9);
            Assert.True(seen.Add(shot));
        }

        Assert.Throws<InvalidOperationException>(() => strategy.GetNextShot());
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void RegisterHit_PrioritizesAdjacentTargetShots()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(9));
        var hit = new BoardCoordinate(4, 4);

        strategy.RegisterShotOutcome(hit, AttackResult.Hit);
        var next = strategy.GetNextShot();

        int manhattanDistance = Math.Abs(next.Row - hit.Row) + Math.Abs(next.Col - hit.Col);
        Assert.Equal(1, manhattanDistance);
    }

    [Fact]
    public void EasyMode_AfterHit_StillFocusesImmediateNextShotNearImpact()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(15), CpuDifficulty.Easy);
        var hit = new BoardCoordinate(6, 6);

        strategy.RegisterShotOutcome(hit, AttackResult.Hit);
        var next = strategy.GetNextShot();

        int manhattanDistance = Math.Abs(next.Row - hit.Row) + Math.Abs(next.Col - hit.Col);
        Assert.Equal(1, manhattanDistance);
    }

    [Fact]
    [Trait("Category", "Core9")]
    public void TwoAlignedHits_TargetsShipLineFirst()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(11));
        strategy.RegisterShotOutcome(new BoardCoordinate(3, 3), AttackResult.Hit);
        strategy.RegisterShotOutcome(new BoardCoordinate(3, 4), AttackResult.Hit);

        var next = strategy.GetNextShot();

        Assert.Equal(3, next.Row);
        Assert.True(next.Col == 2 || next.Col == 5);
    }

    [Fact]
    public void RegisterSunk_ClearsPendingTargetQueue()
    {
        var strategy = new EnemyTargetingStrategy(10, new Random(17));
        strategy.RegisterShotOutcome(new BoardCoordinate(5, 5), AttackResult.Hit);
        Assert.True(strategy.PendingTargetCount > 0);

        strategy.RegisterShotOutcome(new BoardCoordinate(5, 6), AttackResult.Sunk);

        Assert.Equal(0, strategy.PendingTargetCount);
    }
}
