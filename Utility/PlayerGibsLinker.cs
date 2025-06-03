using UnityEngine;

namespace ButteryFixes.Utility
{
    internal class PlayerGibsLinker : MonoBehaviour
    {
        void Start()
        {
            if (GlobalReferences.gibbedPlayer != null)
            {
                if (Configuration.playermodelPatches.Value && GlobalReferences.gibbedPlayer.currentSuitID != 0)
                {
                    UnlockableItem suit = StartOfRound.Instance.unlockablesList.unlockables[GlobalReferences.gibbedPlayer.currentSuitID];
                    foreach (Renderer rend in GetComponentsInChildren<Renderer>())
                    {
                        if (rend.name == "Head (2)")
                        {
                            rend.sharedMaterial = suit.suitMaterial;
                            if (suit.headCostumeObject != null)
                            {
                                Transform hat = Instantiate(suit.headCostumeObject, rend.transform.position, rend.transform.rotation, rend.transform).transform;
                                hat.transform.SetLocalPositionAndRotation(new Vector3(0.0698937327f, 0.0544735007f, -0.685245395f), Quaternion.Euler(96.69699f, 0f, 0f));
                                // head is a little shrunken
                                hat.transform.localScale = new Vector3(hat.transform.localScale.x / rend.transform.lossyScale.x, hat.transform.localScale.y / rend.transform.lossyScale.y, hat.transform.localScale.z / rend.transform.lossyScale.z) * 0.9f;
                            }
                        }
                        else if (rend is SkinnedMeshRenderer)
                        {
                            rend.sharedMaterials =
                            [
                                suit.suitMaterial,
                                rend.sharedMaterials[1]
                            ];
                            if (rend.name == "RendedBodyTorsoMesh" && suit.lowerTorsoCostumeObject != null)
                            {
                                Transform tail = Instantiate(suit.lowerTorsoCostumeObject, rend.transform.position, rend.transform.rotation, rend.transform).transform;
                                tail.transform.SetLocalPositionAndRotation(new Vector3(0.559882343f, -0.0460970625f, -5.00411654f), Quaternion.Euler(88.184f, -51.554f, -51.067f));
                                tail.transform.localScale = new Vector3(tail.transform.localScale.x / rend.transform.lossyScale.x, tail.transform.localScale.y / rend.transform.lossyScale.y, tail.transform.localScale.z / rend.transform.lossyScale.z);
                            }
                        }
                    }
                    Plugin.Logger.LogDebug($"Apply suit to {GlobalReferences.gibbedPlayer.playerUsername}'s gibs");
                }
                GlobalReferences.gibbedPlayer = null;
            }
        }
    }
}
