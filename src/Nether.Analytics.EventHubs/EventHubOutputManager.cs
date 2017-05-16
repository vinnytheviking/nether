﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using System.Text;

namespace Nether.Analytics
{
    public class EventHubOutputManager : IOutputManager
    {
        private string _ehConnectionString;
        private IOutputFormatter _serializer;
        private EventHubClient _client;
        private Encoding _encoding;

        /// <summary>
        /// Creates a new instance, with a JSON output formatter as the default serializer.
        /// </summary>
        /// <param name="outputEventHubConnectionString">The connection string for the event hub output.</param>
        public EventHubOutputManager(string outputEventHubConnectionString)
            : this(outputEventHubConnectionString, new JsonOutputFormatter())
        {
        }

        public EventHubOutputManager(string outputEventHubConnectionString, IOutputFormatter serializer)
            : this(outputEventHubConnectionString, serializer, Encoding.UTF8)
        {
        }

        public EventHubOutputManager(string outputEventHubConnectionString, IOutputFormatter serializer, Encoding encoding)
        {
            _encoding = encoding;
            _serializer = serializer;
            _ehConnectionString = outputEventHubConnectionString;

            // spin up the event hub client
            EnsureEventHubClient();
        }

        private void EnsureEventHubClient()
        {
            if (_client == null)
            {
                // TODO: is this useful?
                if (string.IsNullOrEmpty(_ehConnectionString))
                {
                    throw new ArgumentException("Missing Event Hub connection string.");
                }

                _client = EventHubClient.CreateFromConnectionString(_ehConnectionString);
            }
        }
        private EventHubClient Client
        {
            get
            {
                EnsureEventHubClient();
                return _client;
            }
        }

        public Task FlushAsync()
        {
            // TODO: does it make sense to not do anything? The client doesn't support flushing.
            return Task.FromResult(0);
        }

        public Task OutputMessageAsync(string pipelineName, int idx, Message msg)
        {
            string payload;

            if (_serializer is IHeaderAwareOutputFormatter)
            {
                payload = ((IHeaderAwareOutputFormatter)_serializer).FormatWithHeaders(msg);
            }
            else
            {
                payload = _serializer.Format(msg);
            }


            var eventData = new EventData(_encoding.GetBytes(payload));

            return Client.SendAsync(eventData);
        }
    }
}