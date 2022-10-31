﻿using System;
using System.Collections.Generic;
using Kolman_Freecss.Krodun;
using UnityEngine;

namespace Kolman_Freecss.QuestSystem
{
    public class QuestGiver : MonoBehaviour
    {
        [Header("Quest Info")] public List<QuestSO> QuestsSO = new List<QuestSO>();
        [SerializeField] float turnSpeed = 5f;
        
        public Quest CurrentQuest;
        public List<GameObject> QuestMarkers;

        private List<Quest> Quests = new List<Quest>();
        
        // Auxiliar variables
        private GameObject _notStarted;
        private GameObject _inProgress;
        private GameObject _completed;

        KrodunController _player;

        private void Awake()
        {
            _player = FindObjectOfType<KrodunController>();
            QuestsSO.ForEach(x => Quests.Add(new Quest(x, x.StoryStep)));
            _notStarted = QuestMarkers.Find(g => g.name == "ExclamationNotStarted");
            _inProgress = QuestMarkers.Find(g => g.name == "ExclamationStarted");
            _completed = QuestMarkers.Find(g => g.name == "QuestCompletedMark");
        }

        void Start()
        {
            CurrentQuest = Quests[0];
        }

        public Quest UpdateQuestStatus()
        {
            CurrentQuest.UpdateStatus();
            RefreshQuestMark();
            return CurrentQuest;
        }

        /**
         * Refresh the quest by quest id parameter
         */
        public void RefreshQuest(int questId)
        {
            Quest qs = Quests.Find(x => x.ID == questId).UpdateStatus();
            if (qs.Status == QuestStatus.NotStarted)
            {
                CurrentQuest = qs;
            }
            RefreshQuestMark();
        }

        /**
         * Displays one mark on the quest giver by the current quest status
         */
        private void RefreshQuestMark()
        {
            if (CurrentQuest != null)
            {
                _notStarted.SetActive(false);
                _inProgress.SetActive(false);
                _completed.SetActive(false);
                switch (CurrentQuest.Status)
                {
                    case QuestStatus.NotStarted:
                        _notStarted.SetActive(true);
                        break;
                    case QuestStatus.Started:
                        _inProgress.SetActive(true);
                        break;
                    case QuestStatus.Completed:
                        _completed.SetActive(true);
                        break;
                }
            }
        }
        
        /**
         * Face the quest giver to the player
         */
        public void FaceTarget()
        {
            Vector3 direction = (_player.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
        
        public void FinishQuest()
        {
            CurrentQuest = null;
            RefreshQuestMark();
        }

        public Quest CompleteQuest()
        {
            RefreshQuestMark();
            return CurrentQuest.CompleteQuest();
        }
        
        public bool HasQuest(Quest quest)
        {
            return Quests.Contains(quest);
        }

        public bool HasQuest(int questId)
        {
            return Quests.Exists(x => x.ID == questId);
        }
    }
}