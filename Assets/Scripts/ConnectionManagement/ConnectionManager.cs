﻿using System;
using System.Collections.Generic;
using Kolman_Freecss.QuestSystem;
using Model;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Kolman_Freecss.Krodun.ConnectionManagement
{
    public class ConnectionManager : NetworkBehaviour
    {
        public static ConnectionManager Instance { get; internal set; }
        private MainMenuManager _mainMenuManager;
        
        private string _gameSceneName = "Kolman";
        private string _lobbySceneName = "MultiplayerLobby";

        private void Awake()
        {
            ManageSingleton();
            if (_mainMenuManager == null)
            {
                _mainMenuManager = FindObjectOfType<MainMenuManager>();
            }
        }
        
        private void ManageSingleton()
        {
            if (Instance != null)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void StartHost(string playerName, string ipAddress, int port)
        {
            try
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, (ushort) port);
                if (NetworkManager.Singleton.StartHost())
                {
                    // SceneTransitionHandler.sceneTransitionHandler.RegisterGameCallbacks();
                    SceneTransitionHandler.sceneTransitionHandler.SwitchScene(_lobbySceneName);
                    Debug.Log("Host started");
                }
                else
                {
                    Debug.Log("Host failed to start");
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        private void LoadGame()
        {
            NetworkManager.Singleton.SceneManager.OnSynchronize += SceneManager_LoadGame; 
        }

        private void SceneManager_LoadGame(ulong clientId)
        {
            _mainMenuManager.StartGame();
        }
        
        public void StartClient(string playerName, string ipAddress, int port)
        {
            try
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, (ushort) port);
                if (NetworkManager.Singleton.StartClient())
                {
                    Debug.Log("Client started");
                }
                else
                {
                    Debug.Log("Client failed to start");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /*public void RemovePlayer(ulong clientId)
        {
            // Remove player from list
            PlayersInGame.Remove(new Player
            {
                Id = clientId
            });
        }
        
        public Player GetPlayer(ulong clientId)
        {
            int index = ConnectionManager.Instance.PlayersInGame.IndexOf(new Player
            {
                Id = clientId
            });
            if (index != -1)
            {
                Player player = ConnectionManager.Instance.PlayersInGame[index];
                return player;
            }
            return new Player()
            {
                Id = -1f
            };
        }*/
    }
}