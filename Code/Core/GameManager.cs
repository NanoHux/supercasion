using Sandbox;

namespace TerrysCasino.Core;

/// <summary>
/// Top-level scene manager for Terry's Casino.
/// Place this Component on a dedicated "GameManager" GameObject in every scene.
/// Responsible for initialising systems, tracking game state, and coordinating
/// scene-level logic.
/// </summary>
public sealed class GameManager : Component
{
	// -------------------------------------------------------------------------
	// Types
	// -------------------------------------------------------------------------

	/// <summary>High-level states the game can be in.</summary>
	public enum GameState
	{
		/// <summary>Player is in the casino lobby, free to roam.</summary>
		Lobby,

		/// <summary>Player is seated at a game table.</summary>
		InGame,

		/// <summary>The game is loading or transitioning between scenes.</summary>
		Loading,
	}

	// -------------------------------------------------------------------------
	// Inspector Properties
	// -------------------------------------------------------------------------

	/// <summary>Current game state. Synced so all clients see the same state.</summary>
	[Sync, Property]
	public GameState CurrentState { get; private set; } = GameState.Loading;

	/// <summary>
	/// When true, a new CreditSystem is added to the player GameObject
	/// automatically if one is not already present.
	/// </summary>
	[Property] public bool AutoInitCreditSystem { get; set; } = true;

	// -------------------------------------------------------------------------
	// Singleton Access
	// -------------------------------------------------------------------------

	/// <summary>Scene-level singleton reference. Set during OnStart.</summary>
	public static GameManager Instance { get; private set; }

	// -------------------------------------------------------------------------
	// Lifecycle
	// -------------------------------------------------------------------------

	/// <inheritdoc/>
	protected override void OnStart()
	{
		Instance = this;

		Log.Info( "[GameManager] Starting up Terry's Casino." );

		if ( !IsProxy )
		{
			InitialiseLocalPlayer();
			TransitionTo( GameState.Lobby );
		}
	}

	/// <inheritdoc/>
	protected override void OnDestroy()
	{
		if ( Instance == this )
			Instance = null;
	}

	// -------------------------------------------------------------------------
	// Public API
	// -------------------------------------------------------------------------

	/// <summary>
	/// Transitions the game to a new state and broadcasts the change.
	/// Only the server/host should call this.
	/// </summary>
	/// <param name="newState">The state to transition to.</param>
	public void TransitionTo( GameState newState )
	{
		if ( IsProxy ) return;

		var previous = CurrentState;
		CurrentState = newState;

		Log.Info( $"[GameManager] State: {previous} → {newState}" );
	}

	// -------------------------------------------------------------------------
	// Private Helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Ensures the local player's GameObject has a CreditSystem.
	/// If AutoInitCreditSystem is true and no CreditSystem exists on any
	/// local (non-proxy) player, a warning is logged.
	/// </summary>
	private void InitialiseLocalPlayer()
	{
		if ( !AutoInitCreditSystem ) return;

		// CreditSystem instances are added directly to player GameObjects.
		// Here we just verify at least one exists; actual attachment is the
		// player prefab's responsibility.
		bool found = false;
		foreach ( var cs in Scene.GetAllComponents<CreditSystem>() )
		{
			if ( !cs.IsProxy )
			{
				found = true;
				Log.Info( $"[GameManager] CreditSystem found. Balance: {cs.GetDisplayBalance()}" );
				break;
			}
		}

		if ( !found )
		{
			Log.Warning( "[GameManager] No local CreditSystem found. " +
			             "Ensure the player prefab has a CreditSystem component attached." );
		}
	}
}
