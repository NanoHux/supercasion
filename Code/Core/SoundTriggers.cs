using Sandbox;

namespace TerrysCasino.Core;

/// <summary>
/// Centralized sound trigger definitions for the casino.
/// Maps game events to sound assets. Call from UI or game components.
/// </summary>
public static class SoundTriggers
{
	// Card sounds
	public const string CardDeal = "sounds/card_deal.sound";
	public const string CardFlip = "sounds/card_flip.sound";

	// Chip sounds
	public const string ChipBet = "sounds/chip_bet.sound";
	public const string ChipPotWin = "sounds/chip_pot_win.sound";
	public const string ChipStack = "sounds/chip_stack.sound";

	// Action sounds
	public const string ActionFold = "sounds/action_fold.sound";
	public const string ActionCheck = "sounds/action_check.sound";
	public const string ActionCall = "sounds/action_call.sound";
	public const string ActionRaise = "sounds/action_raise.sound";
	public const string ActionAllIn = "sounds/action_allin.sound";

	// UI sounds
	public const string TimerWarning = "sounds/timer_warning.sound";
	public const string TimerExpired = "sounds/timer_expired.sound";
	public const string CpAward = "sounds/cp_award.sound";
	public const string EmoteSend = "sounds/emote_send.sound";

	// Ambient
	public const string TableAmbience = "sounds/table_ambience.sound";

	/// <summary>
	/// Play a sound at a world position.
	/// </summary>
	public static void PlayAt( string soundPath, Vector3 position )
	{
		Sound.Play( soundPath, position );
	}

	/// <summary>
	/// Play a UI sound (no spatial positioning).
	/// </summary>
	public static void PlayUI( string soundPath )
	{
		Sound.Play( soundPath );
	}
}
