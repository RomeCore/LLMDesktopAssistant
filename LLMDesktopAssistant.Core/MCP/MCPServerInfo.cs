using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace LLMDesktopAssistant.MCP
{
	/// <summary>
	/// Represents the configuration details for connecting to an MCP server, including the connection type, endpoint, and a user-friendly name for the configuration.
	/// </summary>
	public class MCPServerInfo : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this server configuration.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		private string _name = string.Empty;
		/// <summary>
		/// A user-friendly name for the MCP server configuration. This is used for display purposes in the UI to help users identify different server configurations.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private MCPConnectionType _connectionType = MCPConnectionType.Undefined;
		/// <summary>
		/// The type of connection to use when connecting to the MCP server. This determines how the client will communicate with the server (e.g., stdio, HTTP, etc.).
		/// </summary>
		public MCPConnectionType ConnectionType
		{
			get => _connectionType;
			set => SetProperty(ref _connectionType, value);
		}

		private string _endpoint = string.Empty;
		/// <summary>
		/// The endpoint to connect to the MCP server. This could be a file path for stdio transport, a URL for remote transport, etc., depending on the connection type.
		/// </summary>
		public string Endpoint
		{
			get => _endpoint;
			set => SetProperty(ref _endpoint, value);
		}

		public override bool Equals(object? obj)
		{
			return obj is MCPServerInfo other &&
				ConnectionType == other.ConnectionType &&
				Endpoint == other.Endpoint &&
				Name == other.Name;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(ConnectionType, Endpoint, Name);
		}

		public override string ToString()
		{
			return $"{Name} ({ConnectionType}: {Endpoint})";
		}
	}
}