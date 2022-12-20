﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kolman_Freecss.Krodun;
using Kolman_Freecss.Krodun.ConnectionManagement;
using Unity.Netcode;
using UnityEngine;

namespace Kolman_Freecss.QuestSystem
{
    public class QuestManager : NetworkBehaviour
    {
        #region Variables

        public List<StorySO> storiesSO = new List<StorySO>();

        [HideInInspector] Story currentStory = new Story();
        public static QuestManager Instance { get; private set; }
        
        private List<Story> stories = new List<Story>();
        private List<QuestGiver> _questGivers = new List<QuestGiver>();
        
        #endregion

        [HideInInspector]
        public delegate void OnStoryComletedHandler(Story story);
        [HideInInspector]
        public event OnStoryComletedHandler OnStoryComletedEvent;
        
        [HideInInspector]
        public delegate void OnCollectItemHandler(EventQuestType eventQuestType, AmountType amountType, int questId);
        [HideInInspector]
        public event OnCollectItemHandler OnCollectItemEvent;
        
        [HideInInspector] public NetworkVariable<QuestState> QuestStateSync = new NetworkVariable<QuestState>(QuestState.DefaultValue(), NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);
        
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
        }

        private void SubscribeToDelegatesAndUpdateValues()
        {
            SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;
            OnCollectItemEvent += ItemCollectedServerRpc;
        }
        
        public void UpdateQuestState(QuestState previousState, QuestState newState)
        {
            CurrentStory.CurrentQuest.objectives[0].isCompleted = newState.IsCompleted;
            CurrentStory.CurrentQuest.objectives[0].CurrentAmount = newState.CurrrentAmount;
            CurrentStory.CurrentQuest.Status = newState.Status;
        }
        
        private void ClientLoadedScene(ulong clientId)
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

        [ClientRpc]
        public void OnClientConnectedQuestInitClientRpc(ulong clientId, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log("----------------- Quests Init -----------------");
            
            QuestGivers = FindObjectsOfType<QuestGiver>().ToList();
            storiesSO.ForEach(storySO => { Stories.Add(new Story(storySO)); });
            //TODO : Add a way to choose the story
            CurrentStory = Stories[0];
            CurrentStory.StartStory();
            // TODO : Change it to go like an event this refresh
            RefreshQuestGivers();
        }
        
        /*[ServerRpc]*/

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
            OnCollectItemEvent?.Invoke(eventQuestType, amountType, CurrentStory.CurrentQuest.ID);
        }
        
        [ServerRpc]
        private void ItemCollectedServerRpc(EventQuestType eventQuestType, AmountType amountType, int questId)
        {
            Debug.Log("ItemCollectedServerRpc");
            CurrentStory.UpdateQuestObjectiveAmount(eventQuestType, amountType);
            SyncQuestStatus(CurrentStory.CurrentQuest);
        }
        
        private void SyncQuestStatus(Quest quest)
        {
            var state = new QuestState {IsCompleted = quest.objectives[0].isCompleted, CurrrentAmount = quest.objectives[0].CurrentAmount, Status = quest.Status};
            UpdateQuestServerRpc(state);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void UpdateQuestServerRpc(QuestState state, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log($"QuestGiver: UpdateQuestServerRpc: {clientId}");
            QuestStateSyncValue = state;
        }

        /**
         * Finish the current quest and start the next one
         */
        public void CompleteQuest()
        {
            FinishStatusGiverByQuestId(CurrentStory.CurrentQuest.ID);
            if (CurrentStory.CompleteQuest() != null)
            {
                RefreshQuestGivers();
            }
            else
            {
                CurrentStory.CompleteStory();
                OnStoryComletedEvent?.Invoke(CurrentStory);
                Debug.Log("Story completed");
            }
        }

        /**
         * Update the current story and the status of the current quest giver
         */
        public void AcceptQuest()
        {
            UpdateStatusGiverByQuestId(CurrentStory.CurrentQuest);
            CurrentStory.AcceptQuest();
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
        
        public List<Story> Stories { get => stories; set => stories = value; }
        
        public Story CurrentStory { get => currentStory; set => currentStory = value; }
        
        public QuestState QuestStateSyncValue { get => QuestStateSync.Value; set => QuestStateSync.Value = value; }

        #endregion

    }
}