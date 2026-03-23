using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TerrysCasino.Games.Poker;

namespace TerrysCasino.Core;

/// <summary>
/// Stores and retrieves hand replay data (HandSnapshots).
/// Pure logic — no s&box dependencies.
/// </summary>
public class HandReplay
{
	private readonly string _baseDirectory;

	public HandReplay( string baseDirectory )
	{
		_baseDirectory = baseDirectory;
	}

	/// <summary>
	/// Save a complete hand's snapshots for replay.
	/// </summary>
	public bool SaveHand( int handId, int tableId, IReadOnlyList<HandSnapshot> snapshots )
	{
		try
		{
			EnsureDirectory();
			string filePath = GetHandPath( handId, tableId );
			var data = new HandReplayData
			{
				HandId = handId,
				TableId = tableId,
				Timestamp = DateTime.UtcNow.ToString( "o" ),
				Snapshots = snapshots.ToList()
			};
			string json = JsonSerializer.Serialize( data, new JsonSerializerOptions { WriteIndented = false } );
			File.WriteAllText( filePath, json );
			return true;
		}
		catch ( Exception ex )
		{
			Console.Error.WriteLine( $"[HandReplay] Save failed for hand {handId}: {ex.Message}" );
			return false;
		}
	}

	/// <summary>
	/// Load a hand's replay data.
	/// </summary>
	public HandReplayData LoadHand( int handId, int tableId )
	{
		try
		{
			string filePath = GetHandPath( handId, tableId );
			if ( !File.Exists( filePath ) )
				return null;

			string json = File.ReadAllText( filePath );
			return JsonSerializer.Deserialize<HandReplayData>( json );
		}
		catch ( Exception ex )
		{
			Console.Error.WriteLine( $"[HandReplay] Load failed for hand {handId}: {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// List all saved hand IDs for a table, most recent first.
	/// </summary>
	public List<int> ListHands( int tableId )
	{
		try
		{
			if ( !Directory.Exists( _baseDirectory ) )
				return new List<int>();

			return Directory.GetFiles( _baseDirectory, $"table{tableId}_hand*.json" )
				.Select( f =>
				{
					string name = Path.GetFileNameWithoutExtension( f );
					int idx = name.IndexOf( "_hand", StringComparison.Ordinal );
					if ( idx >= 0 && int.TryParse( name.Substring( idx + 5 ), out int id ) )
						return id;
					return -1;
				} )
				.Where( id => id >= 0 )
				.OrderByDescending( id => id )
				.ToList();
		}
		catch
		{
			return new List<int>();
		}
	}

	private string GetHandPath( int handId, int tableId )
	{
		return Path.Combine( _baseDirectory, $"table{tableId}_hand{handId}.json" );
	}

	private void EnsureDirectory()
	{
		if ( !Directory.Exists( _baseDirectory ) )
			Directory.CreateDirectory( _baseDirectory );
	}
}

public class HandReplayData
{
	public int HandId { get; set; }
	public int TableId { get; set; }
	public string Timestamp { get; set; }
	public List<HandSnapshot> Snapshots { get; set; }
}
