using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// Represents a disposable object that can be disposed of to release resources.
	/// </summary>
	public class Disposable : IDisposable
	{
		private class DummyDisposable : IDisposable { public void Dispose() { } }

		/// <summary>
		/// Provides an empty disposable object that does nothing when Dispose is called.
		/// </summary>
		public static IDisposable Empty { get; } = new DummyDisposable();



		private bool _isDisposed;

		/// <summary>
		/// Gets a value indicating whether this instance has been disposed.
		/// </summary>
		[IgnoreDataMember]
		[JsonIgnore]
		[BsonIgnore]
		public bool Disposed => _isDisposed;

		/// <summary>
		/// Occurs when this instance is disposed.
		/// </summary>
		public event EventHandler? OnDispose;

		/// <summary>
		/// Initializes a new instance of the <see cref="Disposable"/> class.
		/// </summary>
		public Disposable()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Disposable"/> class with an action to execute when disposed.
		/// </summary>
		/// <param name="onDispose">The action to execute when disposed.</param>
		public Disposable(Action onDispose) : this()
		{
			OnDispose += (s, e) => onDispose();
		}

		/// <summary>
		/// Releases all resources used by the object.
		/// </summary>
		public void Dispose()
		{
			DisposePrivate(true);
			GC.SuppressFinalize(this);
		}

		private void DisposePrivate(bool disposing)
		{
			if (!_isDisposed)
			{
				_isDisposed = true;
				OnDispose?.Invoke(this, EventArgs.Empty);
				OnDispose = null;
				Dispose(disposing);
			}
		}

		/// <summary>
		/// Releases the unmanaged resources used by the object and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
		}

		/// <summary>
		/// Throws an exception if the object has been disposed.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
		public void ThrowIfDisposed()
		{
			ObjectDisposedException.ThrowIf(_isDisposed, this);
		}

		~Disposable()
		{
			DisposePrivate(false);
		}
	}
}