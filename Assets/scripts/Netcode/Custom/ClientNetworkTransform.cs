using Unity.Netcode.Components;
using UnityEngine;

namespace Player {
    public enum AuthorityMode {
        Server,
        Client
    }
    
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform {
        public AuthorityMode authorityMode = Player.AuthorityMode.Client;

        protected override bool OnIsServerAuthoritative() => authorityMode == Player.AuthorityMode.Server;
    }
}