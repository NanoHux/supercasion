using Sandbox;

namespace TerrysCasino.Core;

/// <summary>
/// Manages the virtual credit balance for a player.
/// Awards 10,000 credits on first join and provides methods to add or deduct credits.
/// Attach this Component to the player GameObject.
/// </summary>
public sealed class CreditSystem : Component
{
	/// <summary>Starting credits awarded to every new player (one-time).</summary>
	private const long NewPlayerBonus = 10_000;

	/// <summary>The player's current virtual credit balance. Synced to all clients.</summary>
	[Sync, Property]
	public long Credits { get; private set; } = 0;

	/// <summary>Whether the new-player bonus has already been granted this session.</summary>
	[Sync]
	private bool BonusGranted { get; set; } = false;

	// -------------------------------------------------------------------------
	// Lifecycle
	// -------------------------------------------------------------------------

	/// <summary>Called once when the Component first becomes active.</summary>
	protected override void OnStart()
	{
		if ( !IsProxy && !BonusGranted )
		{
			Credits = NewPlayerBonus;
			BonusGranted = true;
			Log.Info( $"[CreditSystem] New player bonus granted: {Credits} credits." );
		}
	}

	// -------------------------------------------------------------------------
	// Public API
	// -------------------------------------------------------------------------

	/// <summary>
	/// Returns whether the player has at least <paramref name="amount"/> credits.
	/// Call this before <see cref="DeductCredits"/> to avoid a failed deduction.
	/// </summary>
	/// <param name="amount">Amount to check against the current balance.</param>
	public bool HasEnoughCredits( long amount ) => Credits >= amount;

	/// <summary>
	/// Adds the specified amount to the player's credit balance.
	/// Must be called on the network owner (server/host); the <see cref="Credits"/>
	/// [Sync] property propagates the new value to all clients automatically.
	/// </summary>
	/// <param name="amount">Number of credits to add. Must be greater than zero.</param>
	/// <returns>The new balance after the addition.</returns>
	public long AddCredits( long amount )
	{
		if ( amount <= 0 )
		{
			Log.Warning( $"[CreditSystem] AddCredits called with non-positive amount: {amount}. Ignored." );
			return Credits;
		}

		Credits += amount;
		Log.Info( $"[CreditSystem] +{amount} credits → balance: {Credits}" );
		return Credits;
	}

	/// <summary>
	/// Deducts the specified amount from the player's credit balance.
	/// Will not reduce the balance below zero.
	/// Must be called on the network owner (server/host); the <see cref="Credits"/>
	/// [Sync] property propagates the new value to all clients automatically.
	/// </summary>
	/// <param name="amount">Number of credits to deduct. Must be greater than zero.</param>
	/// <returns>
	/// <c>true</c> if the deduction succeeded (sufficient funds);
	/// <c>false</c> if the player did not have enough credits.
	/// </returns>
	public bool DeductCredits( long amount )
	{
		if ( amount <= 0 )
		{
			Log.Warning( $"[CreditSystem] DeductCredits called with non-positive amount: {amount}. Ignored." );
			return false;
		}

		if ( Credits < amount )
		{
			Log.Warning( $"[CreditSystem] Insufficient credits: need {amount}, have {Credits}." );
			return false;
		}

		Credits -= amount;
		Log.Info( $"[CreditSystem] -{amount} credits → balance: {Credits}" );
		return true;
	}

	/// <summary>
	/// Returns a formatted string of the current credit balance, e.g. "10,000".
	/// Useful for HUD display bindings.
	/// </summary>
	public string GetDisplayBalance() => Credits.ToString( "N0" );
}
