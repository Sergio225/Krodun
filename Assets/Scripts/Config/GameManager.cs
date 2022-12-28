﻿using System;
using Kolman_Freecss.Krodun.ConnectionManagement;
using Kolman_Freecss.QuestSystem;
using Model;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Kolman_Freecss.Krodun
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [HideInInspector]
        public NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);
        
        [HideInInspector]
        public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);
        
        [HideInInspector]
        public delegate void IsSceneLoadedDelegateHandler(bool isLoaded, ulong clientId);
        
        [HideInInspector]
        public event IsSceneLoadedDelegateHandler OnSceneLoadedChanged;

        [HideInInspector] public bool isSceneLoadedValue;
        
        internal static event Action OnSingletonReady;
    
        private void Awake()
        {
            Assert.IsNull(Instance, $"Multiple instances of {nameof(Instance)} detected. This should not happen.");
            Instance = this;
            DontDestroyOnLoad(this);
            isGameOver.OnValueChanged += OnGameOver;
            
            OnSingletonReady?.Invoke();
            if (IsServer)
            {
                isGameStarted.Value = false;
                isGameOver.Value = false;
                if (!Instance)
                    OnSingletonReady += SubscribeToDelegatesAndUpdateValues;
                else
                    SubscribeToDelegatesAndUpdateValues();
            }
        }
        
        private void OnGameOver(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                Debug.Log("Game Over");
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    NetworkManager.Shutdown();
                    if (IsServer)
                    {
                        foreach (Player player in ConnectionManager.Instance.PlayersInGame)
                        {
                            NetworkManager.Singleton.DisconnectClient(player.Id);
                        }
                    }
                }
                SceneManager.LoadScene("GameOver");
            }
        }
        
        private void SubscribeToDelegatesAndUpdateValues()
        {
            Debug.Log("Subscribing to delegates QuestManager");
            QuestManager.Instance.OnStoryComletedEvent += OnStoryCompleted;
        }
        
        private void OnStoryCompleted(Story story)
        {
            if (story.IsCompleted)
            {
                isGameOver.Value = true;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            /*if (IsClient && IsServer)
            }*/
            
            if (IsServer)
            {
                //Server will be notified when a client connects
                NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedGameScene += ClientLoadedGameScene;
            }
            
        }
        
        private void ClientLoadedGameScene(ulong clientId)
        {
            if (IsServer)
            {
                //Server will notified to a single client when his scene is loaded
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] {clientId}
                    }
                };
                OnClientConnectedCallbackClientRpc(clientId, clientRpcParams);
            }
        }
        
        [ClientRpc]
        private void OnClientConnectedCallbackClientRpc(ulong clientId, ClientRpcParams clientRpcParams = default)
        {
            /*if (IsOwner) return;*/
            Debug.Log("------------------SENT Client Loaded Scene------------------");
            Debug.Log("Client Id -> " + clientId);
            OnSceneLoadedChanged?.Invoke(true, clientId);
            isSceneLoadedValue = true;
            StartGame();
        }

        public void Update()
        {
            if (IsServer)
            {
                StartGame();
            }
        }

        private void StartGame()
        {
            if (!isGameStarted.Value && SceneTransitionHandler.sceneTransitionHandler.GetCurrentSceneState() == SceneTransitionHandler.SceneStates.Kolman)
            {
                Debug.Log("------------------START GAME------------------");
                isGameStarted.Value = true;
            }
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected");
            /*if (IsServer)
            {
                // Find if the client is already in the list PlayersInGame
                if (ConnectionManager.Instance.PlayersInGame.IndexOf(new Player
                    {
                        Id = clientId
                    }) == -1)
                {
                    // Add the client to the list PlayersInGame
                    ConnectionManager.Instance.PlayersInGame.Add(new Player
                    {
                        Id = clientId
                    });
                }
            }*/
        }
    }
}