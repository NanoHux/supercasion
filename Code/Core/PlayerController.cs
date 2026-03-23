using Sandbox;

namespace TerrysCasino.Core;

/// <summary>
/// Third-person player controller for the casino lobby.
/// Handles WASD movement, mouse-look, and camera positioning.
/// Requires a CharacterController component on the same GameObject.
/// </summary>
public sealed class PlayerController : Component
{
	// -------------------------------------------------------------------------
	// Inspector Properties
	// -------------------------------------------------------------------------

	/// <summary>Ground movement speed in units per second.</summary>
	[Property] public float MoveSpeed { get; set; } = 200f;

	/// <summary>Mouse look sensitivity multiplier.</summary>
	[Property] public float MouseSensitivity { get; set; } = 0.15f;

	/// <summary>How far behind the player the camera sits.</summary>
	[Property] public float CameraDistance { get; set; } = 150f;

	/// <summary>How high above the player's origin the camera sits.</summary>
	[Property] public float CameraHeight { get; set; } = 80f;

	/// <summary>The camera GameObject to control. Assign in the Inspector.</summary>
	[Property] public GameObject CameraObject { get; set; }

	// -------------------------------------------------------------------------
	// Private State
	// -------------------------------------------------------------------------

	private Angles eyeAngles;
	private CharacterController characterController;

	// -------------------------------------------------------------------------
	// Lifecycle
	// -------------------------------------------------------------------------

	/// <inheritdoc/>
	protected override void OnStart()
	{
		characterController = Components.Get<CharacterController>();
		eyeAngles = WorldRotation.Angles();
	}

	/// <inheritdoc/>
	protected override void OnUpdate()
	{
		// Only the owning client drives this controller.
		if ( IsProxy ) return;

		HandleMouseLook();
		HandleMovement();
		UpdateThirdPersonCamera();
	}

	// -------------------------------------------------------------------------
	// Private Helpers
	// -------------------------------------------------------------------------

	/// <summary>Applies mouse delta to the eye-angles and rotates the player yaw.</summary>
	private void HandleMouseLook()
	{
		Mouse.Visible = false;

		eyeAngles.yaw   += Input.MouseDelta.x * MouseSensitivity;
		eyeAngles.pitch  = MathX.Clamp( eyeAngles.pitch - Input.MouseDelta.y * MouseSensitivity, -60f, 80f );
		eyeAngles.roll   = 0f;

		// Face the direction the player is steering.
		WorldRotation = Rotation.FromYaw( eyeAngles.yaw );
	}

	/// <summary>Reads WASD input and moves the character.</summary>
	private void HandleMovement()
	{
		float forward = ( Input.Down( "Forward"  ) ? 1f : 0f )
		              - ( Input.Down( "Backward" ) ? 1f : 0f );
		float right   = ( Input.Down( "Right"    ) ? 1f : 0f )
		              - ( Input.Down( "Left"     ) ? 1f : 0f );

		var wishDir = new Vector3( right, forward, 0f );

		// Convert local direction to world space using player yaw only.
		wishDir = Rotation.FromYaw( eyeAngles.yaw ) * wishDir;
		wishDir = wishDir.Normal * MoveSpeed;

		if ( characterController is not null )
		{
			characterController.Accelerate( wishDir );
			characterController.ApplyFriction( 8f );
			characterController.Move();
		}
		else
		{
			// Fallback: direct transform movement (no collision).
			WorldPosition += wishDir * Time.Delta;
		}
	}

	/// <summary>Positions the camera behind and above the player each frame.</summary>
	private void UpdateThirdPersonCamera()
	{
		if ( CameraObject is null ) return;

		var pivotLook    = Rotation.FromYaw( eyeAngles.yaw );
		var camOffset    = pivotLook * Vector3.Backward * CameraDistance
		                 + Vector3.Up * CameraHeight;

		var camPos       = WorldPosition + camOffset;
		var lookTarget   = WorldPosition + Vector3.Up * 40f; // aim at head height

		CameraObject.WorldPosition = camPos;
		CameraObject.WorldRotation = Rotation.LookAt( lookTarget - camPos );
	}
}
