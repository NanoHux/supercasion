using TerrysCasino.Games.Poker;
using Xunit;

namespace TerrysCasino.Tests;

public class BettingRoundTests
{
	private static BettingRound CreatePreFlop( int playerCount = 3, int bigBlind = 20 )
	{
		var chips = Enumerable.Repeat( 1000, playerCount ).ToArray();
		var folded = new bool[playerCount];
		var allIn = new bool[playerCount];

		// Blinds: P0 = dealer, P1 = SB (10), P2 = BB (20)
		int sbIndex = 1 % playerCount;
		int bbIndex = 2 % playerCount;
		var bets = new int[playerCount];
		bets[sbIndex] = 10;
		bets[bbIndex] = bigBlind;

		// UTG (P0 for 3 players) acts first pre-flop
		int firstToAct = 0;

		return new BettingRound( chips, folded, allIn, firstToAct, bigBlind, bets );
	}

	private static BettingRound CreatePostFlop( int playerCount = 3 )
	{
		var chips = Enumerable.Repeat( 1000, playerCount ).ToArray();
		var folded = new bool[playerCount];
		var allIn = new bool[playerCount];

		// Post-flop: first to act is P1 (left of dealer P0), no existing bets
		return new BettingRound( chips, folded, allIn, 1 );
	}

	// ─── Basic Flow ───

	[Fact]
	public void AllCheck_CompletesRound()
	{
		var round = CreatePostFlop();
		Assert.False( round.IsComplete );

		// P1, P2, P0 all check
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Check ) ) );
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Check ) ) );
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Check ) ) );

		Assert.True( round.IsComplete );
	}

	[Fact]
	public void CallAround_PreFlop()
	{
		var round = CreatePreFlop();

		// P0 (UTG) calls 20
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );
		// P1 (SB) calls 10 more to match 20
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );
		// P2 (BB) checks (already at 20)
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Check ) ) );

		Assert.True( round.IsComplete );
	}

	// ─── Fold ───

	[Fact]
	public void AllFoldExceptOne_CompletesRound()
	{
		var round = CreatePostFlop();

		// P1 bets (raise from 0)
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Raise, 50 ) ) );
		// P2 folds
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Fold ) ) );
		// P0 folds
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Fold ) ) );

		Assert.True( round.IsComplete );
		Assert.Equal( 1, round.RemainingPlayerCount );
	}

	// ─── Raise ───

	[Fact]
	public void Raise_ResetsAction()
	{
		var round = CreatePostFlop();

		// P1 checks
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Check ) ) );
		// P2 raises 50
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Raise, 50 ) ) );

		Assert.False( round.IsComplete ); // P0 and P1 need to respond

		// P0 calls
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );
		// P1 calls
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );

		Assert.True( round.IsComplete );
	}

	[Fact]
	public void ReRaise_ResetsActionAgain()
	{
		var round = CreatePostFlop();

		// P1 raises 50
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Raise, 50 ) ) );
		// P2 re-raises 100 (50 to call + 100 raise = 150 total)
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Raise, 100 ) ) );

		Assert.False( round.IsComplete );

		// P0 calls
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );
		// P1 calls
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );

		Assert.True( round.IsComplete );
	}

	// ─── All-In ───

	[Fact]
	public void AllIn_PlayerCannotActFurther()
	{
		var chips = new[] { 100, 1000, 1000 };
		var folded = new bool[3];
		var allIn = new bool[3];
		var round = new BettingRound( chips, folded, allIn, 0 );

		// P0 goes all-in for 100
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.AllIn ) ) );
		Assert.True( round.IsAllIn( 0 ) );
		Assert.Equal( 0, round.GetChips( 0 ) );

		// P1 calls
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );
		// P2 calls
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );

		Assert.True( round.IsComplete );
	}

	[Fact]
	public void AllPlayersAllIn_CompletesImmediately()
	{
		var chips = new[] { 100, 200, 300 };
		var folded = new bool[3];
		var allIn = new bool[3];
		var round = new BettingRound( chips, folded, allIn, 0 );

		Assert.True( round.ApplyAction( new PlayerAction( ActionType.AllIn ) ) );
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.AllIn ) ) );
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.AllIn ) ) );

		Assert.True( round.IsComplete );
	}

	// ─── Invalid Actions ───

	[Fact]
	public void CannotCheck_WhenBetExists()
	{
		var round = CreatePreFlop();
		// P0 (UTG) tries to check when BB is 20
		Assert.False( round.ApplyAction( new PlayerAction( ActionType.Check ) ) );
	}

	[Fact]
	public void CannotCall_WhenNoBet()
	{
		var round = CreatePostFlop();
		// P1 tries to call when no bet exists
		Assert.False( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );
	}

	[Fact]
	public void CannotAct_AfterRoundComplete()
	{
		var round = CreatePostFlop();
		round.ApplyAction( new PlayerAction( ActionType.Check ) );
		round.ApplyAction( new PlayerAction( ActionType.Check ) );
		round.ApplyAction( new PlayerAction( ActionType.Check ) );
		Assert.True( round.IsComplete );

		Assert.False( round.ApplyAction( new PlayerAction( ActionType.Check ) ) );
	}

	// ─── Chip Tracking ───

	[Fact]
	public void ChipsDeducted_OnCall()
	{
		var round = CreatePreFlop();
		int chipsBefore = round.GetChips( 0 );
		round.ApplyAction( new PlayerAction( ActionType.Call ) ); // P0 calls 20
		Assert.Equal( chipsBefore - 20, round.GetChips( 0 ) );
	}

	[Fact]
	public void ChipsDeducted_OnRaise()
	{
		var round = CreatePreFlop();
		// P0 raises: call 20 + raise 30 = 50 total
		round.ApplyAction( new PlayerAction( ActionType.Raise, 30 ) );
		Assert.Equal( 950, round.GetChips( 0 ) );
		Assert.Equal( 50, round.GetBetThisRound( 0 ) );
	}

	// ─── Edge Cases ───

	[Fact]
	public void TwoPlayers_HeadsUp()
	{
		var chips = new[] { 1000, 1000 };
		var folded = new bool[2];
		var allIn = new bool[2];
		var round = new BettingRound( chips, folded, allIn, 0 );

		// P0 raises 50
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Raise, 50 ) ) );
		// P1 calls
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Call ) ) );

		Assert.True( round.IsComplete );
	}

	[Fact]
	public void PreFolded_PlayersSkipped()
	{
		var chips = new[] { 1000, 1000, 1000 };
		var folded = new[] { false, true, false }; // P1 already folded
		var allIn = new bool[3];
		var round = new BettingRound( chips, folded, allIn, 0 );

		// Only P0 and P2 should act
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Check ) ) ); // P0
		Assert.True( round.ApplyAction( new PlayerAction( ActionType.Check ) ) ); // P2

		Assert.True( round.IsComplete );
	}

	[Fact]
	public void ValidActions_CorrectForCurrentState()
	{
		var round = CreatePostFlop();
		var actions = round.GetValidActions();

		Assert.Contains( ActionType.Fold, actions );
		Assert.Contains( ActionType.Check, actions ); // No bet yet
		Assert.Contains( ActionType.Raise, actions );
		Assert.Contains( ActionType.AllIn, actions );
		Assert.DoesNotContain( ActionType.Call, actions ); // Nothing to call
	}

	[Fact]
	public void ValidActions_AfterRaise()
	{
		var round = CreatePostFlop();
		round.ApplyAction( new PlayerAction( ActionType.Raise, 50 ) ); // P1 raises

		var actions = round.GetValidActions();
		Assert.Contains( ActionType.Fold, actions );
		Assert.Contains( ActionType.Call, actions ); // Now there's something to call
		Assert.Contains( ActionType.Raise, actions );
		Assert.Contains( ActionType.AllIn, actions );
		Assert.DoesNotContain( ActionType.Check, actions ); // Can't check with a bet
	}
}
