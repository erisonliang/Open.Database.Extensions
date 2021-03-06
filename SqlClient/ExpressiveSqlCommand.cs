﻿using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace Open.Database.Extensions
{

	/// <summary>
	/// A specialized for SqlClient abstraction for executing commands on a database using best practices and simplified expressive syntax.
	/// </summary>
	public class ExpressiveSqlCommand : ExpressiveDbCommandBase<SqlConnection, SqlCommand, SqlDataReader, SqlDbType, ExpressiveSqlCommand>
	{

		/// <param name="connectionPool">The pool to acquire connections from.</param>
		/// <param name="type">The command type.</param>
		/// <param name="command">The SQL command.</param>
		/// <param name="params">The list of params</param>
		public ExpressiveSqlCommand(
			IDbConnectionPool<SqlConnection> connectionPool,
			CommandType type,
			string command,
			IEnumerable<Param>? @params = null)
			: base(connectionPool, type, command, @params)
		{
		}

		/// <param name="connFactory">The factory to generate connections from.</param>
		/// <param name="type">The command type.</param>
		/// <param name="command">The SQL command.</param>
		/// <param name="params">The list of params</param>
		public ExpressiveSqlCommand(
			IDbConnectionFactory<SqlConnection> connFactory,
			CommandType type,
			string command,
			IEnumerable<Param>? @params = null)
			: base(connFactory, type, command, @params)
		{
		}

		/// <param name="connection">The connection to execute the command on.</param>
		/// <param name="transaction">The optional transaction to execute the command on.</param>
		/// <param name="type">The command type.</param>
		/// <param name="command">The SQL command.</param>
		/// <param name="params">The list of params</param>
		public ExpressiveSqlCommand(
			SqlConnection connection,
			IDbTransaction? transaction,
			CommandType type,
			string command,
			IEnumerable<Param>? @params = null)
			: base(connection, transaction, type, command, @params)
		{
		}

		/// <param name="connection">The connection to execute the command on.</param>
		/// <param name="type">The command type.</param>
		/// <param name="command">The SQL command.</param>
		/// <param name="params">The list of params</param>
		public ExpressiveSqlCommand(
			SqlConnection connection,
			CommandType type,
			string command,
			IEnumerable<Param>? @params = null)
			: base(connection, type, command, @params)
		{
		}

		/// <param name="transaction">The transaction to execute the command on.</param>
		/// <param name="type">The command type.</param>
		/// <param name="command">The SQL command.</param>
		/// <param name="params">The list of params</param>
		public ExpressiveSqlCommand(
			IDbTransaction transaction,
			CommandType type,
			string command,
			IEnumerable<Param>? @params = null)
			: base(transaction, type, command, @params)
		{
		}


		/// <summary>
		/// Handles adding the list of parameters to a new command.
		/// </summary>
		/// <param name="command"></param>
		protected override void AddParams(SqlCommand command)
		{
			if (command is null) throw new System.ArgumentNullException(nameof(command));
			Contract.EndContractBlock();

			foreach (var p in Params)
			{
				var np = command
					.Parameters
					.AddWithValue(p.Name, p.Value);

				if (p.Type.HasValue)
					np.SqlDbType = p.Type.Value;
			}
		}

	}

}
