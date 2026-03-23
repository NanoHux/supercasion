using TerrysCasino.Games.Poker;
using Xunit;

namespace TerrysCasino.Tests;

public class SidePotCalculatorTests
{
	[Fact]
	public void TwoPlayers_EqualContributions_OnePot()
	{
		var contributions = new[] { 100, 100 };
		var pots = SidePotCalculator.Calculate( contributions );

		Assert.Single( pots );
		Assert.Equal( 200, pots[0].Amount );
		Assert.Equal( 2, pots[0].EligiblePlayers.Count );
	}

	[Fact]
	public void TwoPlayers_OneAllIn_MainAndSidePot()
	{
		// Player 0 all-in for 50, Player 1 bet 100
		var contributions = new[] { 50, 100 };
		var pots = SidePotCalculator.Calculate( contributions );

		Assert.Equal( 2, pots.Count );

		// Main pot: 50 from each = 100, both eligible
		Assert.Equal( 100, pots[0].Amount );
		Assert.Contains( 0, pots[0].EligiblePlayers );
		Assert.Contains( 1, pots[0].EligiblePlayers );

		// Side pot: remaining 50 from Player 1
		Assert.Equal( 50, pots[1].Amount );
		Assert.Single( pots[1].EligiblePlayers );
		Assert.Contains( 1, pots[1].EligiblePlayers );
	}

	[Fact]
	public void ThreePlayers_AllDifferentContributions()
	{
		// P0: 30, P1: 60, P2: 100
		var contributions = new[] { 30, 60, 100 };
		var pots = SidePotCalculator.Calculate( contributions );

		Assert.Equal( 3, pots.Count );

		// Main: 30 from each = 90, all eligible
		Assert.Equal( 90, pots[0].Amount );
		Assert.Equal( 3, pots[0].EligiblePlayers.Count );

		// Side 1: 30 from P1 and P2 = 60, P1 and P2 eligible
		Assert.Equal( 60, pots[1].Amount );
		Assert.Equal( 2, pots[1].EligiblePlayers.Count );
		Assert.DoesNotContain( 0, pots[1].EligiblePlayers );

		// Side 2: 40 from P2 only = 40, only P2 eligible
		Assert.Equal( 40, pots[2].Amount );
		Assert.Single( pots[2].EligiblePlayers );
		Assert.Contains( 2, pots[2].EligiblePlayers );
	}

	[Fact]
	public void FoldedPlayer_NotEligible()
	{
		// P0: 100, P1: 100, P2: 50 (folded)
		var contributions = new[] { 100, 100, 50 };
		var folded = new HashSet<int> { 2 };
		var pots = SidePotCalculator.Calculate( contributions, folded );

		// Main pot includes P2's chips but P2 is not eligible
		Assert.Equal( 2, pots.Count );
		Assert.Equal( 150, pots[0].Amount ); // 50 from each
		Assert.DoesNotContain( 2, pots[0].EligiblePlayers );
		Assert.Contains( 0, pots[0].EligiblePlayers );
		Assert.Contains( 1, pots[0].EligiblePlayers );
	}

	[Fact]
	public void FourPlayers_MultipleAllIns()
	{
		// P0: 20, P1: 50, P2: 50, P3: 100
		var contributions = new[] { 20, 50, 50, 100 };
		var pots = SidePotCalculator.Calculate( contributions );

		Assert.Equal( 3, pots.Count );

		// Main: 20 from each = 80
		Assert.Equal( 80, pots[0].Amount );
		Assert.Equal( 4, pots[0].EligiblePlayers.Count );

		// Side 1: 30 from P1, P2, P3 = 90
		Assert.Equal( 90, pots[1].Amount );
		Assert.Equal( 3, pots[1].EligiblePlayers.Count );
		Assert.DoesNotContain( 0, pots[1].EligiblePlayers );

		// Side 2: 50 from P3 = 50
		Assert.Equal( 50, pots[2].Amount );
		Assert.Single( pots[2].EligiblePlayers );
		Assert.Contains( 3, pots[2].EligiblePlayers );
	}

	[Fact]
	public void AllEqualContributions_SinglePot()
	{
		var contributions = new[] { 200, 200, 200, 200 };
		var pots = SidePotCalculator.Calculate( contributions );

		Assert.Single( pots );
		Assert.Equal( 800, pots[0].Amount );
		Assert.Equal( 4, pots[0].EligiblePlayers.Count );
	}

	[Fact]
	public void NoContributions_NoPots()
	{
		var contributions = new[] { 0, 0, 0 };
		var pots = SidePotCalculator.Calculate( contributions );
		Assert.Empty( pots );
	}

	[Fact]
	public void AllFoldedExceptOne_ChipsDistributed()
	{
		// Everyone folded except P0. P0 contributed 50, P1 and P2 contributed 100 each.
		// In practice, when all others fold the game awards the pot to the last player
		// BEFORE side pot calculation. But if we do calculate:
		// Main pot (50 level): 50×3 = 150, only P0 eligible
		// Side pot (100 level): 50×2 = 100, no one eligible (both folded)
		var contributions = new[] { 50, 100, 100 };
		var folded = new HashSet<int> { 1, 2 };
		var pots = SidePotCalculator.Calculate( contributions, folded );

		Assert.Equal( 250, pots.Sum( p => p.Amount ) );

		// Main pot: P0 is the sole eligible player
		Assert.Contains( 0, pots[0].EligiblePlayers );

		// Side pot exists but has no eligible players (both folded)
		Assert.Equal( 2, pots.Count );
		Assert.Empty( pots[1].EligiblePlayers );
	}
}
