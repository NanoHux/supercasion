using System;
using System.IO;
using System.Linq;
using TerrysCasino.Core;
using TerrysCasino.Games.Poker;
using Xunit;

namespace TerrysCasino.Tests;

/// <summary>
/// End-to-end integration tests covering the full poker flow:
/// deal → betting rounds → showdown → CP award → audit → replay.
/// </summary>
public class IntegrationTests : IDisposable
{
	private readonly string _tempDir;
	private readonly AuditLogger _auditLogger;
	private readonly CompetitionPointSystem _cpSystem;
	private readonly HandReplay _handReplay;
	private readonly EmoteSystem _emoteSystem;

	public IntegrationTests()
	{
		_tempDir = Path.Combine( Path.GetTempPath(), $"integration_test_{Guid.NewGuid():N}" );
		_auditLogger = new AuditLogger( Path.Combine( _tempDir, "audit" ) );
		_cpSystem = new CompetitionPointSystem();
		_handReplay = new HandReplay( Path.Combine( _tempDir, "replay" ) );
		_emoteSystem = new EmoteSystem();
	}

	public void Dispose()
	{
		if ( Directory.Exists( _tempDir ) )
			Directory.Delete( _tempDir, true );
	}

	// ─── Full Hand Flow ───

	[Fact]
	public void FullHand_TwoPlayers_DealToShowdown_WithAuditAndCpAndReplay()
	{
		var rng = new Random( 42 );
		var hand = new PokerHand( 2, 0, 1, rng );
		hand.Start();

		Assert.Equal( PokerHandState.PreFlop, hand.State );
		Assert.NotNull( hand.GetHoleCards( 0 ) );
		Assert.NotNull( hand.GetHoleCards( 1 ) );

		// Pre-flop: both call
		ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Call, 20 ), "Player0" );
		ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Check ), "Player1" );

		Assert.Equal( PokerHandState.Flop, hand.State );
		Assert.Equal( 3, hand.CommunityCards.Count );

		// Flop: check-check
		ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Check ), "Player1" );
		ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Check ), "Player0" );

		Assert.Equal( PokerHandState.Turn, hand.State );
		Assert.Equal( 4, hand.CommunityCards.Count );

		// Turn: check-check
		ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Check ), "Player1" );
		ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Check ), "Player0" );

		Assert.Equal( PokerHandState.River, hand.State );
		Assert.Equal( 5, hand.CommunityCards.Count );

		// River: check-check
		ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Check ), "Player1" );
		ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Check ), "Player0" );

		// Should be at showdown → complete
		Assert.Equal( PokerHandState.Complete, hand.State );
		Assert.True( hand.Winnings.Count > 0 ); // Someone won

		// CP awarded (past flop, both not folded)
		foreach ( int winner in hand.CpAwardedTo )
		{
			int total = _cpSystem.AwardPoints( $"Player{winner}" );
			Assert.True( total > 0 );
		}

		// Save replay
		Assert.True( hand.Snapshots.Count > 0 );
		Assert.True( _handReplay.SaveHand( 1, 1, hand.Snapshots ) );

		// Verify replay loads
		var replay = _handReplay.LoadHand( 1, 1 );
		Assert.NotNull( replay );
		Assert.Equal( hand.Snapshots.Count, replay.Snapshots.Count );

		// Verify audit log was written
		var auditFiles = Directory.GetFiles( Path.Combine( _tempDir, "audit" ), "audit-*.jsonl" );
		Assert.Single( auditFiles );
		var auditLines = File.ReadAllLines( auditFiles[0] );
		Assert.True( auditLines.Length >= 6 ); // At least 6 actions logged
	}

	[Fact]
	public void FullHand_SixPlayers_AllFold_WinnerTakesAll()
	{
		var rng = new Random( 100 );
		var hand = new PokerHand( 6, 0, 2, rng );
		hand.Start();

		// Pre-flop: everyone folds except last player
		// Dealer=0, SB=1, BB=2. Action starts at 3.
		for ( int i = 3; i <= 5; i++ )
			ApplyAndAudit( hand, i, new PlayerAction( ActionType.Fold ), $"Player{i}" );

		ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Fold ), "Player0" );
		ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Fold ), "Player1" );

		// BB wins by default (everyone else folded)
		Assert.Equal( PokerHandState.Complete, hand.State );
		Assert.True( hand.Winnings.ContainsKey( 2 ) );

		// No CP (didn't reach flop)
		Assert.Empty( hand.CpAwardedTo );
	}

	[Fact]
	public void FullHand_AllIn_RunsOutBoard()
	{
		var rng = new Random( 77 );
		var hand = new PokerHand( 3, 0, 3, rng );
		hand.Start();

		// Pre-flop: all go all-in
		hand.ApplyAction( 0, new PlayerAction( ActionType.AllIn, 1000 ) );
		hand.ApplyAction( 1, new PlayerAction( ActionType.AllIn, 1000 ) );
		hand.ApplyAction( 2, new PlayerAction( ActionType.AllIn, 1000 ) );

		// Should auto-run to complete
		Assert.Equal( PokerHandState.Complete, hand.State );
		Assert.Equal( 5, hand.CommunityCards.Count );
		Assert.True( hand.Winnings.Count > 0 );

		// CP awarded (all in counts as past flop)
		Assert.True( hand.CpAwardedTo.Count > 0 );
	}

	[Fact]
	public void FullHand_RaiseAndCall_ThenShowdown()
	{
		var rng = new Random( 55 );
		var hand = new PokerHand( 2, 0, 4, rng );
		hand.Start();

		// Pre-flop: raise and call
		ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Raise, 60 ), "Player0" );
		ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Call, 60 ), "Player1" );

		Assert.Equal( PokerHandState.Flop, hand.State );

		// Flop through river: check-check
		for ( int round = 0; round < 3; round++ )
		{
			ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Check ), "Player1" );
			ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Check ), "Player0" );
		}

		Assert.Equal( PokerHandState.Complete, hand.State );
	}

	// ─── Edge Cases ───

	[Fact]
	public void ForceFold_DuringBetting_CompletesCorrectly()
	{
		var rng = new Random( 33 );
		var hand = new PokerHand( 3, 0, 5, rng );
		hand.Start();

		// Player 0 disconnects (force fold)
		hand.ForceFold( 0 );

		// Remaining players continue
		hand.ApplyAction( 1, new PlayerAction( ActionType.Call, 20 ) );
		hand.ApplyAction( 2, new PlayerAction( ActionType.Check ) );

		// Should proceed normally
		Assert.Equal( PokerHandState.Flop, hand.State );
		Assert.True( hand.IsFolded( 0 ) );
	}

	[Fact]
	public void SnapshotsAreSerializable()
	{
		var rng = new Random( 11 );
		var hand = new PokerHand( 2, 0, 6, rng );
		hand.Start();

		hand.ApplyAction( 0, new PlayerAction( ActionType.Call, 20 ) );
		hand.ApplyAction( 1, new PlayerAction( ActionType.Check ) );

		foreach ( var snapshot in hand.Snapshots )
		{
			string json = snapshot.Serialize();
			Assert.False( string.IsNullOrEmpty( json ) );

			var deserialized = HandSnapshot.Deserialize( json );
			Assert.NotNull( deserialized );
			Assert.Equal( snapshot.State, deserialized.State );
			Assert.Equal( snapshot.PotTotal, deserialized.PotTotal );
		}
	}

	// ─── CP System Integration ───

	[Fact]
	public void CpSystem_LeaderboardAfterMultipleHands()
	{
		var cp = new CompetitionPointSystem();

		// Simulate 3 players playing 5 hands
		for ( int h = 0; h < 5; h++ )
		{
			cp.AwardPoints( "Alice" );
			cp.AwardPoints( "Bob" );
		}
		// Charlie only played 2 hands
		cp.AwardPoints( "Charlie" );
		cp.AwardPoints( "Charlie" );

		Assert.Equal( 500, cp.GetPoints( "Alice" ) );
		Assert.Equal( 500, cp.GetPoints( "Bob" ) );
		Assert.Equal( 200, cp.GetPoints( "Charlie" ) );

		var board = cp.GetLeaderboard();
		Assert.Equal( 3, board.Count );
		Assert.Equal( 3, cp.GetRank( "Charlie" ) );
	}

	// ─── Emote System Integration ───

	[Fact]
	public void EmoteSystem_MultiPlayerCooldowns()
	{
		float time = 0f;

		Assert.True( _emoteSystem.TrySendEmote( "Alice", 0, time ) );
		Assert.True( _emoteSystem.TrySendEmote( "Bob", 1, time ) );

		time += 3f; // 3 seconds later
		Assert.False( _emoteSystem.TrySendEmote( "Alice", 2, time ) ); // Still on cooldown
		Assert.False( _emoteSystem.TrySendEmote( "Bob", 3, time ) );

		time += 3f; // 6 seconds total
		Assert.True( _emoteSystem.TrySendEmote( "Alice", 4, time ) ); // Cooldown expired
		Assert.True( _emoteSystem.TrySendEmote( "Bob", 5, time ) );
	}

	// ─── Audit + Replay Integration ───

	[Fact]
	public void AuditAndReplay_ConsistentData()
	{
		var rng = new Random( 99 );
		var hand = new PokerHand( 2, 0, 7, rng );
		hand.Start();

		// Play through
		ApplyAndAudit( hand, 0, new PlayerAction( ActionType.Call, 20 ), "Alice" );
		ApplyAndAudit( hand, 1, new PlayerAction( ActionType.Check ), "Bob" );

		// Save snapshots
		_handReplay.SaveHand( 7, 1, hand.Snapshots );

		// Verify both systems have consistent state
		var replay = _handReplay.LoadHand( 7, 1 );
		Assert.NotNull( replay );
		Assert.True( replay.Snapshots.Count >= 2 ); // At least PreFlop + Flop

		var auditFiles = Directory.GetFiles( Path.Combine( _tempDir, "audit" ), "audit-*.jsonl" );
		Assert.Single( auditFiles );
	}

	// ─── Helper ───

	private void ApplyAndAudit( PokerHand hand, int playerIndex, PlayerAction action, string playerId )
	{
		hand.ApplyAction( playerIndex, action );
		_auditLogger.LogEvent( 1, 1, playerId, action.Type.ToString(), action.Amount );
	}
}
