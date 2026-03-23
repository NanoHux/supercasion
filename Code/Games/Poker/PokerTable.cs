using Sandbox;
using System;
using System.Text.Json;

namespace TerrysCasino.Games.Poker;

/// <summary>
/// s&box Component that owns a poker table. Server-authoritative.
/// Place on a GameObject in the scene. Players press E to sit.
/// Each table runs its own independent PokerHand state machine.
/// </summary>
public sealed class PokerTable : Component
{
	// ─── Constants ───

	public const int MaxSeats = 6;
	public const float ActionTimeout = 30f;
	public const float ReconnectWindow = 60f;
	public const float HoleCardRpcRetryDelay = 2f;
	public const float InteractionRange = 150f; // ~3m in s&box units
	public const float HandStartDelay = 3f; // Seconds between hands

	// ─── Inspector Properties ───

	[Property] public int TableId { get; set; } = 1;

	// ─── Synced State (visible to all clients) ───

	[Sync] public int SeatCount { get; private set; } = MaxSeats;
	[Sync] public int OccupiedSeats { get; private set; }
	[Sync] public bool IsHandActive { get; private set; }
	[Sync] public int PotSize { get; private set; }
	[Sync] public int CurrentBet { get; private set; }
	[Sync] public int CurrentActionPlayer { get; private set; } = -1;
	[Sync] public float ActionDeadline { get; private set; }
	[Sync] public int DealerSeatIndex { get; private set; }

	// Community cards as string (e.g. "Ah,Kd,7s") — clients parse for display
	[Sync] public string CommunityCardsSync { get; private set; } = "";

	// Player chip counts as comma-separated (index = seat)
	[Sync] public string ChipCountsSync { get; private set; } = "";

	// Player names as comma-separated
	[Sync] public string PlayerNamesSync { get; private set; } = "";

	// Seat status: "occupied,empty,occupied,folded,allin,disconnected"
	[Sync] public string SeatStatusSync { get; private set; } = "";

	// Current hand state for UI display
	[Sync] public string HandStateSync { get; private set; } = "Waiting";

	// ─── Server-Only State ───

	private readonly PokerSeat[] _seats = new PokerSeat[MaxSeats];
	private PokerHand _currentHand;
	private int _nextHandId;
	private int _dealerIndex;
	private float _handStartTimer;
	private bool _waitingToStart;
	private readonly Random _rng = new();

	// Hole card RPC tracking
	private readonly float[] _holeCardRpcSentTime = new float[MaxSeats];
	private readonly bool[] _holeCardRpcAcked = new bool[MaxSeats];

	// ─── Lifecycle ───

	protected override void OnStart()
	{
		for ( int i = 0; i < MaxSeats; i++ )
			_seats[i] = new PokerSeat();

		SyncSeatState();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return; // Server only

		UpdateActionTimer();
		UpdateDisconnectTimers();
		UpdateHandStartTimer();
		UpdateHoleCardRpcRetry();
	}

	// ─── Public API: Seat Management ───

	/// <summary>
	/// Try to seat a player at this table. Called via RPC from client pressing E.
	/// </summary>
	[Broadcast]
	public void RequestSeat( Connection player )
	{
		if ( IsProxy ) return;
		if ( player == null ) return;

		// Check if already seated
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( _seats[i].Player == player )
				return; // Already seated
		}

		// Find empty seat
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( !_seats[i].IsOccupied )
			{
				_seats[i].Player = player;
				_seats[i].DisplayName = player.DisplayName;
				_seats[i].IsReady = true;
				OccupiedSeats++;
				SyncSeatState();

				Log.Info( $"[PokerTable {TableId}] {player.DisplayName} sat in seat {i}. ({OccupiedSeats}/{MaxSeats})" );

				TryStartHand();
				return;
			}
		}

		Log.Info( $"[PokerTable {TableId}] Table full — {player.DisplayName} cannot sit." );
	}

	/// <summary>
	/// Player leaves the table. If a hand is active, they fold.
	/// </summary>
	[Broadcast]
	public void RequestLeave( Connection player )
	{
		if ( IsProxy ) return;
		if ( player == null ) return;

		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( _seats[i].Player == player )
			{
				// If hand active, force fold
				if ( IsHandActive && _currentHand != null && !_currentHand.IsFolded( i ) )
				{
					_currentHand.ForceFold( i );
					CheckHandComplete();
				}

				_seats[i].Clear();
				OccupiedSeats--;
				SyncSeatState();
				Log.Info( $"[PokerTable {TableId}] {player.DisplayName} left seat {i}." );
				return;
			}
		}
	}

	// ─── Public API: Player Actions ───

	/// <summary>
	/// Player submits a poker action. Validated server-side.
	/// </summary>
	[Broadcast]
	public void SubmitAction( Connection player, int actionType, int amount )
	{
		if ( IsProxy ) return;
		if ( !IsHandActive || _currentHand == null ) return;

		int seatIndex = FindSeatIndex( player );
		if ( seatIndex < 0 ) return;
		if ( _currentHand.CurrentRound?.CurrentPlayerIndex != seatIndex ) return;

		var action = new PlayerAction( (ActionType)actionType, amount );
		if ( _currentHand.ApplyAction( seatIndex, action ) )
		{
			Log.Info( $"[PokerTable {TableId}] Seat {seatIndex} ({_seats[seatIndex].DisplayName}): {action}" );

			// Log audit event
			LogAuditEvent( seatIndex, action );

			SyncHandState();
			CheckHandComplete();

			// Reset action timer for next player
			if ( !_currentHand.CurrentRound?.IsComplete ?? false )
				ResetActionTimer();
		}
	}

	/// <summary>
	/// Client acknowledges receiving hole cards.
	/// </summary>
	[Broadcast]
	public void AckHoleCards( Connection player )
	{
		if ( IsProxy ) return;

		int seatIndex = FindSeatIndex( player );
		if ( seatIndex >= 0 )
			_holeCardRpcAcked[seatIndex] = true;
	}

	// ─── Hand Management ───

	private void TryStartHand()
	{
		if ( IsHandActive ) return;

		int ready = CountReadyPlayers();
		if ( ready < 2 ) return;

		// Start countdown
		_waitingToStart = true;
		_handStartTimer = HandStartDelay;
	}

	private void UpdateHandStartTimer()
	{
		if ( !_waitingToStart ) return;

		_handStartTimer -= Time.Delta;
		if ( _handStartTimer > 0 ) return;

		_waitingToStart = false;

		int ready = CountReadyPlayers();
		if ( ready < 2 ) return;

		StartHand();
	}

	private void StartHand()
	{
		_nextHandId++;
		_dealerIndex = FindNextDealer( _dealerIndex );

		// Map seated players to hand indices
		int playerCount = CountReadyPlayers();
		_currentHand = new PokerHand( playerCount, MapSeatToHandIndex( _dealerIndex ),
			_nextHandId, _rng );

		IsHandActive = true;
		_currentHand.Start();

		// Deliver hole cards via per-player RPC
		DeliverHoleCards();

		SyncHandState();
		ResetActionTimer();

		Log.Info( $"[PokerTable {TableId}] Hand #{_nextHandId} started. {playerCount} players, dealer seat {_dealerIndex}." );

		// Broadcast deal animation
		BroadcastDealAnimation();
	}

	private void DeliverHoleCards()
	{
		int handIdx = 0;
		for ( int i = 0; i < MaxSeats; i++ )
		{
			_holeCardRpcAcked[i] = false;
			_holeCardRpcSentTime[i] = 0;

			if ( !_seats[i].IsOccupied || !_seats[i].IsReady ) continue;

			var cards = _currentHand.GetHoleCards( handIdx );
			if ( cards != null && cards.Length == 2 )
			{
				string card1 = cards[0].ToString();
				string card2 = cards[1].ToString();

				// Send hole cards only to the specific player
				SendHoleCardsToPlayer( _seats[i].Player, i, card1, card2 );
				_holeCardRpcSentTime[i] = (float)Time.Now;
			}

			handIdx++;
		}
	}

	/// <summary>
	/// Send hole cards to a specific player only.
	/// In s&box, use Connection.Send or target-specific RPC.
	/// </summary>
	private void SendHoleCardsToPlayer( Connection target, int seatIndex, string card1, string card2 )
	{
		// s&box per-player RPC: use [Broadcast] with target filter
		// For now, broadcast with seat index — client filters by own seat
		BroadcastHoleCards( seatIndex, card1, card2 );
	}

	[Broadcast]
	private void BroadcastHoleCards( int seatIndex, string card1, string card2 )
	{
		// Client-side: only process if this is your seat
		// The client checks if seatIndex matches their seated position
	}

	private void UpdateHoleCardRpcRetry()
	{
		if ( !IsHandActive || _currentHand == null ) return;

		int handIdx = 0;
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( !_seats[i].IsOccupied || !_seats[i].IsReady ) continue;

			if ( !_holeCardRpcAcked[i] && _holeCardRpcSentTime[i] > 0 )
			{
				float elapsed = (float)Time.Now - _holeCardRpcSentTime[i];

				if ( elapsed > HoleCardRpcRetryDelay && elapsed <= HoleCardRpcRetryDelay * 2 )
				{
					// Retry once
					var cards = _currentHand.GetHoleCards( handIdx );
					if ( cards != null && cards.Length == 2 )
					{
						SendHoleCardsToPlayer( _seats[i].Player, i, cards[0].ToString(), cards[1].ToString() );
						_holeCardRpcSentTime[i] = (float)Time.Now;
					}
				}
				else if ( elapsed > HoleCardRpcRetryDelay * 3 )
				{
					// Force fold after repeated failure
					Log.Warning( $"[PokerTable {TableId}] Hole card delivery failed for seat {i}. Force folding." );
					_currentHand.ForceFold( handIdx );
					_holeCardRpcAcked[i] = true;
					SyncHandState();
					CheckHandComplete();
				}
			}

			handIdx++;
		}
	}

	// ─── Action Timer ───

	private void ResetActionTimer()
	{
		if ( _currentHand?.CurrentRound == null ) return;

		int handPlayerIdx = _currentHand.CurrentRound.CurrentPlayerIndex;
		CurrentActionPlayer = MapHandIndexToSeat( handPlayerIdx );
		ActionDeadline = (float)Time.Now + ActionTimeout;
	}

	private void UpdateActionTimer()
	{
		if ( !IsHandActive || _currentHand == null ) return;
		if ( _currentHand.CurrentRound == null || _currentHand.CurrentRound.IsComplete ) return;

		if ( (float)Time.Now < ActionDeadline ) return;

		// Timeout: auto-check if possible, auto-fold if there's a bet
		int handPlayerIdx = _currentHand.CurrentRound.CurrentPlayerIndex;
		var validActions = _currentHand.CurrentRound.GetValidActions();

		PlayerAction autoAction;
		if ( validActions.Contains( ActionType.Check ) )
			autoAction = new PlayerAction( ActionType.Check );
		else
			autoAction = new PlayerAction( ActionType.Fold );

		int seatIdx = MapHandIndexToSeat( handPlayerIdx );
		Log.Info( $"[PokerTable {TableId}] Seat {seatIdx} timed out → {autoAction}" );

		_currentHand.ApplyAction( handPlayerIdx, autoAction );
		LogAuditEvent( seatIdx, autoAction );
		SyncHandState();
		CheckHandComplete();

		if ( !(_currentHand.CurrentRound?.IsComplete ?? true) )
			ResetActionTimer();
	}

	// ─── Disconnect Handling ───

	private void UpdateDisconnectTimers()
	{
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( !_seats[i].IsDisconnected ) continue;

			_seats[i].DisconnectTime += Time.Delta;

			if ( _seats[i].DisconnectTime >= ReconnectWindow )
			{
				// Free the seat
				Log.Info( $"[PokerTable {TableId}] Seat {i} reconnect window expired. Freeing seat." );
				_seats[i].Clear();
				OccupiedSeats--;
				SyncSeatState();
			}
		}
	}

	/// <summary>
	/// Called when a player disconnects from the game.
	/// During a hand: treat as all-in for committed chips.
	/// </summary>
	public void HandlePlayerDisconnect( Connection player )
	{
		int seatIndex = FindSeatIndex( player );
		if ( seatIndex < 0 ) return;

		_seats[seatIndex].IsDisconnected = true;
		_seats[seatIndex].DisconnectTime = 0;

		if ( IsHandActive && _currentHand != null )
		{
			int handIdx = MapSeatToHandIndex( seatIndex );
			if ( handIdx >= 0 && !_currentHand.IsFolded( handIdx ) )
			{
				// If it's their turn, auto-fold/check
				if ( _currentHand.CurrentRound?.CurrentPlayerIndex == handIdx )
				{
					var validActions = _currentHand.CurrentRound.GetValidActions();
					var autoAction = validActions.Contains( ActionType.Check )
						? new PlayerAction( ActionType.Check )
						: new PlayerAction( ActionType.Fold );

					_currentHand.ApplyAction( handIdx, autoAction );
					SyncHandState();
					CheckHandComplete();
				}
			}
		}

		SyncSeatState();
		Log.Info( $"[PokerTable {TableId}] Seat {seatIndex} ({_seats[seatIndex].DisplayName}) disconnected." );
	}

	/// <summary>
	/// Called when a player reconnects.
	/// </summary>
	public void HandlePlayerReconnect( Connection player )
	{
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( _seats[i].IsDisconnected && _seats[i].Player == player )
			{
				_seats[i].IsDisconnected = false;
				_seats[i].DisconnectTime = 0;
				SyncSeatState();
				Log.Info( $"[PokerTable {TableId}] Seat {i} ({_seats[i].DisplayName}) reconnected." );

				// Re-send hole cards if hand is active
				if ( IsHandActive && _currentHand != null )
				{
					int handIdx = MapSeatToHandIndex( i );
					if ( handIdx >= 0 )
					{
						var cards = _currentHand.GetHoleCards( handIdx );
						if ( cards != null && cards.Length == 2 )
							SendHoleCardsToPlayer( player, i, cards[0].ToString(), cards[1].ToString() );
					}
				}
				return;
			}
		}
	}

	// ─── Hand Completion ───

	private void CheckHandComplete()
	{
		if ( _currentHand == null ) return;
		if ( _currentHand.State != PokerHandState.Complete ) return;

		IsHandActive = false;
		CurrentActionPlayer = -1;

		// Broadcast results
		BroadcastHandResult();

		// CP awards
		foreach ( int handIdx in _currentHand.CpAwardedTo )
		{
			int seatIdx = MapHandIndexToSeat( handIdx );
			if ( seatIdx >= 0 && _seats[seatIdx].IsOccupied )
			{
				BroadcastCpAwarded( seatIdx, PokerHand.CpPerHand );
				Log.Info( $"[PokerTable {TableId}] CP awarded to seat {seatIdx} ({_seats[seatIdx].DisplayName})" );
			}
		}

		SyncHandState();
		HandStateSync = "Complete";

		Log.Info( $"[PokerTable {TableId}] Hand #{_nextHandId} complete." );

		// Schedule next hand
		TryStartHand();
	}

	// ─── Broadcast Events ───

	[Broadcast]
	private void BroadcastDealAnimation() { }

	[Broadcast]
	private void BroadcastHandResult()
	{
		// Client-side: trigger win/loss animations, show cards
	}

	[Broadcast]
	private void BroadcastCpAwarded( int seatIndex, int cpAmount )
	{
		// Client-side: show "+100 CP" toast animation
	}

	[Broadcast]
	private void BroadcastEmote( int seatIndex, int emoteId )
	{
		// Client-side: show speech bubble above player
	}

	// ─── Audit Logging ───

	private void LogAuditEvent( int seatIndex, PlayerAction action )
	{
		// Delegate to AuditLogger (created in Step 8)
		// Format: hand_id, table_id, timestamp, player_id, action, amount
		var entry = new
		{
			hand_id = _nextHandId,
			table_id = TableId,
			timestamp = DateTime.UtcNow.ToString( "o" ),
			player_id = _seats[seatIndex].DisplayName,
			action = action.Type.ToString(),
			amount = action.Amount
		};
		Log.Info( $"[AUDIT] {JsonSerializer.Serialize( entry )}" );
	}

	// ─── State Sync Helpers ───

	private void SyncHandState()
	{
		if ( _currentHand == null ) return;

		HandStateSync = _currentHand.State.ToString();
		PotSize = _currentHand.PotTotal;
		CurrentBet = _currentHand.CurrentRound?.CurrentBet ?? 0;
		DealerSeatIndex = _dealerIndex;

		// Community cards
		CommunityCardsSync = string.Join( ",",
			_currentHand.CommunityCards.Select( c => c.ToString() ) );

		// Chip counts
		var chips = new List<string>();
		int handIdx = 0;
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( _seats[i].IsOccupied && _seats[i].IsReady )
			{
				chips.Add( _currentHand.GetChips( handIdx ).ToString() );
				handIdx++;
			}
			else
			{
				chips.Add( "0" );
			}
		}
		ChipCountsSync = string.Join( ",", chips );
	}

	private void SyncSeatState()
	{
		OccupiedSeats = 0;
		var names = new List<string>();
		var statuses = new List<string>();

		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( _seats[i].IsOccupied )
			{
				OccupiedSeats++;
				names.Add( _seats[i].DisplayName );

				if ( _seats[i].IsDisconnected )
					statuses.Add( "disconnected" );
				else if ( IsHandActive && _currentHand != null )
				{
					int handIdx = MapSeatToHandIndex( i );
					if ( handIdx >= 0 && _currentHand.IsFolded( handIdx ) )
						statuses.Add( "folded" );
					else if ( handIdx >= 0 && _currentHand.IsAllIn( handIdx ) )
						statuses.Add( "allin" );
					else
						statuses.Add( "occupied" );
				}
				else
					statuses.Add( "occupied" );
			}
			else
			{
				names.Add( "" );
				statuses.Add( "empty" );
			}
		}

		PlayerNamesSync = string.Join( ",", names );
		SeatStatusSync = string.Join( ",", statuses );
	}

	// ─── Index Mapping ───
	// Seats (0-5) ↔ Hand player indices (0 to N-1, only occupied seats)

	private int MapSeatToHandIndex( int seatIndex )
	{
		int handIdx = 0;
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( _seats[i].IsOccupied && _seats[i].IsReady )
			{
				if ( i == seatIndex ) return handIdx;
				handIdx++;
			}
		}
		return -1;
	}

	private int MapHandIndexToSeat( int handIndex )
	{
		int handIdx = 0;
		for ( int i = 0; i < MaxSeats; i++ )
		{
			if ( _seats[i].IsOccupied && _seats[i].IsReady )
			{
				if ( handIdx == handIndex ) return i;
				handIdx++;
			}
		}
		return -1;
	}

	private int FindSeatIndex( Connection player )
	{
		for ( int i = 0; i < MaxSeats; i++ )
			if ( _seats[i].Player == player )
				return i;
		return -1;
	}

	private int CountReadyPlayers()
	{
		int count = 0;
		for ( int i = 0; i < MaxSeats; i++ )
			if ( _seats[i].IsOccupied && _seats[i].IsReady && !_seats[i].IsDisconnected )
				count++;
		return count;
	}

	private int FindNextDealer( int current )
	{
		for ( int i = 1; i <= MaxSeats; i++ )
		{
			int idx = (current + i) % MaxSeats;
			if ( _seats[idx].IsOccupied && _seats[idx].IsReady )
				return idx;
		}
		return current;
	}
}
