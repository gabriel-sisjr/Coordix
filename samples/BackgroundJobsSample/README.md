# BackgroundJobsSample

This sample demonstrates how to use Coordix.Background for fire-and-forget background job processing.

## Features Demonstrated

1. **Configuration**: How to set up `AddCoordixBackground()` (automatically includes core services)
2. **Background Requests**: Enqueue requests to be processed asynchronously
3. **Background Notifications**: Enqueue notifications that trigger multiple handlers
4. **Fire-and-Forget**: Jobs are processed in the background without blocking the main thread

## Running the Sample

```bash
dotnet run --project samples/BackgroundJobsSample/BackgroundJobsSample.csproj
```

## What You'll See

The sample demonstrates three scenarios:

1. **Email Request**: A simple request that sends an email in the background
2. **Payment Processing**: A request with response that processes a payment in the background
3. **Order Notification**: A notification that triggers multiple handlers (order processing and inventory update)

All jobs are enqueued immediately and processed asynchronously by the background worker.

## Key Points

- Jobs are enqueued using `IBackgroundMediator.Enqueue()`
- The background worker processes jobs automatically
- Exceptions in one job don't stop processing of other jobs
- Multiple handlers can process the same notification
- Jobs are processed outside the original request context
