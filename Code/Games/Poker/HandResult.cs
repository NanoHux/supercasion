namespace TerrysCasino.Games.Poker;

public readonly struct HandResult : IComparable<HandResult>
{
	public HandRank HandRank { get; }

	/// <summary>
	/// Kicker values used for comparison, ordered from most significant to least.
	/// For example, a pair of Kings with A-9-5 kickers: [13, 14, 9, 5]
	/// </summary>
	public IReadOnlyList<int> Kickers { get; }

	/// <summary>
	/// The best 5 cards that form this hand.
	/// </summary>
	public IReadOnlyList<Card> BestCards { get; }

	public HandResult( HandRank handRank, IReadOnlyList<int> kickers, IReadOnlyList<Card> bestCards )
	{
		HandRank = handRank;
		Kickers = kickers;
		BestCards = bestCards;
	}

	public int CompareTo( HandResult other )
	{
		var rankCmp = HandRank.CompareTo( other.HandRank );
		if ( rankCmp != 0 )
			return rankCmp;

		var minLen = Math.Min( Kickers.Count, other.Kickers.Count );
		for ( int i = 0; i < minLen; i++ )
		{
			var cmp = Kickers[i].CompareTo( other.Kickers[i] );
			if ( cmp != 0 )
				return cmp;
		}

		return 0;
	}

	public static bool operator >( HandResult a, HandResult b ) => a.CompareTo( b ) > 0;
	public static bool operator <( HandResult a, HandResult b ) => a.CompareTo( b ) < 0;
	public static bool operator >=( HandResult a, HandResult b ) => a.CompareTo( b ) >= 0;
	public static bool operator <=( HandResult a, HandResult b ) => a.CompareTo( b ) <= 0;
	public static bool operator ==( HandResult a, HandResult b ) => a.CompareTo( b ) == 0;
	public static bool operator !=( HandResult a, HandResult b ) => a.CompareTo( b ) != 0;

	public override bool Equals( object obj ) => obj is HandResult hr && CompareTo( hr ) == 0;
	public override int GetHashCode() => HashCode.Combine( HandRank, Kickers.Count > 0 ? Kickers[0] : 0 );

	public override string ToString()
	{
		var cards = string.Join( " ", BestCards );
		return $"{HandRank} [{cards}]";
	}
}
