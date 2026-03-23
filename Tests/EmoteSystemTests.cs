using TerrysCasino.Core;
using Xunit;

namespace TerrysCasino.Tests;

public class EmoteSystemTests
{
	[Fact]
	public void TrySendEmote_ValidEmote_ReturnsTrue()
	{
		var system = new EmoteSystem();
		Assert.True( system.TrySendEmote( "player1", 0, 0f ) );
	}

	[Fact]
	public void TrySendEmote_InvalidIndex_ReturnsFalse()
	{
		var system = new EmoteSystem();
		Assert.False( system.TrySendEmote( "player1", -1, 0f ) );
		Assert.False( system.TrySendEmote( "player1", 100, 0f ) );
	}

	[Fact]
	public void TrySendEmote_OnCooldown_ReturnsFalse()
	{
		var system = new EmoteSystem();
		Assert.True( system.TrySendEmote( "player1", 0, 0f ) );
		Assert.False( system.TrySendEmote( "player1", 1, 3f ) ); // 3s < 5s cooldown
	}

	[Fact]
	public void TrySendEmote_AfterCooldown_ReturnsTrue()
	{
		var system = new EmoteSystem();
		Assert.True( system.TrySendEmote( "player1", 0, 0f ) );
		Assert.True( system.TrySendEmote( "player1", 1, 5.1f ) ); // Past 5s
	}

	[Fact]
	public void TrySendEmote_DifferentPlayers_IndependentCooldowns()
	{
		var system = new EmoteSystem();
		Assert.True( system.TrySendEmote( "player1", 0, 0f ) );
		Assert.True( system.TrySendEmote( "player2", 0, 1f ) ); // Different player
		Assert.False( system.TrySendEmote( "player1", 0, 3f ) ); // Still on cooldown
	}

	[Fact]
	public void GetEmoteText_ValidIndex_ReturnsText()
	{
		Assert.Equal( "Nice hand", EmoteSystem.GetEmoteText( 0 ) );
		Assert.Equal( "GG", EmoteSystem.GetEmoteText( 1 ) );
		Assert.Equal( ":(", EmoteSystem.GetEmoteText( 7 ) );
	}

	[Fact]
	public void GetEmoteText_InvalidIndex_ReturnsNull()
	{
		Assert.Null( EmoteSystem.GetEmoteText( -1 ) );
		Assert.Null( EmoteSystem.GetEmoteText( 100 ) );
	}

	[Fact]
	public void GetCooldownRemaining_NoPriorEmote_ReturnsZero()
	{
		var system = new EmoteSystem();
		Assert.Equal( 0f, system.GetCooldownRemaining( "player1", 0f ) );
	}

	[Fact]
	public void GetCooldownRemaining_OnCooldown_ReturnsPositive()
	{
		var system = new EmoteSystem();
		system.TrySendEmote( "player1", 0, 10f );
		float remaining = system.GetCooldownRemaining( "player1", 12f );
		Assert.True( remaining > 0 );
		Assert.True( remaining <= 3f );
	}

	[Fact]
	public void GetCooldownRemaining_PastCooldown_ReturnsZero()
	{
		var system = new EmoteSystem();
		system.TrySendEmote( "player1", 0, 10f );
		Assert.Equal( 0f, system.GetCooldownRemaining( "player1", 20f ) );
	}

	[Fact]
	public void Emotes_Has8Options()
	{
		Assert.Equal( 8, EmoteSystem.Emotes.Length );
	}
}
