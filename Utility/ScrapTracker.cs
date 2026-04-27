using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    public static class ScrapTracker
    {
        static List<GrabbableObject> allTrackedItems = [];
        internal static int TotalValue { get; private set; }

        internal static void Reset()
        {
            allTrackedItems.Clear();
            TotalValue = 0;
        }

        internal static void Track(GrabbableObject grabbableObject, bool addValue = true)
        {
            if (grabbableObject == null || grabbableObject.NetworkObject == null || !grabbableObject.NetworkObject.IsSpawned)
                return;

            if (!grabbableObject.itemProperties.isScrap /*&& grabbableObject.itemProperties.itemId != 14*/)
                return;

            if (grabbableObject.deactivated || grabbableObject.scrapPersistedThroughRounds)
                return;

            if (grabbableObject is RagdollGrabbableObject)
                return;

            if (grabbableObject is GiftBoxItem giftBoxItem && giftBoxItem.IsServer)
            {
                Plugin.Logger.LogDebug($"Scrap tracker: Can't track \"{grabbableObject.name}\" with Track()");
                return;
            }

            if (allTrackedItems.Contains(grabbableObject))
                return;

            allTrackedItems.Add(grabbableObject);
            if (addValue)
                TotalValue += grabbableObject.scrapValue;
            Plugin.Logger.LogDebug($"Scrap tracker: Tracking \"{grabbableObject.name}\" worth ${grabbableObject.scrapValue}");
        }

        internal static void Untrack(GrabbableObject grabbableObject)
        {
            if (grabbableObject != null)
                allTrackedItems.Remove(grabbableObject);
        }

        internal static void CountButlers()
        {
            ButlerEnemyAI[] butlerEnemyAIs = Object.FindObjectsByType<ButlerEnemyAI>(FindObjectsSortMode.None);
            foreach (ButlerEnemyAI butlerEnemyAI in butlerEnemyAIs)
            {
                if (!butlerEnemyAI.isEnemyDead)
                {
                    KnifeItem knife = butlerEnemyAI.knifePrefab?.GetComponent<KnifeItem>();
                    if (knife != null)
                    {
                        TotalValue += knife.scrapValue;
                        Plugin.Logger.LogDebug($"Did not kill Butler (${knife.scrapValue})");
                    }
                }
            }
        }

        internal static void TrackGiftBoxOnServer(GiftBoxItem giftBoxItem)
        {
            if (allTrackedItems.Contains(giftBoxItem))
                return;

            allTrackedItems.Add(giftBoxItem);
            TotalValue = Mathf.Max(TotalValue + giftBoxItem.scrapValue, TotalValue + giftBoxItem.objectInPresentValue);
        }

        public static void TrackGiftBoxOnClient(GiftBoxItem giftBoxItem, GrabbableObject objectInPresent)
        {
            if (!allTrackedItems.Contains(giftBoxItem))
            {
                allTrackedItems.Add(giftBoxItem);
                if (giftBoxItem.scrapValue > 0)
                    TotalValue += giftBoxItem.scrapValue;
            }

            if (giftBoxItem.isInShipRoom)
                RoundManager.Instance.scrapCollectedInLevel = Mathf.Max(RoundManager.Instance.scrapCollectedInLevel - giftBoxItem.scrapValue, 0);

            if (objectInPresent != null)
            {
                if (!allTrackedItems.Contains(objectInPresent))
                    allTrackedItems.Add(objectInPresent);

                if (giftBoxItem.isInShipRoom || objectInPresent.isInShipRoom || objectInPresent.transform.parent == StartOfRound.Instance.elevatorTransform)
                    RoundManager.Instance.CollectNewScrapForThisRound(objectInPresent);
            }

            if (!giftBoxItem.IsServer)
                TotalValue = Mathf.Max(TotalValue - Mathf.Max(giftBoxItem.scrapValue, 0) + (objectInPresent != null ? objectInPresent.scrapValue : 0), TotalValue);
        }
    }
}
