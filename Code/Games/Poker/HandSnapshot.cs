using System.Text.Json;

namespace TerrysCasino.Games.Poker;

/// <summary>
/// Serializable snapshot of the full hand state at a transition point.
/// Used for hand replay system and audit trail.
/// Pure logic — no s&box dependencies.
/// </summary>
public class HandSnapshot
{
	public PokerHandState State { get; set; }
	public long TimestampUtc { get; set; }
	public int HandId { get; set; }

	// Player state
	public int[] ChipStacks { get; set; } = Array.Empty<int>();
	public bool[] Folded { get; set; } = Array.Empty<bool>();
	public bool[] AllIn { get; set; } = Array.Empty<bool>();

	// Cards (stored as strings like "Ah", "Kd")
	public string[][] HoleCards { get; set; } = Array.Empty<string[]>();
	public string[] CommunityCards { get; set; } = Array.Empty<string>();

	// Pot state
	public int PotTotal { get; set; }
	public int CurrentBet { get; set; }
	public int CurrentPlayerIndex { get; set; }
	public int DealerIndex { get; set; }

	// Action that triggered this snapshot (null for initial state)
	public string ActionPlayerIndex { get; set; }
	public string ActionType { get; set; }
	public int ActionAmount { get; set; }

	// Winners (only set at Showdown/Complete)
	public int[] WinnerIndices { get; set; }
	public string[] WinningHandDescriptions { get; set; }

	public string Serialize()
	{
		return JsonSerializer.Serialize( this );
	}

	public static HandSnapshot Deserialize( string json )
	{
		return JsonSerializer.Deserialize<HandSnapshot>( json );
	}
}
