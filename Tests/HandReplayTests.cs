using System;
using System.Collections.Generic;
using System.IO;
using TerrysCasino.Core;
using TerrysCasino.Games.Poker;
using Xunit;

namespace TerrysCasino.Tests;

public class HandReplayTests : IDisposable
{
	private readonly string _testDir;
	private readonly HandReplay _replay;

	public HandReplayTests()
	{
		_testDir = Path.Combine( Path.GetTempPath(), $"replay_test_{Guid.NewGuid():N}" );
		_replay = new HandReplay( _testDir );
	}

	public void Dispose()
	{
		if ( Directory.Exists( _testDir ) )
			Directory.Delete( _testDir, true );
	}

	[Fact]
	public void SaveHand_CreatesFile()
	{
		var snapshots = new List<HandSnapshot>
		{
			new HandSnapshot
			{
				State = PokerHandState.PreFlop,
				ChipStacks = new[] { 990, 980 },
				Folded = new[] { false, false },
				AllIn = new[] { false, false },
				CommunityCards = Array.Empty<string>(),
				PotTotal = 30,
				WinnerIndices = Array.Empty<int>()
			}
		};

		bool result = _replay.SaveHand( 1, 1, snapshots );
		Assert.True( result );
		Assert.True( Directory.Exists( _testDir ) );
		Assert.True( File.Exists( Path.Combine( _testDir, "table1_hand1.json" ) ) );
	}

	[Fact]
	public void LoadHand_ReturnsData()
	{
		var snapshots = new List<HandSnapshot>
		{
			new HandSnapshot
			{
				State = PokerHandState.PreFlop,
				ChipStacks = new[] { 990, 980 },
				Folded = new[] { false, false },
				AllIn = new[] { false, false },
				CommunityCards = Array.Empty<string>(),
				PotTotal = 30,
				WinnerIndices = Array.Empty<int>()
			},
			new HandSnapshot
			{
				State = PokerHandState.Showdown,
				ChipStacks = new[] { 1030, 970 },
				Folded = new[] { false, false },
				AllIn = new[] { false, false },
				CommunityCards = new[] { "Ah", "Kd", "7s", "2c", "9h" },
				PotTotal = 0,
				WinnerIndices = new[] { 0 }
			}
		};

		_replay.SaveHand( 42, 2, snapshots );
		var loaded = _replay.LoadHand( 42, 2 );

		Assert.NotNull( loaded );
		Assert.Equal( 42, loaded.HandId );
		Assert.Equal( 2, loaded.TableId );
		Assert.Equal( 2, loaded.Snapshots.Count );
		Assert.Equal( PokerHandState.PreFlop, loaded.Snapshots[0].State );
		Assert.Equal( PokerHandState.Showdown, loaded.Snapshots[1].State );
		Assert.Equal( 5, loaded.Snapshots[1].CommunityCards.Length );
	}

	[Fact]
	public void LoadHand_NonExistent_ReturnsNull()
	{
		var result = _replay.LoadHand( 999, 1 );
		Assert.Null( result );
	}

	[Fact]
	public void ListHands_ReturnsIdsDescending()
	{
		for ( int i = 1; i <= 5; i++ )
		{
			_replay.SaveHand( i, 1, new List<HandSnapshot> { MakeSnapshot() } );
		}

		var hands = _replay.ListHands( 1 );
		Assert.Equal( 5, hands.Count );
		Assert.Equal( 5, hands[0] );
		Assert.Equal( 1, hands[4] );
	}

	[Fact]
	public void ListHands_EmptyDir_ReturnsEmpty()
	{
		var hands = _replay.ListHands( 1 );
		Assert.Empty( hands );
	}

	[Fact]
	public void ListHands_FiltersByTableId()
	{
		_replay.SaveHand( 1, 1, new List<HandSnapshot> { MakeSnapshot() } );
		_replay.SaveHand( 2, 1, new List<HandSnapshot> { MakeSnapshot() } );
		_replay.SaveHand( 3, 2, new List<HandSnapshot> { MakeSnapshot() } );

		var table1 = _replay.ListHands( 1 );
		var table2 = _replay.ListHands( 2 );

		Assert.Equal( 2, table1.Count );
		Assert.Single( table2 );
	}

	private static HandSnapshot MakeSnapshot()
	{
		return new HandSnapshot
		{
			State = PokerHandState.Complete,
			ChipStacks = Array.Empty<int>(),
			Folded = Array.Empty<bool>(),
			AllIn = Array.Empty<bool>(),
			CommunityCards = Array.Empty<string>(),
			PotTotal = 0,
			WinnerIndices = Array.Empty<int>()
		};
	}
}
