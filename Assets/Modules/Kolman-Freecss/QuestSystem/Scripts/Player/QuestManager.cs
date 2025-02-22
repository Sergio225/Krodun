﻿using System.Collections.Generic;
using System.Linq;
using Kolman_Freecss.Krodun;
using Kolman_Freecss.Krodun.ConnectionManagement;
using Unity.Netcode;
using UnityEngine;

namespace Kolman_Freecss.QuestSystem
{
    public class QuestManager : NetworkBehaviour
    {
        #region ######## Variables ########

        public List<StorySO> storiesSO = new List<StorySO>();

        [HideInInspector] Story currentStory = new Story();
        public static QuestManager Instance { get; private set; }
        
        private List<Story> stories = new List<Story>();
        private List<QuestGiver> _questGivers = new List<QuestGiver>();
        private List<SceneItemHandler> _sceneItemHandlers = new List<SceneItemHandler>();
        
        #endregion

        #region ######## Events ########

        [HideInInspector]
        public delegate void OnStoryComletedHandler(Story story);
        [HideInInspector]
        public event OnStoryComletedHandler OnStoryComletedEvent;
        
        [HideInInspector]
        public delegate void OnQuestObjectiveHandler(EventQuestType eventQuestType, AmountType amountType, int questId);
        [HideInInspector]
        public event OnQuestObjectiveHandler OnQuestObjectiveEvent;
        
        [HideInInspector]
        public delegate void OnLastQuestHandler();
        [HideInInspector]
        public event OnLastQuestHandler OnLastQuestEvent;

        #endregion

        #region ######## Network Variables ########

        [HideInInspector] public NetworkVariable<QuestState> QuestStateSync = new NetworkVariable<QuestState>(QuestState.DefaultValue(), NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);

        #endregion
        
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                //Server will be notified when a client connects
                SubscribeToDelegatesAndUpdateValues();
            }
            
            SubscribeToDelegatesAndUpdateValuesClient();
        }

        private void SubscribeToDelegatesAndUpdateValuesClient()
        {
            QuestStateSync.OnValueChanged += UpdateQuestState;
            OnLastQuestEvent += UpdateLastQuest;
        }

        private void SubscribeToDelegatesAndUpdateValues()
        {
            SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedGameScene += ClientLoadedGameScene;
            OnQuestObjectiveEvent += OnQuestObjectiveHandleServerRpc;
        }
        
        public void UpdateQuestState(QuestState previousState, QuestState newState)
        {
            if (newState.isFinished)
            {
                CurrentStory.CompleteQuest();
                return;
            }
            CurrentStory.CurrentQuest.objectives[0].isCompleted = newState.IsCompleted;
            CurrentStory.CurrentQuest.objectives[0].CurrentAmount = newState.CurrrentAmount;
            CurrentStory.CurrentQuest.Status = newState.Status;
        }
        
        private void ClientLoadedGameScene(ulong clientId)
        {
            if (!IsServer) return;
            
            //Server will notified to a single client when his scene is loaded
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] {clientId}
                }
            };
            OnClientConnectedQuestInitClientRpc(clientId, clientRpcParams);
        }

        
        /*private void Update()
        {
            // ONLY!! Use it like hack to test the quest system            
            if (Input.GetKeyDown(KeyCode.L))
            {
                if (CurrentStory.UpdateQuestObjectiveAmount(CurrentStory.CurrentQuest.objectives[0].EventQuestType,
                        CurrentStory.CurrentQuest.objectives[0].AmountType))
                {
                    Debug.Log("Complete quest -> " + CurrentStory.CurrentQuest.storyStep);
                    // First we update the quest status in quest giver
                    UpdateStatusGiverByQuestId(CurrentStory.CurrentQuest);
                    // Then we complete the quest
                    CompleteStatusGiverByQuestId(CurrentStory.CurrentQuest.ID);
                }

                Debug.Log("Update objective -> " + CurrentStory.CurrentQuest.objectives[0].CurrentAmount + " / " +
                          CurrentStory.CurrentQuest.objectives[0].RequiredAmount);
                Debug.Log("isCompleted -> " + CurrentStory.CurrentQuest.objectives[0].isCompleted);
                if (CurrentStory.CurrentQuest.objectives[0].isCompleted)
                {
                    Debug.Log("Complete quest -> " + CurrentStory.CurrentQuest.storyStep);
                    Debug.Log("Name quest -> " + CurrentStory.CurrentQuest.title);
                    CompleteQuest();
                }
            }
        }*/

        /*
         * This method is used to update the quest objetive
         * @param eventQuestType : The type of the event
         * @param eventEnum : The enum of the event
         */
        public void EventTriggered(EventQuestType eventQuestType, AmountType amountType)
        {
            OnQuestObjectiveHandleServerRpc(eventQuestType, amountType);
        }
        
        private void SyncQuestStatus(Quest quest)
        {
            var state = new QuestState {IsCompleted = quest.objectives[0].isCompleted, 
                CurrrentAmount = quest.objectives[0].CurrentAmount, 
                Status = quest.Status};
            UpdateQuestServerRpc(state);
        }
        
        /**
         * Called when a client finish the quest
         */
        private void SyncQuestStatusFinished()
        {
            var state = new QuestState {isFinished = true};
            UpdateQuestServerRpc(state);
        }
        
        /**
         * Finish the current quest and start the next one
         */
        public void CompleteQuest()
        {
            CompleteQuestServerRpc();
        }
        
        private void HandleDoorState(DoorState doorState, DoorType doorType)
        {
            foreach (var sceneItemHandler in SceneItemHandlers)
            {
                // Filter the door type from the list
                if (doorType == sceneItemHandler.DoorType)
                {
                    // Filter doorType to open or close
                    if (doorType == DoorType.Entrance)
                    {
                        if (doorState == DoorState.Open)
                        {
                            sceneItemHandler.OpenEntranceDoors();
                        }
                        else
                        {
                            sceneItemHandler.CloseEntranceDoors();
                        }
                    }
                    else
                    {
                        if (doorState == DoorState.Open)
                        {
                            sceneItemHandler.OpenExitDoors();
                        }
                        else
                        {
                            sceneItemHandler.CloseExitDoors();
                        }
                    }
                }
            }
        }
        
        private void CloseEntranceDoors()
        {
            HandleDoorState(DoorState.Closed, DoorType.Entrance);         
        }
        
        private void OpenEntranceDoors()
        {
            HandleDoorState(DoorState.Open, DoorType.Entrance);
        }
        
        private void CloseExitDoors()
        {
            HandleDoorState(DoorState.Closed, DoorType.Exit);
        }
        
        private void OpenExitDoors()
        {
            HandleDoorState(DoorState.Open, DoorType.Exit);
        }
        
        #region ######## Client RPCs ########

        [ClientRpc]
        public void OnClientConnectedQuestInitClientRpc(ulong clientId, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log("----------------- Quests Init -----------------");
            
            QuestGivers = FindObjectsOfType<QuestGiver>().ToList();
            SceneItemHandlers = FindObjectsOfType<SceneItemHandler>().ToList();
            CloseEntranceDoors();
            storiesSO.ForEach(storySO => { Stories.Add(new Story(storySO)); });
            //TODO : Add a way to choose the story
            CurrentStory = Stories[0];
            CurrentStory.StartStory();
            // TODO : Change it to go like an event this refresh
            if (IsHost)
                RefreshQuestGivers();
        }

        #endregion
        
        #region ######## ServerCalls ########

        [ServerRpc(RequireOwnership = false)]
        public void OnQuestObjectiveHandleServerRpc(EventQuestType eventQuestType, AmountType amountType, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            OnQuestObjectiveEvent?.Invoke(eventQuestType, amountType, CurrentStory.CurrentQuest.ID);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void CompleteQuestServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log($"Quest completed by client -> {clientId}");
            
            FinishStatusGiverByQuestId(CurrentStory.CurrentQuest.ID);
            Quest q = CurrentStory.CompleteQuest();
            if (q != null)
            {
                RefreshQuestGivers();
            }
            SyncQuestStatusFinished();
            // No more quests in the story so we finish it
            if (q == null)
            {
                CurrentStory.CompleteStory();
                OnStoryComletedEvent?.Invoke(CurrentStory);
                Debug.Log("Story completed");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateQuestServerRpc(QuestState state, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            QuestStateSyncValue = state;
        }
        
        [ServerRpc]
        private void OnQuestObjectiveHandleServerRpc(EventQuestType eventQuestType, AmountType amountType, int questId)
        {
            CurrentStory.UpdateQuestObjectiveAmount(eventQuestType, amountType);
            SyncQuestStatus(CurrentStory.CurrentQuest);
        }
        
        #endregion
        
        /**
         * Update the current story and the status of the current quest giver
         */
        public void AcceptQuest()
        {
            UpdateStatusGiverByQuestId(CurrentStory.CurrentQuest);
            CurrentStory.AcceptQuest();
            IsLastQuest();
        }

        public void IsLastQuest()
        {
            if (CurrentStory.Quests.Count == CurrentStory.CurrentQuest.storyStep + 1)
            {
                Debug.Log("Last quest");
                OnLastQuestEvent?.Invoke();
            }
        }

        public void UpdateLastQuest()
        {
            UpdateLastQuestServerRpc();
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void UpdateLastQuestServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log($"Last quest updated by client -> {clientId}");
            UpdateLastQuestClientRpc();
        }
        
        [ClientRpc]
        public void UpdateLastQuestClientRpc()
        {
            Debug.Log("UpdateLastQuest");
            OpenEntranceDoors();
        }

        /**
         * Update the current story and the next quest givers
         */
        public void NextQuest()
        {
            UpdateStatusGiverByQuestId(CurrentStory.CurrentQuest);
            CurrentStory.NextQuest();
            RefreshQuestGivers();
        }

        public void FinishStatusGiverByQuestId(int questId)
        {
            GetQuestGiverByQuestId(questId).FinishQuest();
        }

        public Quest CompleteStatusGiverByQuestId(int questId)
        {
            return GetQuestGiverByQuestId(questId).CompleteQuest();
        }

        /**
         * Update the current quest of the quest givers
         */
        public Quest UpdateStatusGiverByQuestId(Quest quest)
        {
            return GetQuestGiverByQuestId(quest.ID).UpdateQuestStatus(quest);
        }

        /**
         * Refresh the quest givers that are on the current steps in the current story 
         */
        void RefreshQuestGivers()
        {
            QuestGiver qg = GetQuestGiverByQuestId(CurrentStory.CurrentQuest.ID);
            if (qg != null)
            {
                qg.RefreshQuest(CurrentStory.CurrentQuest.ID);
            }
        }

        QuestGiver GetQuestGiverByQuestId(int questId)
        {
            return QuestGivers.Find(q => q.HasQuest(questId));
        }

        #region ################## GETTERS && SETTERS ################## 

        public List<QuestGiver> QuestGivers { get => _questGivers; set => _questGivers = value; }
        
        public List<SceneItemHandler> SceneItemHandlers { get => _sceneItemHandlers; set => _sceneItemHandlers = value; }

        public List<Story> Stories { get => stories; set => stories = value; }
        
        public Story CurrentStory { get => currentStory; set => currentStory = value; }
        
        public QuestState QuestStateSyncValue { get => QuestStateSync.Value; set => QuestStateSync.Value = value; }

        #endregion

    }
}