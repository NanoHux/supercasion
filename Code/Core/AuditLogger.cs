using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace TerrysCasino.Core;

/// <summary>
/// Server-side anti-collusion audit logger.
/// Writes JSON lines per daily file. Supports grep-based queries like
/// "show all hands where player X folded to player Y."
/// Pure logic — no s&box dependencies (uses System.IO).
/// </summary>
public class AuditLogger
{
	public const int MaxBufferSize = 1000;

	private readonly string _baseDirectory;
	private readonly ConcurrentQueue<AuditEntry> _buffer = new();
	private string _currentFilePath;
	private string _currentDate;
	private bool _isHalted;

	public int BufferCount => _buffer.Count;
	public bool IsHalted => _isHalted;

	public AuditLogger( string baseDirectory )
	{
		_baseDirectory = baseDirectory;
	}

	/// <summary>
	/// Log a poker action event.
	/// </summary>
	public bool LogEvent( int handId, int tableId, string playerId, string action, int amount )
	{
		if ( _isHalted )
			return false;

		var entry = new AuditEntry
		{
			HandId = handId,
			TableId = tableId,
			Timestamp = DateTime.UtcNow.ToString( "o" ),
			PlayerId = playerId,
			Action = action,
			Amount = amount
		};

		return WriteEntry( entry );
	}

	/// <summary>
	/// Try to flush any buffered entries to disk.
	/// Call this periodically (e.g., every few seconds).
	/// </summary>
	public int FlushBuffer()
	{
		int flushed = 0;
		while ( _buffer.TryDequeue( out var entry ) )
		{
			if ( TryWriteToDisk( entry ) )
				flushed++;
			else
			{
				// Re-queue if write still fails
				_buffer.Enqueue( entry );
				break;
			}
		}

		// Un-halt if buffer is back under limit
		if ( _isHalted && _buffer.Count < MaxBufferSize )
			_isHalted = false;

		return flushed;
	}

	private bool WriteEntry( AuditEntry entry )
	{
		if ( TryWriteToDisk( entry ) )
			return true;

		// Buffer on failure
		_buffer.Enqueue( entry );

		if ( _buffer.Count >= MaxBufferSize )
		{
			_isHalted = true;
			Console.Error.WriteLine( $"[AuditLogger] Buffer full ({MaxBufferSize} events). Halting new hands until resolved." );
		}

		return false;
	}

	private bool TryWriteToDisk( AuditEntry entry )
	{
		try
		{
			EnsureDirectory();
			string filePath = GetFilePath();
			string json = JsonSerializer.Serialize( entry );
			File.AppendAllText( filePath, json + "\n" );
			return true;
		}
		catch ( Exception ex )
		{
			Console.Error.WriteLine( $"[AuditLogger] Write failed: {ex.Message}" );
			return false;
		}
	}

	private string GetFilePath()
	{
		string today = DateTime.UtcNow.ToString( "yyyy-MM-dd" );
		if ( today != _currentDate )
		{
			_currentDate = today;
			_currentFilePath = Path.Combine( _baseDirectory, $"audit-{today}.jsonl" );
		}
		return _currentFilePath;
	}

	private void EnsureDirectory()
	{
		if ( !Directory.Exists( _baseDirectory ) )
			Directory.CreateDirectory( _baseDirectory );
	}
}

/// <summary>
/// Single audit log entry. Schema matches design doc:
/// hand_id, table_id, timestamp, player_id, action, amount.
/// </summary>
public class AuditEntry
{
	public int HandId { get; set; }
	public int TableId { get; set; }
	public string Timestamp { get; set; }
	public string PlayerId { get; set; }
	public string Action { get; set; }
	public int Amount { get; set; }
}
