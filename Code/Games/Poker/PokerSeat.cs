using Sandbox;
using TerrysCasino.Games.Poker;

namespace TerrysCasino.Games.Poker;

/// <summary>
/// Represents one seat at a poker table.
/// Tracks the seated player's connection and state.
/// </summary>
public class PokerSeat
{
	public Connection Player { get; set; }
	public string DisplayName { get; set; } = "";
	public bool IsOccupied => Player != null;
	public bool IsReady { get; set; }
	public bool IsDisconnected { get; set; }
	public float DisconnectTime { get; set; }

	public void Clear()
	{
		Player = null;
		DisplayName = "";
		IsReady = false;
		IsDisconnected = false;
		DisconnectTime = 0;
	}
}
