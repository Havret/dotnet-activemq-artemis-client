//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.Testing.Listener
{
    class LinkCollection
    {
        readonly string containerId;
        readonly ConcurrentDictionary<Key, ListenerLink> links;

        public LinkCollection(string containerId)
        {
            this.containerId = containerId;
            this.links = new ConcurrentDictionary<Key, ListenerLink>();
        }

        public bool TryAdd(ListenerLink link)
        {
            Key key = new Key(this.containerId, link);
            return this.links.TryAdd(key, link);
        }

        public bool Remove(ListenerLink link)
        {
            Key key = new Key(this.containerId, link);
            ListenerLink temp;
            return this.links.TryRemove(key, out temp);
        }

        public void Clear()
        {
            this.links.Clear();
        }

        class Key : IEquatable<Key>
        {
            string fromContainer;
            string toContainer;
            string name;
            uint linkHandle;
            ushort sessionChannel;

            public Key(string containerId, ListenerLink link)
            {
                this.name = link.Name;
                this.linkHandle = link.Handle;
                this.sessionChannel = ((ListenerSession) link.Session).GetChannel();
                var listenerConnection = (ListenerConnection) link.Session.Connection;
                string remoteId = listenerConnection.GetRemoteContainerId();
                if (link.Role)
                {
                    this.fromContainer = remoteId;
                    this.toContainer = containerId;
                }
                else
                {
                    this.fromContainer = containerId;
                    this.toContainer = remoteId;
                }
            }

            public bool Equals(Key other)
            {
                return string.Equals(this.fromContainer, other.fromContainer, StringComparison.Ordinal) &&
                    string.Equals(this.toContainer, other.toContainer, StringComparison.Ordinal) &&
                    string.Equals(this.name, other.name, StringComparison.Ordinal) &&
                    this.linkHandle == other.linkHandle &&
                    this.sessionChannel == other.sessionChannel;
            }

            public override int GetHashCode()
            {
                int hash = this.fromContainer.GetHashCode();
                hash = hash * 31 + this.toContainer.GetHashCode();
                hash = hash * 31 + this.name.GetHashCode();
                hash = hash * 31 + this.linkHandle.GetHashCode();
                hash = hash * 31 + this.sessionChannel.GetHashCode();
                return hash;
            }

            public override bool Equals(object obj)
            {
                Key key = obj as Key;
                return key == null ? false : this.Equals(key);
            }
        }
    }
}
