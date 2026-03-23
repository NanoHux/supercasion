namespace TerrysCasino.Games.Poker;

public enum Suit
{
	Hearts,
	Diamonds,
	Clubs,
	Spades
}

public enum Rank
{
	Two = 2,
	Three,
	Four,
	Five,
	Six,
	Seven,
	Eight,
	Nine,
	Ten,
	Jack,
	Queen,
	King,
	Ace
}

public readonly struct Card : IEquatable<Card>
{
	public Rank Rank { get; }
	public Suit Suit { get; }

	public Card( Rank rank, Suit suit )
	{
		Rank = rank;
		Suit = suit;
	}

	public bool Equals( Card other ) => Rank == other.Rank && Suit == other.Suit;
	public override bool Equals( object obj ) => obj is Card c && Equals( c );
	public override int GetHashCode() => HashCode.Combine( Rank, Suit );
	public static bool operator ==( Card a, Card b ) => a.Equals( b );
	public static bool operator !=( Card a, Card b ) => !a.Equals( b );

	public override string ToString()
	{
		var rankStr = Rank switch
		{
			Rank.Two => "2",
			Rank.Three => "3",
			Rank.Four => "4",
			Rank.Five => "5",
			Rank.Six => "6",
			Rank.Seven => "7",
			Rank.Eight => "8",
			Rank.Nine => "9",
			Rank.Ten => "T",
			Rank.Jack => "J",
			Rank.Queen => "Q",
			Rank.King => "K",
			Rank.Ace => "A",
			_ => "?"
		};
		var suitStr = Suit switch
		{
			Suit.Hearts => "h",
			Suit.Diamonds => "d",
			Suit.Clubs => "c",
			Suit.Spades => "s",
			_ => "?"
		};
		return rankStr + suitStr;
	}

	public static Card Parse( string s )
	{
		if ( s.Length != 2 )
			throw new ArgumentException( $"Invalid card string: {s}" );

		var rank = s[0] switch
		{
			'2' => Rank.Two,
			'3' => Rank.Three,
			'4' => Rank.Four,
			'5' => Rank.Five,
			'6' => Rank.Six,
			'7' => Rank.Seven,
			'8' => Rank.Eight,
			'9' => Rank.Nine,
			'T' => Rank.Ten,
			'J' => Rank.Jack,
			'Q' => Rank.Queen,
			'K' => Rank.King,
			'A' => Rank.Ace,
			_ => throw new ArgumentException( $"Invalid rank: {s[0]}" )
		};
		var suit = s[1] switch
		{
			'h' => Suit.Hearts,
			'd' => Suit.Diamonds,
			'c' => Suit.Clubs,
			's' => Suit.Spades,
			_ => throw new ArgumentException( $"Invalid suit: {s[1]}" )
		};
		return new Card( rank, suit );
	}
}
