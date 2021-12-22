using Cinemachine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Core
{
    public class CameraController : MonoBehaviour
    {
        // State
        CinemachineStateDrivenCamera stateCamera = null;
        List<CinemachineVirtualCamera> virtualCameras = new List<CinemachineVirtualCamera>();

        // Cached References
        GameObject playerGameObject = null;
        ReInitLazyValue<Player> player;
        ReInitLazyValue<Party> party;

        private void Awake()
        {
            stateCamera = GetComponentInChildren<CinemachineStateDrivenCamera>();
            virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>().ToList();

            player = new ReInitLazyValue<Player>(SetupPlayerReference);
            party = new ReInitLazyValue<Party>(SetupPartyReference);
        }

        private void OnEnable()
        {
            if (party.value != null) { party.value.partyUpdated += SetUpStateDrivenCamera; }   
        }

        private void OnDisable()
        {
            if (party.value != null) { party.value.partyUpdated -= SetUpStateDrivenCamera; }
        }

        private void Start()
        {
            player.ForceInit();
            party.ForceInit();
            SetUpStateDrivenCamera();
            SetUpVirtualCameraFollowers();
        }

        private Player SetupPlayerReference()
        {
            if (playerGameObject == null) { playerGameObject = GameObject.FindGameObjectWithTag("Player"); }
            return playerGameObject?.GetComponent<Player>();
        }

        private Party SetupPartyReference()
        {
            if (playerGameObject == null) { playerGameObject = GameObject.FindGameObjectWithTag("Player"); }
            return playerGameObject?.GetComponent<Party>();
        }

        private void SetUpStateDrivenCamera()
        {
            if (party.value != null)
            {
                UpdateStateAnimator(party.value.GetLeadCharacterAnimator());
            }
        }

        private void SetUpVirtualCameraFollowers()
        {
            if (party.value == null) { return; }

            foreach (CinemachineVirtualCamera virtualCamera in virtualCameras)
            {
                virtualCamera.Follow = player.value.transform;
            }
        }

        private void UpdateStateAnimator(Animator characterAnimator)
        {
            stateCamera.m_AnimatedTarget = characterAnimator;
        }
    }

}
