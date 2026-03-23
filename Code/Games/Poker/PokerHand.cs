namespace TerrysCasino.Games.Poker;

/// <summary>
/// Orchestrates a single poker hand from deal through showdown and payout.
/// Emits a HandSnapshot at each state transition for replay.
/// Pure logic — no s&box dependencies. The PokerTable (s&box Component) wraps this.
/// </summary>
public class PokerHand
{
	public const int StartingChips = 1000;
	public const int SmallBlind = 10;
	public const int BigBlind = 20;
	public const int CpPerHand = 100;

	private readonly int _playerCount;
	private readonly int _dealerIndex;
	private readonly Deck _deck;
	private readonly List<HandSnapshot> _snapshots = new();

	private PokerHandState _state = PokerHandState.Waiting;
	private int[] _chips;
	private bool[] _folded;
	private bool[] _allIn;
	private Card[][] _holeCards;
	private List<Card> _communityCards = new();
	private int[] _totalContributions; // Total chips put in by each player across all rounds
	private BettingRound _currentRound;
	private int _handId;

	// Results
	private List<Pot> _pots;
	private Dictionary<int, int> _winnings = new(); // playerIndex → chips won
	private Dictionary<int, HandResult> _handResults = new(); // playerIndex → evaluated hand
	private List<int> _cpAwardedTo = new(); // players who earned CP

	public PokerHandState State => _state;
	public int PlayerCount => _playerCount;
	public int DealerIndex => _dealerIndex;
	public IReadOnlyList<HandSnapshot> Snapshots => _snapshots;
	public BettingRound CurrentRound => _currentRound;
	public IReadOnlyList<Card> CommunityCards => _communityCards;
	public IReadOnlyDictionary<int, int> Winnings => _winnings;
	public IReadOnlyList<int> CpAwardedTo => _cpAwardedTo;

	public int GetChips( int playerIndex ) => _chips[playerIndex];
	public bool IsFolded( int playerIndex ) => _folded[playerIndex];
	public bool IsAllIn( int playerIndex ) => _allIn[playerIndex];
	public Card[] GetHoleCards( int playerIndex ) => _holeCards[playerIndex];

	public int PotTotal => _totalContributions?.Sum() ?? 0;

	public PokerHand( int playerCount, int dealerIndex, int handId = 0, Random rng = null )
	{
		if ( playerCount < 2 || playerCount > 6 )
			throw new ArgumentException( "Player count must be 2-6." );

		_playerCount = playerCount;
		_dealerIndex = dealerIndex % playerCount;
		_handId = handId;
		_deck = new Deck( rng );

		_chips = new int[playerCount];
		_folded = new bool[playerCount];
		_allIn = new bool[playerCount];
		_holeCards = new Card[playerCount][];
		_totalContributions = new int[playerCount];

		for ( int i = 0; i < playerCount; i++ )
		{
			_chips[i] = StartingChips;
			_holeCards[i] = Array.Empty<Card>();
		}
	}

	/// <summary>
	/// Start the hand: shuffle, post blinds, deal hole cards.
	/// </summary>
	public void Start()
	{
		if ( _state != PokerHandState.Waiting )
			throw new InvalidOperationException( $"Cannot start hand in state {_state}" );

		_deck.Shuffle();
		TransitionTo( PokerHandState.Dealing );

		// Post blinds (heads-up: dealer = SB, other = BB)
		int sbIndex, bbIndex;
		if ( _playerCount == 2 )
		{
			sbIndex = _dealerIndex;
			bbIndex = NextActivePlayer( _dealerIndex );
		}
		else
		{
			sbIndex = NextActivePlayer( _dealerIndex );
			bbIndex = NextActivePlayer( sbIndex );
		}

		RecordBlind( sbIndex, SmallBlind );
		RecordBlind( bbIndex, BigBlind );

		// Deal 2 hole cards to each player
		for ( int i = 0; i < _playerCount; i++ )
		{
			int pi = ((_dealerIndex + 1) + i) % _playerCount;
			_holeCards[pi] = new[] { _deck.Deal(), _deck.Deal() };
		}

		// Start pre-flop betting
		// Heads-up: SB (dealer) acts first pre-flop
		int firstToAct = _playerCount == 2 ? sbIndex : NextActivePlayer( bbIndex );
		var betsPlaced = new int[_playerCount];
		betsPlaced[sbIndex] = SmallBlind;
		betsPlaced[bbIndex] = BigBlind;

		TransitionTo( PokerHandState.PreFlop );
		_currentRound = new BettingRound( _chips, _folded, _allIn, firstToAct, BigBlind, betsPlaced );
		SyncFromBettingRound(); // Sync chip deductions from blinds
	}

	/// <summary>
	/// Apply a player action in the current betting round.
	/// Returns true if valid and applied.
	/// </summary>
	public bool ApplyAction( int playerIndex, PlayerAction action )
	{
		if ( _state != PokerHandState.PreFlop && _state != PokerHandState.Flop
			&& _state != PokerHandState.Turn && _state != PokerHandState.River )
			return false;

		if ( _currentRound == null || _currentRound.IsComplete )
			return false;

		if ( _currentRound.CurrentPlayerIndex != playerIndex )
			return false;

		if ( !_currentRound.ApplyAction( action ) )
			return false;

		EmitSnapshot( playerIndex.ToString(), action.Type.ToString(), action.Amount );

		// Sync state from betting round
		SyncFromBettingRound();

		if ( _currentRound.IsComplete )
			AdvanceState();

		return true;
	}

	/// <summary>
	/// Force-fold a player (used for timeout or RPC failure).
	/// </summary>
	public bool ForceFold( int playerIndex )
	{
		return ApplyAction( playerIndex, new PlayerAction( ActionType.Fold ) );
	}

	private void AdvanceState()
	{
		// Collect contributions from this round
		for ( int i = 0; i < _playerCount; i++ )
			_totalContributions[i] += _currentRound.GetBetThisRound( i );

		// Check if only one player remains (all others folded)
		int remaining = 0;
		int lastPlayer = -1;
		for ( int i = 0; i < _playerCount; i++ )
		{
			if ( !_folded[i] )
			{
				remaining++;
				lastPlayer = i;
			}
		}

		if ( remaining <= 1 )
		{
			// All fold → skip to award
			AwardPots();
			return;
		}

		// Check if all remaining players are all-in (no more betting possible)
		bool allInOrFolded = true;
		for ( int i = 0; i < _playerCount; i++ )
		{
			if ( !_folded[i] && !_allIn[i] )
			{
				allInOrFolded = false;
				break;
			}
		}

		switch ( _state )
		{
			case PokerHandState.PreFlop:
				DealCommunity( 3 ); // Flop
				if ( allInOrFolded )
				{
					TransitionTo( PokerHandState.Flop );
					DealCommunity( 1 ); // Turn
					TransitionTo( PokerHandState.Turn );
					DealCommunity( 1 ); // River
					TransitionTo( PokerHandState.River );
					DoShowdown();
				}
				else
				{
					TransitionTo( PokerHandState.Flop );
					StartNewBettingRound();
				}
				break;

			case PokerHandState.Flop:
				DealCommunity( 1 ); // Turn
				if ( allInOrFolded )
				{
					TransitionTo( PokerHandState.Turn );
					DealCommunity( 1 ); // River
					TransitionTo( PokerHandState.River );
					DoShowdown();
				}
				else
				{
					TransitionTo( PokerHandState.Turn );
					StartNewBettingRound();
				}
				break;

			case PokerHandState.Turn:
				DealCommunity( 1 ); // River
				if ( allInOrFolded )
				{
					TransitionTo( PokerHandState.River );
					DoShowdown();
				}
				else
				{
					TransitionTo( PokerHandState.River );
					StartNewBettingRound();
				}
				break;

			case PokerHandState.River:
				DoShowdown();
				break;
		}
	}

	private void DealCommunity( int count )
	{
		for ( int i = 0; i < count; i++ )
			_communityCards.Add( _deck.Deal() );
	}

	private void StartNewBettingRound()
	{
		int firstToAct = NextActivePlayer( _dealerIndex );
		_currentRound = new BettingRound( _chips, _folded, _allIn, firstToAct );
	}

	private void DoShowdown()
	{
		TransitionTo( PokerHandState.Showdown );

		// Evaluate hands for all non-folded players
		for ( int i = 0; i < _playerCount; i++ )
		{
			if ( _folded[i] ) continue;

			var allCards = new List<Card>( _holeCards[i] );
			allCards.AddRange( _communityCards );
			_handResults[i] = HandEvaluator.Evaluate( allCards );
		}

		AwardPots();
	}

	private void AwardPots()
	{
		var foldedSet = new HashSet<int>();
		for ( int i = 0; i < _playerCount; i++ )
			if ( _folded[i] )
				foldedSet.Add( i );

		_pots = SidePotCalculator.Calculate( _totalContributions, foldedSet );

		foreach ( var pot in _pots )
		{
			if ( pot.EligiblePlayers.Count == 0 )
				continue;

			if ( pot.EligiblePlayers.Count == 1 )
			{
				// Uncontested — single player wins
				int winner = pot.EligiblePlayers[0];
				_winnings[winner] = _winnings.GetValueOrDefault( winner ) + pot.Amount;
			}
			else if ( _handResults.Count > 0 )
			{
				// Showdown — compare hands
				var eligibleResults = pot.EligiblePlayers
					.Where( p => _handResults.ContainsKey( p ) )
					.Select( p => _handResults[p] )
					.ToList();

				var eligibleIndices = pot.EligiblePlayers
					.Where( p => _handResults.ContainsKey( p ) )
					.ToList();

				var winners = HandEvaluator.FindWinners( eligibleResults );
				int share = pot.Amount / winners.Count;
				int remainder = pot.Amount % winners.Count;

				for ( int w = 0; w < winners.Count; w++ )
				{
					int winnerIdx = eligibleIndices[winners[w]];
					int amount = share + (w == 0 ? remainder : 0); // First winner gets remainder
					_winnings[winnerIdx] = _winnings.GetValueOrDefault( winnerIdx ) + amount;
				}
			}
			else
			{
				// All-fold: single remaining player
				int winner = pot.EligiblePlayers[0];
				_winnings[winner] = _winnings.GetValueOrDefault( winner ) + pot.Amount;
			}
		}

		// Award CP to all non-folded players who participated past dealing
		// Per design: disconnected all-in players earn CP only if they won
		TransitionTo( PokerHandState.CPAward );

		bool reachedFlop = _communityCards.Count >= 3;

		for ( int i = 0; i < _playerCount; i++ )
		{
			// CP only if hand reached flop (per design doc)
			if ( !reachedFlop ) continue;

			// Non-folded players always get CP
			if ( !_folded[i] )
			{
				_cpAwardedTo.Add( i );
			}
		}

		TransitionTo( PokerHandState.Complete );
	}

	private void RecordBlind( int playerIndex, int amount )
	{
		// Blinds are deducted by BettingRound via betsAlreadyPlaced.
		// We do NOT deduct here — just note the amount for the BettingRound constructor.
		// _chips stays at starting value; BettingRound handles deduction.
	}

	private int NextActivePlayer( int fromIndex )
	{
		for ( int i = 1; i <= _playerCount; i++ )
		{
			int idx = (fromIndex + i) % _playerCount;
			if ( !_folded[idx] )
				return idx;
		}
		return fromIndex; // Shouldn't happen
	}

	private void SyncFromBettingRound()
	{
		for ( int i = 0; i < _playerCount; i++ )
		{
			_chips[i] = _currentRound.GetChips( i );
			_folded[i] = _currentRound.IsFolded( i );
			_allIn[i] = _currentRound.IsAllIn( i );
		}
	}

	private void TransitionTo( PokerHandState newState )
	{
		_state = newState;
		EmitSnapshot( null, null, 0 );
	}

	private void EmitSnapshot( string actionPlayerIndex, string actionType, int actionAmount )
	{
		var snapshot = new HandSnapshot
		{
			State = _state,
			TimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			HandId = _handId,
			ChipStacks = (int[])_chips.Clone(),
			Folded = (bool[])_folded.Clone(),
			AllIn = (bool[])_allIn.Clone(),
			HoleCards = _holeCards.Select( h => h.Select( c => c.ToString() ).ToArray() ).ToArray(),
			CommunityCards = _communityCards.Select( c => c.ToString() ).ToArray(),
			PotTotal = PotTotal,
			CurrentBet = _currentRound?.CurrentBet ?? 0,
			CurrentPlayerIndex = _currentRound?.CurrentPlayerIndex ?? -1,
			DealerIndex = _dealerIndex,
			ActionPlayerIndex = actionPlayerIndex,
			ActionType = actionType,
			ActionAmount = actionAmount,
		};

		if ( _state == PokerHandState.Complete && _winnings.Count > 0 )
		{
			snapshot.WinnerIndices = _winnings.Keys.ToArray();
			snapshot.WinningHandDescriptions = _winnings.Keys
				.Select( i => _handResults.ContainsKey( i ) ? _handResults[i].ToString() : "Last player standing" )
				.ToArray();
		}

		_snapshots.Add( snapshot );
	}
}
