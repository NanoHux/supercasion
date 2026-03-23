using TerrysCasino.Games.Poker;
using Xunit;

namespace TerrysCasino.Tests;

public class PokerHandTests
{
	private PokerHand CreateHand( int players = 3, int dealer = 0, int seed = 42 )
	{
		return new PokerHand( players, dealer, handId: 1, rng: new Random( seed ) );
	}

	// ─── Basic Flow ───

	[Fact]
	public void NewHand_StartsInWaiting()
	{
		var hand = CreateHand();
		Assert.Equal( PokerHandState.Waiting, hand.State );
	}

	[Fact]
	public void Start_MovesToPreFlop()
	{
		var hand = CreateHand();
		hand.Start();
		Assert.Equal( PokerHandState.PreFlop, hand.State );
	}

	[Fact]
	public void Start_DealsHoleCards()
	{
		var hand = CreateHand();
		hand.Start();

		for ( int i = 0; i < hand.PlayerCount; i++ )
		{
			Assert.Equal( 2, hand.GetHoleCards( i ).Length );
		}
	}

	[Fact]
	public void Start_PostsBlinds()
	{
		var hand = CreateHand( players: 3, dealer: 0 );
		hand.Start();

		// Dealer=0, SB=1, BB=2
		// Chips reflect after BettingRound deducts blinds
		Assert.Equal( 1000, hand.GetChips( 0 ) ); // Dealer hasn't bet yet
		Assert.Equal( 990, hand.GetChips( 1 ) );   // SB posted 10
		Assert.Equal( 980, hand.GetChips( 2 ) );   // BB posted 20
	}

	// ─── Full Hand Play-through ───

	[Fact]
	public void FullHand_AllCallToShowdown()
	{
		var hand = CreateHand();
		hand.Start();

		// Pre-flop: P0 calls, P1 calls, P2 checks
		Assert.True( hand.ApplyAction( 0, new PlayerAction( ActionType.Call ) ) );
		Assert.True( hand.ApplyAction( 1, new PlayerAction( ActionType.Call ) ) );
		Assert.True( hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) ) );

		Assert.Equal( PokerHandState.Flop, hand.State );
		Assert.Equal( 3, hand.CommunityCards.Count );

		// Flop: all check
		Assert.True( hand.ApplyAction( 1, new PlayerAction( ActionType.Check ) ) );
		Assert.True( hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) ) );
		Assert.True( hand.ApplyAction( 0, new PlayerAction( ActionType.Check ) ) );

		Assert.Equal( PokerHandState.Turn, hand.State );
		Assert.Equal( 4, hand.CommunityCards.Count );

		// Turn: all check
		Assert.True( hand.ApplyAction( 1, new PlayerAction( ActionType.Check ) ) );
		Assert.True( hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) ) );
		Assert.True( hand.ApplyAction( 0, new PlayerAction( ActionType.Check ) ) );

		Assert.Equal( PokerHandState.River, hand.State );
		Assert.Equal( 5, hand.CommunityCards.Count );

		// River: all check
		Assert.True( hand.ApplyAction( 1, new PlayerAction( ActionType.Check ) ) );
		Assert.True( hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) ) );
		Assert.True( hand.ApplyAction( 0, new PlayerAction( ActionType.Check ) ) );

		Assert.Equal( PokerHandState.Complete, hand.State );

		// Someone should have won
		Assert.True( hand.Winnings.Count > 0 );
		Assert.Equal( 60, hand.Winnings.Values.Sum() ); // 20 * 3 players
	}

	// ─── All Fold ───

	[Fact]
	public void AllFold_LastPlayerWins()
	{
		var hand = CreateHand();
		hand.Start();

		// P0 folds, P1 folds → P2 (BB) wins
		Assert.True( hand.ApplyAction( 0, new PlayerAction( ActionType.Fold ) ) );
		Assert.True( hand.ApplyAction( 1, new PlayerAction( ActionType.Fold ) ) );

		Assert.Equal( PokerHandState.Complete, hand.State );
		Assert.True( hand.Winnings.ContainsKey( 2 ) );
		Assert.Equal( 30, hand.Winnings[2] ); // SB(10) + BB(20) = 30 total pot
	}

	[Fact]
	public void AllFold_NoCP_BeforeFlop()
	{
		var hand = CreateHand();
		hand.Start();

		hand.ApplyAction( 0, new PlayerAction( ActionType.Fold ) );
		hand.ApplyAction( 1, new PlayerAction( ActionType.Fold ) );

		// CP only awarded if hand reaches flop
		Assert.Empty( hand.CpAwardedTo );
	}

	// ─── CP Award ───

	[Fact]
	public void CP_AwardedAfterFlop()
	{
		var hand = CreateHand();
		hand.Start();

		// Pre-flop: all call
		hand.ApplyAction( 0, new PlayerAction( ActionType.Call ) );
		hand.ApplyAction( 1, new PlayerAction( ActionType.Call ) );
		hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) );

		// Flop: P1 folds, others check
		hand.ApplyAction( 1, new PlayerAction( ActionType.Fold ) );
		hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) );
		hand.ApplyAction( 0, new PlayerAction( ActionType.Check ) );

		// Turn: all check
		hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) );
		hand.ApplyAction( 0, new PlayerAction( ActionType.Check ) );

		// River: all check
		hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) );
		hand.ApplyAction( 0, new PlayerAction( ActionType.Check ) );

		Assert.Equal( PokerHandState.Complete, hand.State );

		// Only non-folded players (0 and 2) get CP
		Assert.Contains( 0, hand.CpAwardedTo );
		Assert.Contains( 2, hand.CpAwardedTo );
		Assert.DoesNotContain( 1, hand.CpAwardedTo ); // Folded
	}

	// ─── Raise and Call ───

	[Fact]
	public void Raise_ThenCall_WorksCorrectly()
	{
		var hand = CreateHand();
		hand.Start();

		// Pre-flop: P0 raises 30 (call 20 + raise 30 = 50 total)
		Assert.True( hand.ApplyAction( 0, new PlayerAction( ActionType.Raise, 30 ) ) );
		// P1 calls
		Assert.True( hand.ApplyAction( 1, new PlayerAction( ActionType.Call ) ) );
		// P2 calls
		Assert.True( hand.ApplyAction( 2, new PlayerAction( ActionType.Call ) ) );

		Assert.Equal( PokerHandState.Flop, hand.State );
	}

	// ─── All-In ───

	[Fact]
	public void AllIn_RunsOutBoard()
	{
		var hand = CreateHand();
		hand.Start();

		// Pre-flop: everyone goes all-in
		hand.ApplyAction( 0, new PlayerAction( ActionType.AllIn ) );
		hand.ApplyAction( 1, new PlayerAction( ActionType.AllIn ) );
		hand.ApplyAction( 2, new PlayerAction( ActionType.AllIn ) );

		// Should run all community cards and complete
		Assert.Equal( PokerHandState.Complete, hand.State );
		Assert.Equal( 5, hand.CommunityCards.Count );
		Assert.Equal( 3000, hand.Winnings.Values.Sum() ); // All chips awarded
	}

	// ─── Snapshots ───

	[Fact]
	public void Snapshots_EmittedAtEveryTransition()
	{
		var hand = CreateHand();
		hand.Start();

		// Should have snapshots for: Dealing, PreFlop
		Assert.True( hand.Snapshots.Count >= 2 );
		Assert.Equal( PokerHandState.Dealing, hand.Snapshots[0].State );
		Assert.Equal( PokerHandState.PreFlop, hand.Snapshots[1].State );
	}

	[Fact]
	public void Snapshots_IncludeActions()
	{
		var hand = CreateHand();
		hand.Start();

		hand.ApplyAction( 0, new PlayerAction( ActionType.Call ) );

		// Latest snapshot should record the action
		var lastAction = hand.Snapshots.Last( s => s.ActionType != null );
		Assert.Equal( "Call", lastAction.ActionType );
		Assert.Equal( "0", lastAction.ActionPlayerIndex );
	}

	[Fact]
	public void Snapshots_Serializable()
	{
		var hand = CreateHand();
		hand.Start();

		var snapshot = hand.Snapshots[0];
		var json = snapshot.Serialize();
		var deserialized = HandSnapshot.Deserialize( json );

		Assert.Equal( snapshot.State, deserialized.State );
		Assert.Equal( snapshot.HandId, deserialized.HandId );
		Assert.Equal( snapshot.DealerIndex, deserialized.DealerIndex );
	}

	// ─── Edge Cases ───

	[Fact]
	public void TwoPlayers_HeadsUp()
	{
		var hand = CreateHand( players: 2, dealer: 0 );
		hand.Start();

		// Heads-up: dealer=0 is SB (acts first pre-flop), P1 is BB
		// P0 (SB/dealer) calls to match BB
		Assert.True( hand.ApplyAction( 0, new PlayerAction( ActionType.Call ) ) );
		// P1 (BB) checks
		Assert.True( hand.ApplyAction( 1, new PlayerAction( ActionType.Check ) ) );

		Assert.Equal( PokerHandState.Flop, hand.State );
	}

	[Fact]
	public void SixPlayers_Works()
	{
		var hand = CreateHand( players: 6, dealer: 0 );
		hand.Start();

		Assert.Equal( PokerHandState.PreFlop, hand.State );
		for ( int i = 0; i < 6; i++ )
			Assert.Equal( 2, hand.GetHoleCards( i ).Length );
	}

	[Fact]
	public void InvalidPlayerCount_Throws()
	{
		Assert.Throws<ArgumentException>( () => new PokerHand( 1, 0 ) );
		Assert.Throws<ArgumentException>( () => new PokerHand( 7, 0 ) );
	}

	[Fact]
	public void WrongPlayerAction_Rejected()
	{
		var hand = CreateHand();
		hand.Start();

		// P1 tries to act when it's P0's turn
		Assert.False( hand.ApplyAction( 1, new PlayerAction( ActionType.Call ) ) );
	}

	[Fact]
	public void ForceFold_Works()
	{
		var hand = CreateHand();
		hand.Start();

		// Force-fold current player (P0)
		Assert.True( hand.ForceFold( 0 ) );
		Assert.True( hand.IsFolded( 0 ) );
	}

	[Fact]
	public void CompletedHand_HasWinnerInfo()
	{
		var hand = CreateHand();
		hand.Start();

		// Quick fold to complete
		hand.ApplyAction( 0, new PlayerAction( ActionType.Fold ) );
		hand.ApplyAction( 1, new PlayerAction( ActionType.Fold ) );

		// Last snapshot should have winner info
		var lastSnapshot = hand.Snapshots.Last();
		Assert.Equal( PokerHandState.Complete, lastSnapshot.State );
		Assert.NotNull( lastSnapshot.WinnerIndices );
		Assert.True( lastSnapshot.WinnerIndices.Length > 0 );
	}
}
