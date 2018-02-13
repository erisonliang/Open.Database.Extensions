# Open.Database.Extensions
 
Useful set of utilities and abstractions for simplifying modern database operations and ensuring dependency injection compatibility.

## Connection Factories

Connection factories facilitate creation and disposal of connections without the concern of a connection reference or need for awareness of a connection string.  A ```SqlConnectionFactory``` is provided and can be overriden to provide more specific dependency injection configurations.

## Expressive Commands

The provided expressive command classes allow for an expressive means to append parameters and execute the results without lenghty complicated setup.

Extensions are provied to create commands from connection factories.

### Example

```cs
var result = connectionFactory
   .StoredProcedure("[procedure name]")
   .AddParam("a",1)
   .AddParam("b",true)
   .AddParam("c","hello")
   .ExecuteScalar();
```


## Asynchronous

End-to-end asynchronous methods suffixed with `Async`.

When using the SQL Client, asychronous methods are available as well as `.ToTargetBlockAsync<T>(target)` and `.AsSourceBlockAsync<T>()` Dataflow methods.

## Extensions

Instead of writing this:
```cs
var myResult = new List<T>();
using(var reader = await mySqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection))
{
   while(await reader.ReadAsync())
     list.Add(transform(reader));
}
```

Is now simplifed to this:
```cs
var myResult = await cmd.ToListAsync(transform);
```

## Deferred Transformation

In order to keep connection open time to a minimum, some methods cache data before closing the connection and then subsequently applying the transformations as needed.

#### `Results<T>()` and `ResultsAsync<T>()`

Synchronously queries (pulls all the data).  Then using the provided type `T` entity, the data is coerced by which properties intersect with the ones available to the ```IDataReader```.

Optionally a 

#### `Retrieve()` and `RetrieveAsync()`

Synchronously queries (pulls all the data).  Returns a `QueryResult<Queue<object[]>>` containing the requested data and column mappings.  The `.AsDequeueingMappedEnumerable()` extension will iteratively convert the results to dictionaries for ease of access.

#### `AsSourceBlockAsync<T>()`

(Fully asynchronous.) Retuns a Dataflow source block.  Then asynchronously buffers and transforms the results allowing for any possible Dataflow configuration.  The source block is marked as complete when there are no more results.  If the block is somehow marked as complete externally, the flow of data will stop and the connection will close.

### `AsSourceBlockAsync<T>()` versus `ResultsAsync<T>`

Depending on the level of asynchrony in your application, you may want to avoid too much buffering of data. 

`AsSourceBlockAsync<T>()` is fully asynchronous from end-to-end and can keep total buffering to a minimum by consuming (receiving) results as fast as possible, but may incur additional latency between reads.

`ResultsAsync<T>()` is fully asynchronous from end-to-end but returns an `IEnumerable<T>` that although has fully buffered the all the data into memory, has deferred the transformation until enumerated.  This way, the asynchronous data pipeline is fully complete before synchronously transforming the data.

Both methods ultimately are using a Queue<object[]> or ConcurrentQueue<object[]> (Dataflow) to buffer the data, but `ResultsAsync<T>()` buffers the entire data set before dequeuing and transforming the results.
