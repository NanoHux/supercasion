using Sandbox;

namespace TerrysCasino.Games.Poker;

/// <summary>
/// Client-side component on the PokerTable GameObject.
/// Shows "Press E to sit" tooltip when player is within range.
/// Handles E key input to request seat / leave table.
/// </summary>
public sealed class PokerTableInteraction : Component
{
	[Property] public PokerTable Table { get; set; }
	[Property] public float InteractionRange { get; set; } = 150f;

	private bool _isSeated;
	private bool _isInRange;

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( Table == null ) return;

		var localPlayer = Scene.GetAllComponents<Core.PlayerController>()
			.FirstOrDefault( p => !p.IsProxy );

		if ( localPlayer == null ) return;

		float dist = Vector3.DistanceBetween( localPlayer.WorldPosition, WorldPosition );
		_isInRange = dist <= InteractionRange;

		if ( _isInRange && Input.Pressed( "use" ) )
		{
			if ( _isSeated )
			{
				Table.RequestLeave( Connection.Local );
				_isSeated = false;
			}
			else
			{
				Table.RequestSeat( Connection.Local );
				_isSeated = true;
			}
		}
	}

	/// <summary>
	/// Returns true if the local player is within interaction range.
	/// Used by UI to show tooltip.
	/// </summary>
	public bool IsLocalPlayerInRange() => _isInRange && !_isSeated;

	/// <summary>
	/// Returns true if the local player is currently seated here.
	/// </summary>
	public bool IsLocalPlayerSeated() => _isSeated;
}
