namespace TerrysCasino.Games.Poker;

public enum ActionType
{
	Fold,
	Check,
	Call,
	Raise,
	AllIn
}

public readonly struct PlayerAction
{
	public ActionType Type { get; }
	public int Amount { get; }

	public PlayerAction( ActionType type, int amount = 0 )
	{
		Type = type;
		Amount = amount;
	}

	public override string ToString() => Amount > 0 ? $"{Type} {Amount}" : Type.ToString();
}
