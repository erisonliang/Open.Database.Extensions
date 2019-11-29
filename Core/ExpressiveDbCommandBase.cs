﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Open.Database.Extensions
{

	/// <summary>
	/// An base class for executing commands on a database using best practices and simplified expressive syntax.
	/// Includes methods for use with DbConnection and DbCommand types.
	/// </summary>
	/// <typeparam name="TConnection">The type of the connection to be used.</typeparam>
	/// <typeparam name="TCommand">The type of the commands generated by the connection.</typeparam>
	/// <typeparam name="TReader">The type of reader created by the command.</typeparam>
	/// <typeparam name="TDbType">The DB type enum to use for parameters.</typeparam>
	/// <typeparam name="TThis">The type of this class in order to facilitate proper expressive notation.</typeparam>
	public abstract class ExpressiveDbCommandBase<TConnection, TCommand, TReader, TDbType, TThis>
			: ExpressiveCommandBase<TConnection, TCommand, TReader, TDbType, TThis>, IExecuteReaderAsync<TReader>
			where TConnection : DbConnection
			where TCommand : DbCommand
			where TReader : DbDataReader
			where TDbType : struct
			where TThis : ExpressiveDbCommandBase<TConnection, TCommand, TReader, TDbType, TThis>
	{

		/// <param name="connectionPool">The pool to acquire connections from.</param>
		/// <param name="type">The command type.</param>
		/// <param name="command">The SQL command.</param>
		/// <param name="params">The list of params</param>
		public ExpressiveDbCommandBase(
			IDbConnectionPool<TConnection> connectionPool,
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
		public ExpressiveDbCommandBase(
			IDbConnectionFactory<TConnection> connFactory,
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
		public ExpressiveDbCommandBase(
			TConnection connection,
			IDbTransaction? transaction,
			CommandType type,
			string command,
			IEnumerable<Param>? @params = null)
			: base(connection, transaction, type, command, @params)
		{
		}

		/// <param name="transaction">The transaction to execute the command on.</param>
		/// <param name="type">The command type.</param>
		/// <param name="command">The SQL command.</param>
		/// <param name="params">The list of params</param>
		public ExpressiveDbCommandBase(
			IDbTransaction transaction,
			CommandType type,
			string command,
			IEnumerable<Param>? @params = null)
			: base(transaction, type, command, @params)
		{
		}

		/// <summary>
		/// By default (false), for async methods, the underlying iteration operation for a reader will be .Read() whenever possible.  If set to true, .ReadAsync() will be used.
		/// Using .ReadAsync() can introduce unexpected latency and additional CPU overhead.
		/// This should only be set to true if there is a clear reason why and should be profiled before and after.
		/// </summary>
		public bool UseAsyncRead { get; set; }

		/// <summary>
		/// Sets the UseAsyncRead value.
		/// </summary>
		public TThis EnableAsyncRead(bool value = true)
		{
			UseAsyncRead = value;
			return (TThis)this;
		}

		/// <summary>
		/// Calls ExecuteNonQueryAsync on the underlying command.
		/// </summary>
		/// <returns>The integer response from the method.</returns>
		public ValueTask<int> ExecuteNonQueryAsync()
			=> ExecuteAsync(command => new ValueTask<int>(command.ExecuteNonQueryAsync(CancellationToken)));

		/// <summary>
		/// Calls ExecuteScalarAsync on the underlying command.
		/// </summary>
		/// <returns>The value returned from the method.</returns>
		public ValueTask<object> ExecuteScalarAsync()
			=> ExecuteAsync(command => new ValueTask<object>(command.ExecuteScalarAsync(CancellationToken)));

		/// <summary>
		/// Asynchronously executes scalar on the underlying command.
		/// </summary>
		/// <typeparam name="T">The type expected.</typeparam>
		/// <param name="transform">The transform function for the result.</param>
		/// <returns>The value returned from the method.</returns>
		public async ValueTask<T> ExecuteScalarAsync<T>(Func<object, T> transform)
		{
			if (transform is null) throw new ArgumentNullException(nameof(transform));
			Contract.EndContractBlock();

			return transform(await ExecuteScalarAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Asynchronously executes scalar on the underlying command and casts to the expected type.
		/// </summary>
		/// <typeparam name="T">The type expected.</typeparam>
		/// <returns>The value returned from the method.</returns>
		public async ValueTask<T> ExecuteScalarAsync<T>()
			=> (T)await ExecuteScalarAsync().ConfigureAwait(false);

		/// <summary>
		/// Asynchronously executes scalar on the underlying command.
		/// </summary>
		/// <typeparam name="T">The type expected.</typeparam>
		/// <param name="transform">The transform function (task) for the result.</param>
		/// <returns>The value returned from the method.</returns>
		public async ValueTask<T> ExecuteScalarAsync<T>(Func<object, ValueTask<T>> transform)
		{
			if (transform is null) throw new ArgumentNullException(nameof(transform));
			Contract.EndContractBlock();

			return await transform(await ExecuteScalarAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Iterates asynchronously and will stop iterating if canceled.
		/// </summary>
		/// <param name="handler">The active IDataRecord is passed to this handler.</param>
		/// <param name="behavior">The behavior to use with the data reader.</param>
		public ValueTask IterateReaderAsync(Action<IDataRecord> handler, CommandBehavior behavior = CommandBehavior.Default)
			=> ExecuteReaderAsync(reader => reader.ForEachAsync(handler, UseAsyncRead, CancellationToken), behavior | CommandBehavior.SingleResult);

		/// <summary>
		/// Iterates asynchronously and will stop iterating if canceled.
		/// </summary>
		/// <param name="handler">The active IDataRecord is passed to this handler.</param>
		/// <param name="behavior">The behavior to use with the data reader.</param>
		public ValueTask IterateReaderAsync(Func<IDataRecord, ValueTask> handler, CommandBehavior behavior = CommandBehavior.Default)
			=> ExecuteReaderAsync(reader => reader.ForEachAsync(handler, CancellationToken), behavior | CommandBehavior.SingleResult);

		/// <summary>
		/// Iterates asynchronously until the handler returns false.  Then cancels.
		/// </summary>
		/// <param name="predicate">If true, the iteration continues.</param>
		/// <param name="behavior">The behavior to use with the data reader.</param>
		/// <returns>The task that completes when the iteration is done or the predicate evaluates false.</returns>
		public ValueTask IterateReaderWhileAsync(Func<IDataRecord, bool> predicate, CommandBehavior behavior = CommandBehavior.Default)
			=> ExecuteReaderAsync(reader => reader.IterateWhileAsync(predicate, UseAsyncRead, CancellationToken), behavior | CommandBehavior.SingleResult);

		/// <summary>
		/// Iterates asynchronously until the handler returns false.  Then cancels.
		/// </summary>
		/// <param name="predicate">If true, the iteration continues.</param>
		/// <param name="behavior">The behavior to use with the data reader.</param>
		/// <returns>The task that completes when the iteration is done or the predicate evaluates false.</returns>
		public ValueTask IterateReaderWhileAsync(Func<IDataRecord, ValueTask<bool>> predicate, CommandBehavior behavior = CommandBehavior.Default)
			=> ExecuteReaderAsync(reader => reader.IterateWhileAsync(predicate, CancellationToken), behavior | CommandBehavior.SingleResult);

		/// <summary>
		/// Asynchronously iterates a IDataReader and returns the each result until the count is met.
		/// </summary>
		/// <typeparam name="T">The return type of the transform function.</typeparam>
		/// <param name="transform">The transform function to process each IDataRecord.</param>
		/// <param name="count">The maximum number of records before complete.</param>
		/// <param name="behavior">The behavior to use with the data reader.</param>
		/// <returns>The value from the transform.</returns>
		public ValueTask<IList<T>> TakeAsync<T>(Func<IDataRecord, T> transform, int count, CommandBehavior behavior = CommandBehavior.Default)
		{
			if (transform is null) throw new ArgumentNullException(nameof(transform));
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Cannot be negative.");
			Contract.EndContractBlock();

			return count == 0
				? new ValueTask<IList<T>>(Array.Empty<T>())
				: TakeAsyncCore();

			async ValueTask<IList<T>> TakeAsyncCore()
			{
				var results = new List<T>();
				await IterateReaderWhileAsync(record =>
				{
					results.Add(transform(record));
					return results.Count < count;
				}, behavior);
				return results;
			}
		}

		/// <summary>
		/// Reads the first column from every record and returns the results as a list..
		/// DBNull values are converted to null.
		/// </summary>
		/// <returns>The list of transformed records.</returns>
		public ValueTask<IEnumerable<object?>> FirstOrdinalResultsAsync()
			=> ExecuteReaderAsync(reader => reader.FirstOrdinalResultsAsync(UseAsyncRead, CancellationToken), CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);

		/// <summary>
		/// Reads the first column from every record..
		/// DBNull values are converted to null.
		/// </summary>
		/// <returns>The enumerable of casted values.</returns>
		public ValueTask<IEnumerable<T0>> FirstOrdinalResultsAsync<T0>()
			=> ExecuteReaderAsync(reader => reader.FirstOrdinalResultsAsync<T0>(UseAsyncRead, CancellationToken), CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);

		/// <summary>
		/// Asynchronously iterates all records within the current result set using an IDataReader and returns the desired results.
		/// </summary>
		/// <param name="n">The first ordinal to include in the request to the reader for each record.</param>
		/// <param name="others">The remaining ordinals to request from the reader for each record.</param>
		/// <returns>The QueryResult that contains all the results and the column mappings.</returns>
		public ValueTask<QueryResult<Queue<object[]>>> RetrieveAsync(int n, params int[] others)
			=> RetrieveAsync(Concat(n, others));

		/// <summary>
		/// Iterates all records within the current result set using an IDataReader and returns the desired results.
		/// </summary>
		/// <param name="c">The first column name to include in the request to the reader for each record.</param>
		/// <param name="others">The remaining column names to request from the reader for each record.</param>
		/// <returns>The QueryResult that contains all the results and the column mappings.</returns>
		public ValueTask<QueryResult<Queue<object[]>>> RetrieveAsync(string c, params string[] others)
			=> RetrieveAsync(Concat(c, others));

		/// <summary>
		/// Asynchronously returns all records via a transform function.
		/// </summary>
		/// <param name="transform">The desired column names.</param>
		/// <param name="behavior">The behavior to use with the data reader.</param>
		/// <returns>A task containing the list of results.</returns>
		public async ValueTask<List<T>> ToListAsync<T>(Func<IDataRecord, T> transform, CommandBehavior behavior = CommandBehavior.Default)
		{
			var results = new List<T>();
			await IterateReaderAsync(record => results.Add(transform(record)), behavior).ConfigureAwait(false);
			return results;
		}

		/// <summary>
		/// Asynchronously returns all records and iteratively attempts to map the fields to type T.
		/// </summary>
		/// <typeparam name="T">The model type to map the values to (using reflection).</typeparam>
		/// <param name="fieldMappingOverrides">An override map of field names to column names where the keys are the property names, and values are the column names.</param>
		/// <returns>A task containing the list of results.</returns>
		public ValueTask<IEnumerable<T>> ResultsAsync<T>(IEnumerable<KeyValuePair<string, string>>? fieldMappingOverrides) where T : new()
			=> ResultsAsync<T>(fieldMappingOverrides?.Select(kvp => (kvp.Key, kvp.Value)));

		/// <summary>
		/// Asynchronously returns all records and iteratively attempts to map the fields to type T.
		/// </summary>
		/// <typeparam name="T">The model type to map the values to (using reflection).</typeparam>
		/// <param name="fieldMappingOverrides">An override map of field names to column names where the keys are the property names, and values are the column names.</param>
		/// <returns>A task containing the list of results.</returns>
		public ValueTask<IEnumerable<T>> ResultsAsync<T>(params (string Field, string Column)[] fieldMappingOverrides) where T : new()
			=> ResultsAsync<T>(fieldMappingOverrides as IEnumerable<(string Field, string Column)>);

		/// <summary>
		/// Asynchronously iterates all records within the first result set using an IDataReader and returns the results.
		/// </summary>
		/// <returns>The QueryResult that contains all the results and the column mappings.</returns>
		public ValueTask<QueryResult<Queue<object[]>>> RetrieveAsync()
			=> ExecuteReaderAsync(reader => reader.RetrieveAsync(useReadAsync: UseAsyncRead), CommandBehavior.SingleResult);

		/// <summary>
		/// Asynchronously iterates all records within the current result set using an IDataReader and returns the desired results.
		/// </summary>
		/// <param name="ordinals">The ordinals to request from the reader for each record.</param>
		/// <returns>The QueryResult that contains all the results and the column mappings.</returns>
		public ValueTask<QueryResult<Queue<object[]>>> RetrieveAsync(IEnumerable<int> ordinals)
			=> ExecuteReaderAsync(reader => reader.RetrieveAsync(ordinals, useReadAsync: UseAsyncRead), CommandBehavior.SingleResult);

		/// <summary>
		/// Iterates all records within the first result set using an IDataReader and returns the desired results as a list of Dictionaries containing only the specified column values.
		/// </summary>
		/// <param name="columnNames">The column names to select.</param>
		/// <param name="normalizeColumnOrder">Orders the results arrays by ordinal.</param>
		/// <returns>The QueryResult that contains all the results and the column mappings.</returns>
		public ValueTask<QueryResult<Queue<object[]>>> RetrieveAsync(IEnumerable<string> columnNames, bool normalizeColumnOrder = false)
			=> ExecuteReaderAsync(reader => reader.RetrieveAsync(columnNames, normalizeColumnOrder, useReadAsync: UseAsyncRead));


		/// <summary>
		/// Asynchronously returns all records and iteratively attempts to map the fields to type T.
		/// </summary>
		/// <typeparam name="T">The model type to map the values to (using reflection).</typeparam>
		/// <param name="fieldMappingOverrides">An override map of field names to column names where the keys are the property names, and values are the column names.</param>
		/// <returns>A task containing the list of results.</returns>
		public ValueTask<IEnumerable<T>> ResultsAsync<T>(IEnumerable<(string Field, string Column)>? fieldMappingOverrides)
			where T : new()
			=> ExecuteReaderAsync(reader => reader.ResultsAsync<T>(fieldMappingOverrides, useReadAsync: UseAsyncRead));
	}
}
