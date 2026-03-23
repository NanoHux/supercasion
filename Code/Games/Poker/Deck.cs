namespace TerrysCasino.Games.Poker;

/// <summary>
/// Standard 52-card deck with Fisher-Yates shuffle.
/// Pure logic — no s&box dependencies.
/// </summary>
public class Deck
{
	private readonly List<Card> _cards = new();
	private int _dealIndex;
	private readonly Random _rng;

	public int Remaining => _cards.Count - _dealIndex;

	public Deck( Random rng = null )
	{
		_rng = rng ?? new Random();
		Reset();
	}

	public void Reset()
	{
		_cards.Clear();
		_dealIndex = 0;

		foreach ( Suit suit in Enum.GetValues<Suit>() )
		foreach ( Rank rank in Enum.GetValues<Rank>() )
		{
			_cards.Add( new Card( rank, suit ) );
		}
	}

	public void Shuffle()
	{
		_dealIndex = 0;

		// Fisher-Yates shuffle
		for ( int i = _cards.Count - 1; i > 0; i-- )
		{
			int j = _rng.Next( i + 1 );
			(_cards[i], _cards[j]) = (_cards[j], _cards[i]);
		}
	}

	public Card Deal()
	{
		if ( _dealIndex >= _cards.Count )
			throw new InvalidOperationException( "No cards remaining in deck." );

		return _cards[_dealIndex++];
	}

	public List<Card> Deal( int count )
	{
		var result = new List<Card>( count );
		for ( int i = 0; i < count; i++ )
			result.Add( Deal() );
		return result;
	}
}
