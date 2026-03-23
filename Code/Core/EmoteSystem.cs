using System;
using System.Collections.Generic;

namespace TerrysCasino.Core;

/// <summary>
/// Server-side emote rate limiting and validation.
/// Pure logic — no s&box dependencies.
/// </summary>
public class EmoteSystem
{
	public static readonly string[] Emotes = new[]
	{
		"Nice hand", "GG", "Bluff?", "Wow",
		"Thanks", "Oops", ":)", ":("
	};

	public const float CooldownSeconds = 5f;

	private readonly Dictionary<string, float> _lastEmoteTime = new();

	/// <summary>
	/// Try to send an emote. Returns false if on cooldown or invalid.
	/// </summary>
	public bool TrySendEmote( string playerId, int emoteIndex, float currentTime )
	{
		if ( emoteIndex < 0 || emoteIndex >= Emotes.Length )
			return false;

		if ( _lastEmoteTime.TryGetValue( playerId, out float lastTime ) )
		{
			if ( currentTime - lastTime < CooldownSeconds )
				return false;
		}

		_lastEmoteTime[playerId] = currentTime;
		return true;
	}

	/// <summary>
	/// Get emote text by index.
	/// </summary>
	public static string GetEmoteText( int index )
	{
		if ( index < 0 || index >= Emotes.Length )
			return null;
		return Emotes[index];
	}

	/// <summary>
	/// Get remaining cooldown for a player.
	/// </summary>
	public float GetCooldownRemaining( string playerId, float currentTime )
	{
		if ( _lastEmoteTime.TryGetValue( playerId, out float lastTime ) )
		{
			float remaining = CooldownSeconds - (currentTime - lastTime);
			return remaining > 0 ? remaining : 0;
		}
		return 0;
	}
}
