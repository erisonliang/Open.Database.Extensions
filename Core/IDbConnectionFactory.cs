﻿using System;
using System.Data;

namespace Open.Database.Extensions
{
	/// <summary>
	/// Simplified interface with IDbConnection as the generic type.
	/// </summary>
	public interface IDbConnectionFactory
	{
		/// <summary>
		/// Generates a new connection of declared generic type.
		/// </summary>
		/// <returns>An IDbConnection.</returns>
		IDbConnection Create();
	}

	/// <summary>
	/// Base interface for creating connections.
	/// Useful for dependency injection.
	/// </summary>
	/// <typeparam name="TConnection">The actual connection type.</typeparam>
	public interface IDbConnectionFactory<out TConnection> : IDbConnectionFactory
		where TConnection : IDbConnection
	{
		/// <summary>
		/// Generates a new connection of declared generic type.
		/// </summary>
		/// <returns>An connection of type <typeparamref name="TConnection"/>.</returns>
		new TConnection Create();
	}

	/// <summary>
	/// Extensions for converting a connection factory into a pool.
	/// </summary>
	public static class DbConnectionFactoryExtensions
	{
		class PoolFromFactory : IDbConnectionPool
		{
			private readonly IDbConnectionFactory _connectionFactory;

			public PoolFromFactory(IDbConnectionFactory connectionFactory)
			{
				_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
			}

			public IDbConnection Take()
				=> _connectionFactory.Create();

			public void Give(IDbConnection connection)
			{
				connection.Dispose();
			}
		}

		class PoolFromFactory<TConnection> :  IDbConnectionPool<TConnection>
			where TConnection : IDbConnection
		{
			private readonly IDbConnectionFactory<TConnection> _connectionFactory;

			public PoolFromFactory(IDbConnectionFactory<TConnection> connectionFactory)
			{
				_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
			}

			public TConnection Take()
				=> _connectionFactory.Create();

			IDbConnection IDbConnectionPool.Take() => Take();

			public void Give(IDbConnection connection)
			{
				connection.Dispose();
			}
		}


		/// <summary>
		/// Provides a connection pool that simply creates from a connection factory and disposes when returned.
		/// </summary>
		/// <param name="connectionFactory">The connection factory to generate connections from.</param>
		/// <returns></returns>
		public static IDbConnectionPool AsPool(this IDbConnectionFactory connectionFactory)
			=> new PoolFromFactory(connectionFactory);

		/// <summary>
		/// Provides a connection pool that simply creates from a connection factory and disposes when returned.
		/// </summary>
		/// <param name="connectionFactory">The connection factory to generate connections from.</param>
		/// <returns></returns>
		public static IDbConnectionPool<TConnection> AsPool<TConnection>(this IDbConnectionFactory<TConnection> connectionFactory)
			where TConnection : IDbConnection
			=> new PoolFromFactory<TConnection>(connectionFactory);
	}
}
