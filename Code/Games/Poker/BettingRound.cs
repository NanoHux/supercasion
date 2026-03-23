namespace TerrysCasino.Games.Poker;

/// <summary>
/// Manages one betting round (pre-flop, flop, turn, or river).
/// Tracks who has acted, current bet level, and when the round completes.
/// Pure logic — no s&box dependencies.
/// </summary>
public class BettingRound
{
	private readonly int _playerCount;
	private readonly int[] _chips;
	private readonly int[] _betsThisRound;
	private readonly bool[] _folded;
	private readonly bool[] _allIn;
	private readonly bool[] _hasActed;
	private int _currentBet;
	private int _currentPlayerIndex;
	private int _lastRaiserIndex;
	private bool _isComplete;

	public int CurrentPlayerIndex => _currentPlayerIndex;
	public int CurrentBet => _currentBet;
	public bool IsComplete => _isComplete;
	public int PlayerCount => _playerCount;

	public int GetChips( int playerIndex ) => _chips[playerIndex];
	public int GetBetThisRound( int playerIndex ) => _betsThisRound[playerIndex];
	public bool IsFolded( int playerIndex ) => _folded[playerIndex];
	public bool IsAllIn( int playerIndex ) => _allIn[playerIndex];

	/// <summary>
	/// Total chips contributed by all players in this round.
	/// </summary>
	public int TotalBetsThisRound => _betsThisRound.Sum();

	/// <summary>
	/// Number of players still active (not folded, not all-in).
	/// </summary>
	public int ActivePlayerCount
	{
		get
		{
			int count = 0;
			for ( int i = 0; i < _playerCount; i++ )
				if ( !_folded[i] && !_allIn[i] )
					count++;
			return count;
		}
	}

	/// <summary>
	/// Number of players not folded (includes all-in players).
	/// </summary>
	public int RemainingPlayerCount
	{
		get
		{
			int count = 0;
			for ( int i = 0; i < _playerCount; i++ )
				if ( !_folded[i] )
					count++;
			return count;
		}
	}

	/// <summary>
	/// Create a new betting round.
	/// </summary>
	/// <param name="chips">Each player's chip stack.</param>
	/// <param name="folded">Which players already folded in a prior round.</param>
	/// <param name="allIn">Which players are already all-in from a prior round.</param>
	/// <param name="firstToAct">Index of the first player to act.</param>
	/// <param name="initialBet">Current bet level (e.g., big blind amount for pre-flop).</param>
	/// <param name="betsAlreadyPlaced">Bets already placed (e.g., blinds for pre-flop).</param>
	public BettingRound( int[] chips, bool[] folded, bool[] allIn, int firstToAct,
		int initialBet = 0, int[] betsAlreadyPlaced = null )
	{
		_playerCount = chips.Length;
		_chips = (int[])chips.Clone();
		_folded = (bool[])folded.Clone();
		_allIn = (bool[])allIn.Clone();
		_betsThisRound = new int[_playerCount];
		_hasActed = new bool[_playerCount];
		_currentBet = initialBet;
		_lastRaiserIndex = -1;

		if ( betsAlreadyPlaced != null )
		{
			for ( int i = 0; i < _playerCount; i++ )
			{
				_betsThisRound[i] = betsAlreadyPlaced[i];
				_chips[i] -= betsAlreadyPlaced[i];
			}
		}

		// Mark folded and all-in players as having acted
		for ( int i = 0; i < _playerCount; i++ )
		{
			if ( _folded[i] || _allIn[i] )
				_hasActed[i] = true;
		}

		_currentPlayerIndex = firstToAct;
		AdvanceToNextActive();
		CheckComplete();
	}

	/// <summary>
	/// Get the valid actions for the current player.
	/// </summary>
	public List<ActionType> GetValidActions()
	{
		if ( _isComplete )
			return new List<ActionType>();

		var actions = new List<ActionType>();
		int toCall = _currentBet - _betsThisRound[_currentPlayerIndex];

		actions.Add( ActionType.Fold );

		if ( toCall <= 0 )
			actions.Add( ActionType.Check );
		else
			actions.Add( ActionType.Call );

		if ( _chips[_currentPlayerIndex] > toCall )
			actions.Add( ActionType.Raise );

		actions.Add( ActionType.AllIn );

		return actions;
	}

	/// <summary>
	/// Get the minimum raise amount (must be at least the size of the last raise, or the big blind).
	/// </summary>
	public int GetMinRaise( int bigBlind = 20 )
	{
		int toCall = _currentBet - _betsThisRound[_currentPlayerIndex];
		int minRaiseIncrement = Math.Max( bigBlind, _currentBet > 0 ? _currentBet : bigBlind );
		int minRaiseTotal = toCall + minRaiseIncrement;
		return Math.Min( minRaiseTotal, _chips[_currentPlayerIndex] );
	}

	/// <summary>
	/// Apply a player action. Returns true if the action was valid and applied.
	/// </summary>
	public bool ApplyAction( PlayerAction action )
	{
		if ( _isComplete )
			return false;

		int pi = _currentPlayerIndex;
		if ( _folded[pi] || _allIn[pi] )
			return false;

		int toCall = _currentBet - _betsThisRound[pi];

		switch ( action.Type )
		{
			case ActionType.Fold:
				_folded[pi] = true;
				break;

			case ActionType.Check:
				if ( toCall > 0 )
					return false; // Can't check when there's a bet to call
				break;

			case ActionType.Call:
				if ( toCall <= 0 )
					return false; // Nothing to call
				int callAmount = Math.Min( toCall, _chips[pi] );
				_chips[pi] -= callAmount;
				_betsThisRound[pi] += callAmount;
				if ( _chips[pi] == 0 )
					_allIn[pi] = true;
				break;

			case ActionType.Raise:
				if ( action.Amount <= 0 )
					return false;
				int totalToAdd = toCall + action.Amount;
				if ( totalToAdd > _chips[pi] )
					return false; // Not enough chips (should use AllIn)
				_chips[pi] -= totalToAdd;
				_betsThisRound[pi] += totalToAdd;
				_currentBet = _betsThisRound[pi];
				_lastRaiserIndex = pi;
				// Reset hasActed for others (they need to respond to the raise)
				for ( int i = 0; i < _playerCount; i++ )
				{
					if ( i != pi && !_folded[i] && !_allIn[i] )
						_hasActed[i] = false;
				}
				break;

			case ActionType.AllIn:
				int allInAmount = _chips[pi];
				_chips[pi] = 0;
				_betsThisRound[pi] += allInAmount;
				_allIn[pi] = true;
				if ( _betsThisRound[pi] > _currentBet )
				{
					_currentBet = _betsThisRound[pi];
					_lastRaiserIndex = pi;
					for ( int i = 0; i < _playerCount; i++ )
					{
						if ( i != pi && !_folded[i] && !_allIn[i] )
							_hasActed[i] = false;
					}
				}
				break;

			default:
				return false;
		}

		_hasActed[pi] = true;
		AdvanceToNextPlayer();
		CheckComplete();

		return true;
	}

	private void AdvanceToNextPlayer()
	{
		for ( int i = 0; i < _playerCount; i++ )
		{
			_currentPlayerIndex = (_currentPlayerIndex + 1) % _playerCount;
			if ( !_folded[_currentPlayerIndex] && !_allIn[_currentPlayerIndex] && !_hasActed[_currentPlayerIndex] )
				return;
		}
	}

	private void AdvanceToNextActive()
	{
		// If current player is already active and hasn't acted, stay
		if ( !_folded[_currentPlayerIndex] && !_allIn[_currentPlayerIndex] && !_hasActed[_currentPlayerIndex] )
			return;

		AdvanceToNextPlayer();
	}

	private void CheckComplete()
	{
		if ( _isComplete )
			return;

		// Only one player left (all others folded)
		if ( RemainingPlayerCount <= 1 )
		{
			_isComplete = true;
			return;
		}

		// No active players left (all are folded or all-in)
		if ( ActivePlayerCount == 0 )
		{
			_isComplete = true;
			return;
		}

		// All active players have acted and bets are matched
		bool allActed = true;
		for ( int i = 0; i < _playerCount; i++ )
		{
			if ( _folded[i] || _allIn[i] )
				continue;
			if ( !_hasActed[i] || _betsThisRound[i] < _currentBet )
			{
				allActed = false;
				break;
			}
		}

		if ( allActed )
			_isComplete = true;
	}
}
