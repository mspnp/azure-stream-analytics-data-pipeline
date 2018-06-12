using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.EventHubs;

namespace taxi
{
    public static class EventDataExtensions
    {
        public static IEnumerable<EventDataBatch> Partition
            (this IEnumerable<EventData> source, int batchSize = 131072, string partitionKey = null)
        {
            // We'll assume one batch
            EventDataBatch eventDataBatch = new EventDataBatch(batchSize, partitionKey);
            foreach (var eventData in source)
            {
                if (!eventDataBatch.TryAdd(eventData))
                {
                    yield return eventDataBatch;
                    // We need to create the new batch and add here otherwise, we'll lose this message
                    eventDataBatch = new EventDataBatch(batchSize, partitionKey);
                    // It will be small enough in our case, but we should probably figure out a better way later
                    eventDataBatch.TryAdd(eventData);
                }
            }

            if (eventDataBatch.Count > 0)
            {
                yield return eventDataBatch;
            }
        }
    }
}